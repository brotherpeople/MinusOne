using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class BaseCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Card Data")]
    public int cardNumber = 1;
    public Image cardImage;

    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite selectedSprite;
    public Sprite disabledSprite;

    [Header("Interaction Settings")]
    public bool isDraggable = false;

    private bool isDisabled = false;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    public System.Action<BaseCard> OnCardClicked;
    public System.Action<BaseCard> OnDragStart;
    public System.Action<BaseCard> OnDragEnd;

    void Awake()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        
        var button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                if (!isDragging) OnCardClicked?.Invoke(this);
            });
        }
    }

    void Start()
    {
        originalPosition = transform.localPosition;
        originalParent = transform.parent;
        // if (normalSprite != null && cardImage != null)
        // {
        //     SetNormalState();
        // }
        // else
        // {
        //     Debug.Log($"Card {cardNumber}: Waiting for sprite assignment");
        // }
        Debug.Log($"Card {cardNumber}: Start() called - waiting for explicit state setting");

    }

    public void SetCardNumber(int number) => cardNumber = number;
    public int GetCardNumber() => cardNumber;
    public bool IsDisabled() => isDisabled;
    public bool IsDragging() => isDragging;

    public void SetDraggable(bool draggable) => isDraggable = draggable;

    public void SetNormalState()
    {
        if (cardImage == null)
        {
            Debug.LogError($"Card {cardNumber}: cardImage is null in SetNormalState");
            return;
        }

        if (canvasGroup == null)
        {
            Debug.LogError($"Card {cardNumber}: canvasGroup is null in SetNormalState");
            return;
        }

        isDisabled = false;
        Debug.Log($"Card {cardNumber}: isDisabled set to false");


        if (normalSprite != null)
        {
            cardImage.sprite = normalSprite;
            Debug.Log($"Card {cardNumber}: Applied normal sprite");

        }
        else
        {
            Debug.LogWarning($"Card {cardNumber}: normalSprite is null");
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        var button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true;
            Debug.Log($"Card {cardNumber}: Button enabled");

        }
        Debug.Log($"Card {cardNumber}: SetNormalState complete. IsDisabled(): {IsDisabled()}");

    }

    public void SetSelectedState()
    {
        if (cardImage != null && selectedSprite != null)
        {
            cardImage.sprite = selectedSprite;
        }

    }

    public void SetDisabledState()
    {
        if (cardImage == null || canvasGroup == null)
        {
            Debug.LogError($"Card {cardNumber}: Missing components in SetDisabledState");
            return;
        }

        isDisabled = true;
        Debug.Log($"Card {cardNumber}: isDisabled set to true");

        if (disabledSprite != null)
        {
            cardImage.sprite = disabledSprite;
            Debug.Log($"Card {cardNumber}: Applied disabled sprite");

        }
        else
        {
            Debug.LogWarning($"Card {cardNumber}: disabledSprite is null!");
        }


        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        var button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = false;
            Debug.Log($"Card {cardNumber}: Button disabled");

        }
        Debug.Log($"Card {cardNumber}: SetDisabledState complete. IsDisabled(): {IsDisabled()}");

    }
    public void Initialize()
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();
            
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        Debug.Log($"Card {cardNumber}: Initialize called");
            
        // SetNormalState();
    }


    // For ClickableCard behavior (MainScene)
    public void MoveToZone(Transform zone)
    {
        transform.SetParent(zone);
        transform.localPosition = Vector3.zero;
        SetSelectedState();
    }

    public void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        SetNormalState();
    }

    public bool IsInOriginalPosition() => Vector3.Distance(transform.localPosition, originalPosition) < 10f;

    // For DraggableCard behavior (FieldScene)
    public void MoveToPosition(Vector3 position) => StartCoroutine(AnimateToPosition(position));

    private IEnumerator AnimateToPosition(Vector3 targetPosition)
    {
        Vector3 startPos = transform.localPosition;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.localPosition = targetPosition;
    }

    #region Drag and Drop Implementation
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable || isDisabled) return;

        isDragging = true;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
        }
        OnDragStart?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable || isDisabled) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform, eventData.position,
            eventData.pressEventCamera, out Vector2 localPoint);
        transform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable || isDisabled) return;

        isDragging = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        OnDragEnd?.Invoke(this);
    }
    #endregion
}
