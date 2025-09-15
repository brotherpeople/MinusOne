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

    // initialize card components and setup click events
    void Awake()
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();

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

    // store original position and parent for reset functionality
    void Start()
    {
        originalPosition = transform.localPosition;
        originalParent = transform.parent;
    }

    // set card number
    public void SetCardNumber(int number) => cardNumber = number;

    // get card number
    public int GetCardNumber() => cardNumber;

    // check if card is disabled
    public bool IsDisabled() => isDisabled;

    // check if card is being dragged
    public bool IsDragging() => isDragging;

    // enable/disable drag functionality
    public void SetDraggable(bool draggable) => isDraggable = draggable;

    // set card state as normal/active
    public void SetNormalState()
    {
        if (cardImage == null || canvasGroup == null) return;

        isDisabled = false;

        if (normalSprite != null)
            cardImage.sprite = normalSprite;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        var button = GetComponent<Button>();
        if (button != null)
            button.interactable = true;
    }

    // set card state as selected
    public void SetSelectedState()
    {
        if (cardImage != null && selectedSprite != null)
            cardImage.sprite = selectedSprite;
    }

    // set card state as disabled
    public void SetDisabledState()
    {
        if (cardImage == null || canvasGroup == null) return;

        isDisabled = true;

        if (disabledSprite != null)
            cardImage.sprite = disabledSprite;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        var button = GetComponent<Button>();
        if (button != null)
            button.interactable = false;
    }

    // move card to specific zone and set selected state (for card selection scene)
    public void MoveToZone(Transform zone)
    {
        transform.SetParent(zone);
        transform.localPosition = Vector3.zero;
        SetSelectedState();
    }

    // return card to original position and reset state
    public void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        SetNormalState();
    }

    // check if card is in original position
    public bool IsInOriginalPosition() => Vector3.Distance(transform.localPosition, originalPosition) < 10f;

    // move card to specific position with animation (for field scene)
    public void MoveToPosition(Vector3 position) => StartCoroutine(AnimateToPosition(position));

    // animate card movement to target position
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
    // start drag operation
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

    // handle card dragging
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable || isDisabled) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform, eventData.position,
            eventData.pressEventCamera, out Vector2 localPoint);
        transform.localPosition = localPoint;
    }

    // end drag operation
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