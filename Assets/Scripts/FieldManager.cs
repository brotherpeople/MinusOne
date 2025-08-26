using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FieldManager : MonoBehaviour
{
    [Header("Player Cards")]
    public Transform playerCardParent;
    public GameObject playerCardPrefab;

    [Header("AI Player Info Panels")]
    public PlayerInfoPanel leftPlayerPanel;   // Left side
    public PlayerInfoPanel topPlayerPanel;    // Top side  
    public PlayerInfoPanel rightPlayerPanel;  // Right side

    [Header("Card Sprites")]
    public Sprite[] normalSprites = new Sprite[8];
    public Sprite[] selectedSprites = new Sprite[8];
    public Sprite[] disabledSprites = new Sprite[8];
    public Sprite cardBackSprite;                 // Card back sprite

    [Header("Submit Button")]
    public GameObject submitButtonPrefab;  // Prefab for submit button
    public Vector3 submitButtonOffset = new Vector3(0, -80, 0);  // Offset from card position
    [Header("Disabled text")]
    public GameObject disabledTextPrefab;     // Current disabled prefab

    [Header("Result Display")]
    public Transform resultCardParent;
    public Vector3 resultStartPosition = new Vector3(-300f, 0f, 0f);
    public float resultCardSpacing = 200f;

    [Header("Animation")]
    public float animationDuration = 1f;

    private int playerLeftCard;
    private int playerRightCard;
    private int currentRound;
    private int aiPlayerCount;

    private Vector3 leftCardStartPos;
    private Vector3 rightCardStartPos;

    private int[] aiLeftCards;
    private int[] aiRightCards;

    private ClickableCard leftCard;
    private ClickableCard rightCard;
    private ClickableCard selectedCard = null;  // Currently selected card
    private GameObject submitButton = null;     // Current submit button
    private GameObject disabledText = null;     // Current disabledText


    // Result cards storage
    private int[] finalSubmittedCards;

    void Start()
    {
        InitializeGameData();
        SetupAllPlayers();
        AnimatePlayerCards();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (selectedCard != null)
            {
                Debug.Log("Right mouse button clicked");
                HandleCardSelection(null);
            }
        }
    }

    #region Initialization Methods
    void InitializeGameData()
    {
        // Load player data from previous scene
        playerLeftCard = PlayerPrefs.GetInt("LeftCard", 1);
        playerRightCard = PlayerPrefs.GetInt("RightCard", 2);
        currentRound = PlayerPrefs.GetInt("CurrentRound", 1);
        aiPlayerCount = PlayerPrefs.GetInt("AICount", 3);

        leftCardStartPos = new Vector3(-180, 200, 0);
        rightCardStartPos = new Vector3(180, 200, 0);

        // Generate AI card selections
        GenerateAICardSelections();

        Debug.Log($"Field Scene loaded - Round: {currentRound}, Player cards: {playerLeftCard}, {playerRightCard}");
    }

    void GenerateAICardSelections()
    {
        aiLeftCards = new int[aiPlayerCount];
        aiRightCards = new int[aiPlayerCount];

        for (int i = 0; i < aiPlayerCount; i++)
        {
            if (GameManager.Instance != null && GameManager.Instance.allPlayersCardData != null)
            {
                int playerIndex = i + 1; // AI players start from index 1
                var playerData = GameManager.Instance.allPlayersCardData[playerIndex];

                aiLeftCards[i] = playerData.selectedLeftCard;
                aiRightCards[i] = playerData.selectedRightCard;

                Debug.Log($"AI Player {i + 1} cards from GameManager: {aiLeftCards[i]}, {aiRightCards[i]}");
            }
            else
            {
                // Fallback: random selection
                int firstCard = Random.Range(1, 9);
                int secondCard;
                do { secondCard = Random.Range(1, 9); } while (secondCard == firstCard);

                aiLeftCards[i] = firstCard;
                aiRightCards[i] = secondCard;

                Debug.Log($"AI Player {i + 1} fallback selection: {firstCard}, {secondCard}");
            }
        }
    }

    void SetupAllPlayers()
    {
        SetupPlayerCards();
        SetupAIPlayers();
    }

    void SetupPlayerCards()
    {
        // Create left card
        GameObject leftCardObj = Instantiate(playerCardPrefab, playerCardParent);
        leftCard = leftCardObj.GetComponent<ClickableCard>();
        leftCard.SetCardNumber(playerLeftCard);
        leftCard.normalSprite = normalSprites[playerLeftCard - 1];
        leftCard.selectedSprite = selectedSprites[playerLeftCard - 1];
        leftCard.disabledSprite = disabledSprites[playerLeftCard - 1];
        leftCard.OnCardClicked += OnPlayerCardClicked;
        leftCardObj.GetComponent<RectTransform>().localPosition = leftCardStartPos;

        // Create right card
        GameObject rightCardObj = Instantiate(playerCardPrefab, playerCardParent);
        rightCard = rightCardObj.GetComponent<ClickableCard>();
        rightCard.SetCardNumber(playerRightCard);
        rightCard.normalSprite = normalSprites[playerRightCard - 1];
        rightCard.selectedSprite = selectedSprites[playerRightCard - 1];
        rightCard.disabledSprite = disabledSprites[playerRightCard - 1];
        rightCard.OnCardClicked += OnPlayerCardClicked;
        rightCardObj.GetComponent<RectTransform>().localPosition = rightCardStartPos;
    }

    void SetupAIPlayers()
    {
        PlayerInfoPanel[] panels = { leftPlayerPanel, topPlayerPanel, rightPlayerPanel };

        for (int i = 0; i < aiPlayerCount && i < 3; i++)
        {
            if (panels[i] != null)
            {
                // Get victory tokens from GameManager
                int victoryTokens = 0;
                if (GameManager.Instance != null && i + 1 < GameManager.Instance.victoryTokens.Length)
                {
                    victoryTokens = GameManager.Instance.victoryTokens[i + 1]; // +1 because player 0 is human
                }

                // Set up with actual card sprites
                Sprite leftCardSprite = normalSprites[aiLeftCards[i] - 1];
                Sprite rightCardSprite = normalSprites[aiRightCards[i] - 1];

                panels[i].SetupPlayer($"PLAYER {i + 1}", victoryTokens, leftCardSprite, rightCardSprite);
            }
        }
    }
    #endregion

    #region Card Animation
    void AnimatePlayerCards()
    {
        if (leftCard != null) StartCoroutine(AnimateCard(leftCard.transform, 130));
        if (rightCard != null) StartCoroutine(AnimateCard(rightCard.transform, 130));
    }

    IEnumerator AnimateCard(Transform card, float targetPosY)
    {
        Vector3 startPos = card.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, targetPosY, startPos.z);

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = 1f - Mathf.Pow(1f - t, 4f); // Ease out effect
            card.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        card.localPosition = targetPos;
    }
    #endregion

    #region Card Selection and Submission
    void OnPlayerCardClicked(ClickableCard clickedCard)
    {
        Debug.Log($"Player clicked card: {clickedCard.GetCardNumber()}");

        // If same card is clicked again or mouse right button clicked, deselect it
        if (selectedCard == clickedCard || Input.GetMouseButtonDown(1))
        {
            HandleCardSelection(null);
            return;
        }

        // Select the new card (automatically deselects previous if any)
        HandleCardSelection(clickedCard);
    }

    void HandleCardSelection(ClickableCard newSelectedCard)
    {
        // Deselect current card if any
        if (selectedCard != null)
        {
            selectedCard.SetNormalState();
        }

        // Reset both cards to normal state first
        if (leftCard != null) leftCard.SetNormalState();
        if (rightCard != null) rightCard.SetNormalState();

        // Select new card if provided
        selectedCard = newSelectedCard;
        if (selectedCard != null)
        {
            selectedCard.SetSelectedState();

            // Disable the other card
            ClickableCard otherCard = (selectedCard == leftCard) ? rightCard : leftCard;
            if (otherCard != null)
            {
                otherCard.SetDisabledState();
                disabledText = Instantiate(disabledTextPrefab, playerCardParent);
                Vector3 textPos = otherCard.transform.localPosition;
                disabledText.GetComponent<RectTransform>().localPosition = textPos;
            }

            CreateSubmitButton();
            Debug.Log($"Card {selectedCard.GetCardNumber()} selected, other card disabled");
        }
        else
        {
            Destroy(submitButton);
            Destroy(disabledText);
            Debug.Log("Card deselected, both cards enabled");
        }
    }

    void CreateSubmitButton()
    {
        if (submitButtonPrefab != null && selectedCard != null)
        {
            submitButton = Instantiate(submitButtonPrefab, playerCardParent);

            Vector3 buttonPosition = selectedCard.transform.localPosition + submitButtonOffset;
            submitButton.GetComponent<RectTransform>().localPosition = buttonPosition;

            // Setup button text and click event
            TextMeshProUGUI buttonText = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            Button button = submitButton.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnSubmitButtonClicked);

            Debug.Log($"Submit button created at position: {buttonPosition}");
        }
        else
        {
            Debug.LogWarning("Submit button prefab is not assigned or no card selected!");
        }
    }

    void OnSubmitButtonClicked()
    {
        if (selectedCard != null)
        {
            Destroy(submitButton);
            Destroy(disabledText);
            Debug.Log($"OnSubmitButtonClicked - aiPlayerCount: {aiPlayerCount}");

            // Initialize result array first
            finalSubmittedCards = new int[aiPlayerCount + 1]; // AI players + human player
            Debug.Log($"Initialized finalSubmittedCards with length: {finalSubmittedCards.Length}");

            ProcessPlayerSubmission();
            ProcessAISubmissions();

            // Show all submitted cards in the center
            ShowResultCards();

            HandleCardSelection(null); // Deselect card and destroy button
        }
    }

    void ProcessPlayerSubmission()
    {
        int submittedCard = selectedCard.GetCardNumber();
        int tempStorageCard = (selectedCard == leftCard) ? rightCard.GetCardNumber() : leftCard.GetCardNumber();
        Debug.Log($"Player submitted card: {submittedCard}, Temp storage: {tempStorageCard}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ProcessCardSubmission(0, submittedCard, tempStorageCard); // Player index 0
        }

        // Ensure array is initialized before accessing
        if (finalSubmittedCards == null)
        {
            Debug.LogWarning("finalSubmittedCards is null in ProcessPlayerSubmission!");
            return;
        }

        // Store player's submitted card (last position)
        finalSubmittedCards[aiPlayerCount] = submittedCard;
        Debug.Log($"Player card stored at position {aiPlayerCount}: {submittedCard}");

    }

    void ProcessAISubmissions()
    {
        // Ensure array is initialized before accessing
        if (finalSubmittedCards == null)
        {
            Debug.LogWarning("finalSubmittedCards is null in ProcessAISubmissions!");
            return;
        }

        for (int i = 0; i < aiPlayerCount; i++)
        {
            int playerIndex = i + 1; // AI players start from index 1

            // Randomly choose between left and right card
            bool chooseLeft = Random.Range(0, 2) == 0;
            int submittedCard = chooseLeft ? aiLeftCards[i] : aiRightCards[i];
            int tempStorageCard = chooseLeft ? aiRightCards[i] : aiLeftCards[i];

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ProcessCardSubmission(playerIndex, submittedCard, tempStorageCard);
            }

            // Store AI's submitted card
            finalSubmittedCards[i] = submittedCard;

            Debug.Log($"AI Player {i + 1} submitted: {submittedCard}, Temp storage: {tempStorageCard}");
            Debug.Log($"AI card stored at position {i}: {submittedCard}");
        }
    }

    void ShowResultCards()
    {
        if (finalSubmittedCards == null || resultCardParent == null)
        {
            Debug.LogWarning("Result cards data or parent not set!");
            return;
        }

        // Update AI players' cards in their info panels
        UpdateAIPlayerPanels();

        // Create result cards with specified dimensions (110x140)
        for (int i = 0; i < finalSubmittedCards.Length; i++)
        {
            GameObject resultCardObj = Instantiate(playerCardPrefab, resultCardParent);
            ClickableCard resultCard = resultCardObj.GetComponent<ClickableCard>();

            int cardNumber = finalSubmittedCards[i];
            resultCard.SetCardNumber(cardNumber);
            resultCard.normalSprite = normalSprites[cardNumber - 1];
            resultCard.cardImage.sprite = resultCard.normalSprite;

            // Disable clicking on result cards
            resultCard.GetComponent<Button>().interactable = false;

            // Set card dimensions to 110x140
            RectTransform rectTransform = resultCardObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(110f, 140f);

            // Position cards horizontally
            Vector3 cardPosition = resultStartPosition + new Vector3(i * resultCardSpacing, 0f, 0f);
            rectTransform.localPosition = cardPosition;

            // Add player label
            string playerLabel = (i < aiPlayerCount) ? $"PLAYER {i + 1}" : "YOU";
            Debug.Log($"Result card {i}: {playerLabel} played {cardNumber}");
        }

        Debug.Log("All result cards displayed with updated player cards");

        // TODO: Calculate and show winner
        // TODO: Add continue button for next round
    }

    void UpdateAIPlayerPanels()
    {
        PlayerInfoPanel[] panels = { leftPlayerPanel, topPlayerPanel, rightPlayerPanel };

        for (int i = 0; i < aiPlayerCount && i < 3; i++)
        {
            if (panels[i] != null)
            {
                int submittedCard = finalSubmittedCards[i];

                // Determine which card was submitted and update sprites accordingly
                Sprite leftCardSprite, rightCardSprite;

                if (aiLeftCards[i] == submittedCard)
                {
                    // Left card was submitted - use selected sprite for left, normal for right
                    leftCardSprite = selectedSprites[aiLeftCards[i] - 1];
                    rightCardSprite = disabledSprites[aiRightCards[i] - 1]; // Right card goes to temp storage
                }
                else
                {
                    // Right card was submitted - use selected sprite for right, normal for left
                    leftCardSprite = disabledSprites[aiLeftCards[i] - 1]; // Left card goes to temp storage
                    rightCardSprite = selectedSprites[aiRightCards[i] - 1];
                }

                // Get victory tokens from GameManager
                int victoryTokens = 0;
                if (GameManager.Instance != null && i + 1 < GameManager.Instance.victoryTokens.Length)
                {
                    victoryTokens = GameManager.Instance.victoryTokens[i + 1];
                }

                // Update the panel with new sprites
                panels[i].SetupPlayer($"PLAYER {i + 1}", victoryTokens, leftCardSprite, rightCardSprite);

                Debug.Log($"AI Player {i + 1} panel updated - Submitted: {submittedCard}");
            }
        }
    }
    #endregion
}