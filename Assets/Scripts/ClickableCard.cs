using UnityEngine;
using UnityEngine.UI;

// 간단한 클릭 기반 카드 시스템
public class ClickableCard : MonoBehaviour
{
    [Header("Card Data")]
    public int cardNumber = 1;
    public Image cardImage;
    public Button cardButton;

    [Header("Card Sprites")]
    public Sprite normalSprite;
    public Sprite selectedSprite;
    public Sprite disabledSprite;

    private bool isSelected = false;
    private bool isDisabled = false;
    private Vector3 originalPosition;
    private Transform originalParent;

    public System.Action<ClickableCard> OnCardClicked;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalParent = transform.parent;

        if (cardButton == null)
            cardButton = GetComponent<Button>();

        cardButton.onClick.AddListener(OnClick);
        SetNormalState();
    }

    public void SetCardNumber(int number)
    {
        cardNumber = number;
    }

    public void SetNormalState()
    {
        isSelected = false;
        isDisabled = false;
        if (cardImage != null && normalSprite != null)
            cardImage.sprite = normalSprite;
        cardButton.interactable = true;
    }

    public void SetSelectedState()
    {
        isSelected = true;
        if (cardImage != null && selectedSprite != null)
            cardImage.sprite = selectedSprite;
    }

    public void SetDisabledState()
    {
        isSelected = false;
        isDisabled = true;
        if (cardImage != null && disabledSprite != null)
            cardImage.sprite = disabledSprite;
        cardButton.interactable = false;
    }

    public void MoveToZone(Transform zoneTransform)
    {
        transform.SetParent(zoneTransform);
        transform.localPosition = Vector3.zero;
        SetSelectedState();
    }

    public void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        SetNormalState();
    }

    public int GetCardNumber() => cardNumber;
    public bool IsSelected() => isSelected;
    public bool IsDisabled() => isDisabled;
    public bool IsInOriginalPosition() => transform.parent == originalParent;

    private void OnClick()
    {
        if (!isDisabled)
            OnCardClicked?.Invoke(this);
    }
}