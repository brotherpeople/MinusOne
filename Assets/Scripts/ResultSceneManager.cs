using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ResultSceneManager : MonoBehaviour
{
    [Header("AI Player Info Panels")]
    public PlayerInfoPanel leftPlayerPanel;   // Left side
    public PlayerInfoPanel topPlayerPanel;    // Top side  
    public PlayerInfoPanel rightPlayerPanel;  // Right side

    [Header("Card Sprites")]
    public Sprite[] normalSprites = new Sprite[8];
    public Sprite[] selectedSprites = new Sprite[8];
    public Sprite[] disabledSprites = new Sprite[8];
    public Sprite cardBackSprite;

    [Header("Result Display")]
    public Transform resultCardParent;
    public GameObject playerCardPrefab;
    public GameObject playerTextPrefab;
    public Vector3 resultStartPosition = new Vector3(-300f, 0f, 0f);
    public float resultCardSpacing = 130f;

    [Header("Winner Display")]
    public GameObject winnerTextPrefab;
    public Vector3 winnerTextPosition = new Vector3(0f, -150f, 0f);
    private List<ClickableCard> resultCards = new List<ClickableCard>();

    private int playerLeftCard;
    private int playerRightCard;
    private int playerSubmittedCard;
    private int currentRound;
    private int aiPlayerCount;

    private int[] aiLeftCards;
    private int[] aiRightCards;
    private int[] finalSubmittedCards;  // [AI1, AI2, AI3, Player]

    void Start()
    {
        LoadGameData();
        GenerateAICardSelections();

        // Initialize result array first
        finalSubmittedCards = new int[aiPlayerCount + 1]; // AI players + human player
        Debug.Log($"Initialized finalSubmittedCards with length: {finalSubmittedCards.Length}");

        ProcessPlayerSubmission();
        ProcessAISubmissions();

        // Show all submitted cards in the center
        ShowResultCards();
    }

    void LoadGameData()
    {
        // Load data from previous scene
        playerLeftCard = PlayerPrefs.GetInt("LeftCard", 1);
        playerRightCard = PlayerPrefs.GetInt("RightCard", 2);
        playerSubmittedCard = PlayerPrefs.GetInt("PlayerSubmittedCard", 1);
        currentRound = PlayerPrefs.GetInt("CurrentRound", 1);
        aiPlayerCount = PlayerPrefs.GetInt("AICount", 3);

        Debug.Log($"Result Scene loaded - Round: {currentRound}, Player submitted: {playerSubmittedCard}");
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

    void ProcessPlayerSubmission()
    {
        int submittedCard = playerSubmittedCard;
        int tempStorageCard = (playerSubmittedCard == playerLeftCard) ? playerRightCard : playerLeftCard;
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

        // Create result cards with specified dimensions (110*140)
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

            // Set card dimensions to 110*140
            RectTransform rectTransform = resultCardObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(110f, 140f);

            // Position cards horizontally
            Vector3 cardPosition = resultStartPosition + new Vector3(i * resultCardSpacing, 0f, 0f);
            rectTransform.localPosition = cardPosition;

            if (playerTextPrefab != null)
            {
                GameObject playerTextObj = Instantiate(playerTextPrefab, resultCardParent);
                Vector3 textPosition = cardPosition + new Vector3(0f, 100f, 0f);
                playerTextObj.GetComponent<RectTransform>().localPosition = textPosition;

                // Set player text
                TextMeshProUGUI playerText = playerTextObj.GetComponent<TextMeshProUGUI>();
                if (playerText != null)
                {
                    string playerLabel = (i < aiPlayerCount) ? $"P{i + 1}" : "YOU";
                    playerText.text = playerLabel;
                }

                Debug.Log($"Player text created: {playerText?.text} at position {textPosition}");
            }

            // Add player label
            string playerLabelDebug = (i < aiPlayerCount) ? $"PLAYER {i + 1}" : "YOU";
            Debug.Log($"Result card {i}: {playerLabelDebug} played {cardNumber}");
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
                    // Left card was submitted - use selected sprite for left, disabled for right
                    leftCardSprite = selectedSprites[aiLeftCards[i] - 1];
                    rightCardSprite = disabledSprites[aiRightCards[i] - 1]; // Right card goes to temp storage
                }
                else
                {
                    // Right card was submitted - use selected sprite for right, disabled for left
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

    void DetermineWinner()
    {

    }
}