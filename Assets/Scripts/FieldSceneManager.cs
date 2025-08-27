using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class FieldSceneManager : MonoBehaviour
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

                // Set up with card back sprites (hidden until result scene)
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

        // If same card is clicked again, deselect it
        if (selectedCard == clickedCard)
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
            int submittedCard = selectedCard.GetCardNumber();

            Debug.Log($"Player submitted card: {submittedCard}");

            // Save player's submitted card to PlayerPrefs
            PlayerPrefs.SetInt("PlayerSubmittedCard", submittedCard);
            PlayerPrefs.Save();

            // Load Result Scene
            SceneManager.LoadScene("ResultScene");
        }
    }
    #endregion
}