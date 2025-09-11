using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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

    void Start()
    {
        originalPosition = transform.localPosition;
        originalParent = transform.parent;
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        
        GetComponent<Button>().onClick.AddListener(() => {
            if (!isDragging) OnCardClicked?.Invoke(this);
        });
        
        SetNormalState();
    }

    public void SetCardNumber(int number) => cardNumber = number;
    public int GetCardNumber() => cardNumber;
    public bool IsDisabled() => isDisabled;
    public bool IsDragging() => isDragging;

    public void SetDraggable(bool draggable) => isDraggable = draggable;

    public void SetNormalState()
    {
        isDisabled = false;
        cardImage.sprite = normalSprite;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        GetComponent<Button>().interactable = true;
    }

    public void SetSelectedState() => cardImage.sprite = selectedSprite;

    public void SetDisabledState()
    {
        isDisabled = true;
        cardImage.sprite = disabledSprite;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        GetComponent<Button>().interactable = false;
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
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
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
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        OnDragEnd?.Invoke(this);
    }
    #endregion
}
