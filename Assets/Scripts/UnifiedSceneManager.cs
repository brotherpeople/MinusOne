using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UnifiedSceneManager : MonoBehaviour
{
    [Header("Scene Type")]
    public SceneType currentScene = SceneType.CardSelection;

    [Header("Common UI")]
    public GameObject cardPrefab;
    public GameObject fieldCardPrefab;
    public Transform cardParent;
    public Sprite[] normalSprites = new Sprite[8];
    public Sprite[] selectedSprites = new Sprite[8];
    public Sprite[] disabledSprites = new Sprite[8];

    [Header("Card Selection Scene")]
    public Transform leftZone;
    public Transform rightZone;
    public Button confirmButton;

    [Header("Field Scene")]
    public RectTransform submitArea;
    public Image submitAreaImage;
    public Color normalAreaColor = Color.white;
    public Color highlightAreaColor = Color.green;
    public GameObject disabledTextPrefab;
    private GameObject disabledText = null;
    private Vector3 dragOriginalPos;

    [Header("Result Scene")]
    public Transform resultCardParent;
    public PlayerInfoPanel[] aiPanels = new PlayerInfoPanel[3];
    public GameObject playerTextPrefab;
    public GameObject textBoxPrefab;
    public GameObject shouldBeFadedOutText;
    public GameObject fieldPanel;
    public GameObject nextRoundButton;

    [Header("Winner Reason UI")]
    public GameObject winnerReasonPrefab;
    public Transform uiParent;
    private GameObject currentReasonUI;
    private TextMeshProUGUI reasonText;

    [Header("Round Display")]
    public GameObject roundDisplayPanel;
    public TextMeshProUGUI roundNumberText;

    public enum SceneType { CardSelection, Field, Result }

    private List<BaseCard> allCards = new List<BaseCard>();
    private BaseCard leftZoneCard = null;
    private BaseCard rightZoneCard = null;
    private BaseCard selectedCard = null;

    // initialize scene based on current scene type
    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameCompleted())
        {
            return;
        }

        switch (currentScene)
        {
            case SceneType.CardSelection: StartCardSelection(); break;
            case SceneType.Field: StartFieldScene(); break;
            case SceneType.Result: StartResultScene(); break;
        }
    }

    #region Card Selection Scene
    // initialize card selection scene
    void StartCardSelection()
    {
        StartCoroutine(ShowRoundNumber());
        CreateCards(isDraggable: false);
        confirmButton?.onClick.AddListener(OnConfirmClicked);
        GameManager.Instance?.GenerateAISelections();
    }

    // show round number with fade animation
    IEnumerator ShowRoundNumber()
    {
        if (roundDisplayPanel && roundNumberText)
        {
            roundDisplayPanel.SetActive(true);
            CanvasGroup canvasGroup = roundDisplayPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = roundDisplayPanel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;

            int round = GameManager.Instance?.currentRound ?? 1;
            bool isSettlement = GameManager.Instance?.ShouldShowSurvivalRound() ?? false;
            roundNumberText.text = isSettlement ? $"ROUND {round}\n* SURVIVAL ROUND *" : $"ROUND {round}";

            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(FadeOut(canvasGroup, 0.5f));
            roundDisplayPanel.SetActive(false);
        }
    }

    // fade out UI element
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

    // handle card click in selection scene
    void OnCardClicked_Selection(BaseCard card)
    {
        if (card.IsDisabled()) return;

        if (leftZoneCard == null)
        {
            leftZoneCard = card;
            card.MoveToZone(leftZone);
        }
        else if (rightZoneCard == null && card != leftZoneCard)
        {
            rightZoneCard = card;
            card.MoveToZone(rightZone);
        }
        else if (card == leftZoneCard)
        {
            leftZoneCard = null;
            card.ReturnToOriginalPosition();
        }
        else if (card == rightZoneCard)
        {
            rightZoneCard = null;
            card.ReturnToOriginalPosition();
        }

        if (confirmButton) confirmButton.interactable = (leftZoneCard != null && rightZoneCard != null);
    }

    // confirm card selection and proceed to field scene
    void OnConfirmClicked()
    {
        int leftCard = leftZoneCard.GetCardNumber();
        int rightCard = rightZoneCard.GetCardNumber();

        PlayerPrefs.SetInt("LeftCard", leftCard);
        PlayerPrefs.SetInt("RightCard", rightCard);
        PlayerPrefs.SetInt("CurrentRound", GameManager.Instance?.currentRound ?? 1);

        var humanData = GameManager.Instance?.GetPlayerData(Player.Human);
        if (humanData != null)
        {
            humanData.selectedLeftCard = leftCard;
            humanData.selectedRightCard = rightCard;
        }

        currentScene = SceneType.Field;
        ClearScene();
        SceneManager.LoadScene("FieldScene");
    }
    #endregion

    #region Field Scene
    // initialize field scene
    void StartFieldScene()
    {
        CreateFieldCards();
        SetupAIPanels();
    }

    // create draggable cards for field scene
    void CreateFieldCards()
    {
        int leftCard = PlayerPrefs.GetInt("LeftCard", 1);
        int rightCard = PlayerPrefs.GetInt("RightCard", 2);

        CreateCard(leftCard, new Vector3(-180, 130, 0), isDraggable: true, useCardPrefab: true);
        CreateCard(rightCard, new Vector3(180, 130, 0), isDraggable: true, useCardPrefab: true);

        foreach (var card in allCards)
        {
            card.SetNormalState();
            card.OnCardClicked += OnCardClicked_Field;
            card.OnDragStart += OnDragStart;
            card.OnDragEnd += OnDragEnd;
        }
    }

    // handle card click in field scene
    void OnCardClicked_Field(BaseCard card)
    {
        if (card.IsDisabled()) return;

        if (selectedCard == card)
        {
            selectedCard = null;
            card.SetNormalState();
            var otherCard = allCards.FirstOrDefault(c => c != card);
            otherCard?.SetNormalState();

            if (disabledText != null)
            {
                Destroy(disabledText);
                disabledText = null;
            }
        }
        else
        {
            selectedCard = card;
            card.SetSelectedState();

            var otherCard = allCards.FirstOrDefault(c => c != card);
            if (otherCard != null)
            {
                otherCard.SetDisabledState();

                if (disabledTextPrefab != null)
                {
                    disabledText = Instantiate(disabledTextPrefab, cardParent);
                    disabledText.SetActive(true);
                    Vector3 textPos = otherCard.transform.localPosition;
                    disabledText.GetComponent<RectTransform>().localPosition = textPos;
                }
            }
        }
    }

    // handle drag start
    void OnDragStart(BaseCard card)
    {
        selectedCard = card;
        dragOriginalPos = card.transform.localPosition;
        if (submitAreaImage) submitAreaImage.color = highlightAreaColor;

        var otherCard = allCards.FirstOrDefault(c => c != card);
        if (otherCard != null)
        {
            otherCard.SetDisabledState();

            if (disabledTextPrefab != null)
            {
                disabledText = Instantiate(disabledTextPrefab, cardParent);
                disabledText.SetActive(true);
                Vector3 textPos = otherCard.transform.localPosition;
                disabledText.GetComponent<RectTransform>().localPosition = textPos;
            }
        }
    }

    // handle drag end
    void OnDragEnd(BaseCard card)
    {
        if (submitAreaImage)
        {
            Color subColor = submitAreaImage.color;
            submitAreaImage.color = normalAreaColor;
            subColor.a = 0f;
        }

        if (IsCardInSubmitArea(card))
        {
            ProcessSubmission(card);
        }
        else
        {
            card.MoveToPosition(dragOriginalPos);
            var otherCard = allCards.FirstOrDefault(c => c != card);
            otherCard?.SetNormalState();
            selectedCard = null;

            if (disabledText != null)
            {
                Destroy(disabledText);
                disabledText = null;
            }
        }
    }

    // check if card is in submit area
    bool IsCardInSubmitArea(BaseCard card)
    {
        if (!submitArea) return false;
        var cardRect = card.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(submitArea, cardRect.position);
    }

    // process human card submission
    void ProcessSubmission(BaseCard card)
    {
        int submitted = card.GetCardNumber();
        var otherCard = allCards.FirstOrDefault(c => c != card);
        int temp = otherCard?.GetCardNumber() ?? 0;

        GameManager.Instance?.ProcessSubmission(Player.Human, submitted, temp);
        GameManager.Instance?.SetPlayerSubmission(Player.Human, submitted);

        card.MoveToPosition(new Vector3(0, 600, 0));

        ProcessAISubmissions();
        SceneManager.LoadScene("ResultScene");
    }

    // process AI card submissions
    void ProcessAISubmissions()
    {
        var activeAI = GameManager.Instance?.GetActivePlayers().Where(p => p.IsAI()) ?? new List<Player>();

        foreach (var player in activeAI)
        {
            var data = GameManager.Instance?.GetPlayerData(player);
            if (data != null)
            {
                bool chooseLeft = Random.value < 0.5f;
                int submitted = chooseLeft ? data.selectedLeftCard : data.selectedRightCard;
                int temp = chooseLeft ? data.selectedRightCard : data.selectedLeftCard;

                GameManager.Instance?.ProcessSubmission(player, submitted, temp);
                GameManager.Instance?.SetPlayerSubmission(player, submitted);
            }
        }
    }

    // setup AI player panels
    void SetupAIPanels()
    {
        var activePlayers = GameManager.Instance?.GetActivePlayers().Where(p => p.IsAI()).ToList()
                          ?? new List<Player> { Player.AI_1, Player.AI_2, Player.AI_3 };

        for (int i = 0; i < activePlayers.Count && i < aiPanels.Length; i++)
        {
            var player = activePlayers[i];
            var data = GameManager.Instance?.GetPlayerData(player);

            if (aiPanels[i] && data != null)
            {
                var leftSprite = normalSprites[data.selectedLeftCard - 1];
                var rightSprite = normalSprites[data.selectedRightCard - 1];
                aiPanels[i].Setup(player.GetDisplayName(), data.victoryTokens, data.points, leftSprite, rightSprite);
            }
        }
    }
    #endregion

    #region Result Scene
    // initialize result scene
    void StartResultScene()
    {
        nextRoundButton.SetActive(false);
        CreateResultCards();
        UpdateAIPanels();
        DetermineWinner();
        StartCoroutine(FadeOutObject(shouldBeFadedOutText));
        StartCoroutine(FadeOutObject(fieldPanel));
        StartCoroutine(MoveCardsUp(resultCardParent));

        int round = GameManager.Instance?.currentRound ?? 1;
        bool isSurvival = GameManager.Instance?.ShouldShowSurvivalRound() ?? false;

        var buttonText = nextRoundButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText) buttonText.text = isSurvival ? "DETERMINE SURVIVOR" : "NEXT ROUND";
    }

    // move cards up with animation
    IEnumerator MoveCardsUp(Transform transform)
    {
        RectTransform cardParentRect = transform.GetComponent<RectTransform>();
        if (cardParentRect == null) yield break;

        Vector3 startPos = cardParentRect.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 100f, 0);

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cardParentRect.localPosition = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        cardParentRect.localPosition = endPos;
    }

    // fade out game object
    IEnumerator FadeOutObject(GameObject gameObject)
    {
        CanvasGroup canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // create result cards showing submitted cards
    void CreateResultCards()
    {
        var submissions = GameManager.Instance?.GetCurrentSubmissions();
        if (submissions == null || submissions.Count == 0) return;

        var activePlayers = GameManager.Instance?.GetActivePlayers() ?? new List<Player> { Player.Human };

        for (int i = 0; i < activePlayers.Count; i++)
        {
            var player = activePlayers[i];
            if (submissions.ContainsKey(player))
            {
                CreateResultCard(player, submissions[player], i);
            }
        }
    }

    // create individual result card
    void CreateResultCard(Player player, int cardNumber, int position)
    {
        var cardObj = Instantiate(fieldCardPrefab, resultCardParent);
        var card = cardObj.GetComponent<BaseCard>();

        card.normalSprite = normalSprites[cardNumber - 1];
        card.selectedSprite = selectedSprites[cardNumber - 1];
        card.disabledSprite = disabledSprites[cardNumber - 1];

        card.SetCardNumber(cardNumber);
        card.SetDraggable(false);
        card.SetNormalState();

        var button = card.GetComponent<Button>();
        button.onClick.RemoveAllListeners();

        var rect = cardObj.GetComponent<RectTransform>();
        Vector3 cardPos = new Vector3(-240f + position * 160f, -40f, 0f);
        rect.localPosition = cardPos;

        if (playerTextPrefab != null)
        {
            var textObj = Instantiate(playerTextPrefab, resultCardParent);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.localPosition = new Vector3(cardPos.x, cardPos.y + 100f, 0f);
            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = player.GetShortName();
            }
        }

        if (textBoxPrefab != null)
        {
            var boxObj = Instantiate(textBoxPrefab, resultCardParent);
            var boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.localPosition = new Vector3(cardPos.x, cardPos.y + 100f, 0f);
        }

        var nameText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = player.GetShortName();
        }
    }

    // update AI panels with submission results
    void UpdateAIPanels()
    {
        var submissions = GameManager.Instance?.GetCurrentSubmissions();
        var activeAI = GameManager.Instance?.GetActivePlayers().Where(p => p.IsAI()).ToList() ?? new List<Player>();

        for (int i = 0; i < activeAI.Count && i < aiPanels.Length; i++)
        {
            var player = activeAI[i];
            var data = GameManager.Instance?.GetPlayerData(player);

            if (data != null)
            {
                if (submissions != null && submissions.ContainsKey(player))
                {
                    int submitted = submissions[player];

                    var leftSprite = (data.selectedLeftCard == submitted) ?
                        selectedSprites[data.selectedLeftCard - 1] : disabledSprites[data.selectedLeftCard - 1];
                    var rightSprite = (data.selectedRightCard == submitted) ?
                        selectedSprites[data.selectedRightCard - 1] : disabledSprites[data.selectedRightCard - 1];

                    if (aiPanels[i] != null)
                    {
                        aiPanels[i].Setup(player.GetDisplayName(), data.victoryTokens, data.points, leftSprite, rightSprite);
                    }
                }
            }
        }
    }

    // determine round winner and assign points
    void DetermineWinner()
    {
        var submissions = GameManager.Instance?.GetCurrentSubmissions();
        if (submissions == null) return;

        CreateWinnerReasonUI();

        var uniqueCards = submissions
            .GroupBy(x => x.Value)
            .Where(g => g.Count() == 1)
            .OrderBy(g => g.Key)
            .ToList();

        if (uniqueCards.Any())
        {
            var winner = uniqueCards.First().First();
            var winningCard = uniqueCards.First().Key;

            GameManager.Instance?.AddPoint(winner.Key, winningCard);
            var winnerData = GameManager.Instance?.GetPlayerData(winner.Key);
            if (winnerData != null) winnerData.victoryTokens += 1;
            GameManager.Instance?.SetLastWinner(winner.Key);

            ShowWinnerText(winner.Key, winningCard, submissions);
        }
        else
        {
            ShowNoWinnerText(submissions);
        }

        RefreshAIPanelScore();
    }

    // refresh AI panel scores after winner determination
    void RefreshAIPanelScore()
    {
        var activeAI = GameManager.Instance?.GetActivePlayers().Where(p => p.IsAI()).ToList();

        for (int i = 0; i < activeAI.Count && i < aiPanels.Length; i++)
        {
            var player = activeAI[i];
            var data = GameManager.Instance?.GetPlayerData(player);

            if (aiPanels[i] && data != null)
            {
                aiPanels[i].Setup(player.GetDisplayName(),
                                 data.victoryTokens,
                                 data.points,
                                 aiPanels[i].leftCardImage.sprite,
                                 aiPanels[i].rightCardImage.sprite);
            }
        }
    }

    // show winner announcement text
    void ShowWinnerText(Player winner, int winningCard, Dictionary<Player, int> submissions)
    {
        List<string> messages = new List<string>();

        messages.Add($"{winner.GetDisplayName()} wins with card {winningCard}!");
        messages.Add($"{winner.GetDisplayName()} gets {winningCard} points!");
        messages.Add($"{winner.GetDisplayName()} gets 1 victory token!");

        StartCoroutine(TypewriterEffect(messages));
    }

    // show no winner announcement text
    void ShowNoWinnerText(Dictionary<Player, int> submissions)
    {
        List<string> messages = new List<string>();

        messages.Add("All cards are duplicated!");
        messages.Add("No winner this round!");
        messages.Add("No points or tokens awarded!");

        StartCoroutine(TypewriterEffect(messages));
    }

    // animate text with typewriter effect
    IEnumerator TypewriterEffect(List<string> messages)
    {
        if (reasonText == null) yield break;

        reasonText.text = "";
        string fullText = "";

        foreach (string message in messages)
        {
            for (int i = 0; i < message.Length; i++)
            {
                fullText += message[i];
                reasonText.text = fullText;
                yield return new WaitForSeconds(0.05f);
            }

            fullText += "\n";
            reasonText.text = fullText;

            yield return new WaitForSeconds(0.8f);
        }

        yield return new WaitForSeconds(1f);
        nextRoundButton.SetActive(true);
    }

    // proceed to next round or survival calculation
    public void ProceedToNextRound()
    {
        if (currentReasonUI != null)
        {
            Destroy(currentReasonUI);
        }
        GameManager.Instance?.ClearSubmissions();

        if (GameManager.Instance != null)
        {
            int round = GameManager.Instance.currentRound;
            bool isSurvival = new int[] { 3, 6, 9, 12, 18 }.Contains(round);

            if (isSurvival)
            {
                FindObjectOfType<SurvivalRoundManager>()?.ShowSurvivalCalculation();
                return;
            }
            if (round >= GameManager.Instance.maxRounds)
            {
                return;
            }

            GameManager.Instance.ClearSubmissions();
            GameManager.Instance.currentRound++;
        }

        nextRoundButton.SetActive(false);
        SceneManager.LoadScene("CardSelectionScene");
    }

    // create winner reason UI
    void CreateWinnerReasonUI()
    {
        if (winnerReasonPrefab != null && uiParent != null)
        {
            currentReasonUI = Instantiate(winnerReasonPrefab, uiParent);
            reasonText = currentReasonUI.GetComponentInChildren<TextMeshProUGUI>();

            if (reasonText != null)
            {
                reasonText.text = "";
            }
        }
    }
    #endregion

    #region Common Methods
    // create cards for the scene (available and disabled)
    void CreateCards(bool isDraggable)
    {
        if (GameManager.Instance == null) return;

        var playerData = GameManager.Instance.GetPlayerData(Player.Human);
        if (playerData == null) return;

        var availableCards = playerData.availableCards;
        var disabledCards = playerData.disabledCards;

        allCards.Clear();

        var allCardsToShow = new HashSet<int>(availableCards);
        foreach (int disabledCard in disabledCards)
        {
            allCardsToShow.Add(disabledCard);
        }

        var sortedCards = new List<int>(allCardsToShow);
        sortedCards.Sort();

        for (int i = 0; i < sortedCards.Count; i++)
        {
            int cardNumber = sortedCards[i];
            Vector3 position = GetCardPositionByIndex(i);

            CreateCard(cardNumber, position, isDraggable);

            if (allCards.Count > 0)
            {
                var currentCard = allCards[allCards.Count - 1];

                if (disabledCards.Contains(cardNumber))
                {
                    currentCard.SetDisabledState();
                }
                else
                {
                    currentCard.SetNormalState();
                }
            }
        }

        if (!isDraggable)
        {
            foreach (var card in allCards)
            {
                card.OnCardClicked += OnCardClicked_Selection;
            }
        }
    }

    // get card position by index
    Vector3 GetCardPositionByIndex(int index)
    {
        if (index < 4)
        {
            return new Vector3(-375f + index * 250f, 50f, 0);
        }
        else
        {
            return new Vector3(-375f + (index - 4) * 250f, -300f, 0);
        }
    }

    // create individual card
    void CreateCard(int cardNumber, Vector3 position, bool isDraggable, bool useCardPrefab = true)
    {
        GameObject prefabToUse = useCardPrefab ? cardPrefab : fieldCardPrefab;

        var cardObj = Instantiate(prefabToUse, cardParent);
        var card = cardObj.GetComponent<BaseCard>();

        card.SetCardNumber(cardNumber);

        if (normalSprites != null && cardNumber > 0 && cardNumber <= normalSprites.Length)
        {
            card.normalSprite = normalSprites[cardNumber - 1];
            if (selectedSprites != null && selectedSprites.Length >= cardNumber)
            {
                card.selectedSprite = selectedSprites[cardNumber - 1];
            }
            if (disabledSprites != null && disabledSprites.Length >= cardNumber)
            {
                card.disabledSprite = disabledSprites[cardNumber - 1];
            }
        }

        card.SetDraggable(isDraggable);

        var rectTransform = cardObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localPosition = position;
        }

        allCards.Add(card);
    }

    // clear all scene objects
    void ClearScene()
    {
        foreach (var card in allCards)
        {
            if (card) Destroy(card.gameObject);
        }
        allCards.Clear();

        if (resultCardParent)
        {
            for (int i = resultCardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(resultCardParent.GetChild(i).gameObject);
            }
        }
    }
    #endregion
}