using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;
using TMPro;

public class MainSceneManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject cardPrefab;
    public Transform cardParent;
    public Transform leftZone;
    public Transform rightZone;
    public Button confirmButton;

    [Header("Card Sprites")]
    public Sprite[] normalSprites = new Sprite[8];
    public Sprite[] selectedSprites = new Sprite[8];
    public Sprite[] disabledSprites = new Sprite[8];

    [Header("Layout")]
    public float cardSpacing = 250f;
    public Vector2 topRowPosition = new Vector2(-375f, 50f);
    public Vector2 bottomRowPosition = new Vector2(-375f, -300f);

    [Header("Round Display")]
    public GameObject roundDisplayPanel;
    public TextMeshProUGUI roundNumberText;
    public float roundDisplayDuration = 2f;

    private List<ClickableCard> allCards = new List<ClickableCard>();
    private List<ClickableCard> disabledCards = new List<ClickableCard>();
    private ClickableCard leftZoneCard = null;
    private ClickableCard rightZoneCard = null;

    void Start()
    {
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        yield return StartCoroutine(ShowRoundNumber());
        CreateCards();
        SetupConfirmButton();

        // Generate AI selections for this round
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GenerateAISelections();
        }
    }
    IEnumerator ShowRoundNumber()
    {
        int currentRound = GameManager.Instance.currentRound;
        roundDisplayPanel.SetActive(true);
        if (currentRound == 3 || currentRound == 6 || currentRound == 9 || currentRound == 12 || currentRound == 18)
        {
            roundNumberText.text = $"ROUND {currentRound}\n* SURVIVAL ROUND *";
        }
        else
        {
            roundNumberText.text = $"ROUND {currentRound}";
        }

        CanvasGroup canvasGroup = roundDisplayPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeIn(canvasGroup, 0.5f));
            yield return new WaitForSeconds(roundDisplayDuration);
            yield return StartCoroutine(FadeOut(canvasGroup, 0.5f));
        }
        else
        {
            yield return new WaitForSeconds(roundDisplayDuration);
        }

        // Hide the panel
        roundDisplayPanel.SetActive(false);
    }
    IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
    void SetupConfirmButton()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            confirmButton.interactable = false;
        }
    }

    void CreateCards()
    {
        // Get available cards for human player from GameManager
        List<int> availableCards = new List<int>();
        if (GameManager.Instance != null)
        {
            availableCards = GameManager.Instance.GetAvailableCards(0); // Player 0 is human
        }
        else
        {
            // Fallback: all cards available
            for (int i = 1; i <= 8; i++)
                availableCards.Add(i);
        }

        for (int i = 0; i < 8; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
            ClickableCard card = cardObj.GetComponent<ClickableCard>();

            card.SetCardNumber(i + 1);

            // Set sprites
            if (i < normalSprites.Length && normalSprites[i] != null)
                card.normalSprite = normalSprites[i];
            if (i < selectedSprites.Length && selectedSprites[i] != null)
                card.selectedSprite = selectedSprites[i];
            if (i < disabledSprites.Length && disabledSprites[i] != null)
                card.disabledSprite = disabledSprites[i];

            // Set position (4 cards per row)
            Vector3 cardPosition;
            if (i < 4)
                cardPosition = new Vector3(topRowPosition.x + (i * cardSpacing), topRowPosition.y, 0);
            else
                cardPosition = new Vector3(bottomRowPosition.x + ((i - 4) * cardSpacing), bottomRowPosition.y, 0);

            cardObj.GetComponent<RectTransform>().localPosition = cardPosition;

            card.OnCardClicked += OnCardClicked;
            allCards.Add(card);

            // Set card state based on availability
            if (!availableCards.Contains(i + 1))
            {
                card.SetDisabledState();
                disabledCards.Add(card);
            }
        }

        Debug.Log($"Created cards - Available: {availableCards.Count}, Disabled: {disabledCards.Count}");
    }

    private void UpdateConfirmButton()
    {
        bool canConfirm = (leftZoneCard != null && rightZoneCard != null);
        if (confirmButton != null)
            confirmButton.interactable = canConfirm;
    }

    private void OnConfirmButtonClicked()
    {
        if (leftZoneCard != null && rightZoneCard != null)
        {
            Debug.Log($"Cards confirmed: Left({leftZoneCard.GetCardNumber()}), Right({rightZoneCard.GetCardNumber()})");

            if (GameManager.Instance != null)
                GameManager.Instance.SetGamePhase(GameManager.GamePhase.FieldPhase);

            LoadFieldScene();
        }
    }

    private void LoadFieldScene()
    {
        // Save selected card info
        PlayerPrefs.SetInt("LeftCard", leftZoneCard.GetCardNumber());
        PlayerPrefs.SetInt("RightCard", rightZoneCard.GetCardNumber());

        if (GameManager.Instance != null)
        {
            PlayerPrefs.SetInt("CurrentRound", GameManager.Instance.currentRound);
            PlayerPrefs.SetInt("AICount", GameManager.Instance.aiPlayerCount);
        }

        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene("FieldScene");
    }

    private void OnCardClicked(ClickableCard clickedCard)
    {
        if (clickedCard.IsDisabled()) return;

        if (clickedCard.IsInOriginalPosition())
        {
            if (leftZoneCard == null)
            {
                leftZoneCard = clickedCard;
                clickedCard.MoveToZone(leftZone);
            }
            else if (rightZoneCard == null)
            {
                rightZoneCard = clickedCard;
                clickedCard.MoveToZone(rightZone);
            }
        }
        else
        {
            if (clickedCard == leftZoneCard)
            {
                leftZoneCard = null;
                clickedCard.ReturnToOriginalPosition();
            }
            else if (clickedCard == rightZoneCard)
            {
                rightZoneCard = null;
                clickedCard.ReturnToOriginalPosition();
            }
        }

        UpdateConfirmButton();
    }

    // Apply disabled cards from previous round
    public void ApplyDisabledCards()
    {
        foreach (ClickableCard card in disabledCards)
        {
            card.SetDisabledState();
        }
        Debug.Log($"{disabledCards.Count} cards are disabled this round");
    }

    // Clear disabled cards for next round
    public void ClearDisabledCards()
    {
        foreach (ClickableCard card in disabledCards)
        {
            card.SetNormalState();
        }
        disabledCards.Clear();
        Debug.Log("Disabled cards restored");
    }

    // Add card to disabled list (called from FieldScene)
    public void AddToDisabledCards(int cardNumber)
    {
        ClickableCard card = allCards.Find(c => c.GetCardNumber() == cardNumber);
        if (card != null && !disabledCards.Contains(card))
        {
            disabledCards.Add(card);
        }
    }
}