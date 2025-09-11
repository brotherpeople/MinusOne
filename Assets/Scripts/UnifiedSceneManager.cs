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
    public GameObject cardPrefab; // Single prefab for all scenes
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
    
    [Header("Result Scene")]
    public Transform resultCardParent;
    public PlayerInfoPanel[] aiPanels = new PlayerInfoPanel[3];
    
    [Header("Round Display")]
    public GameObject roundDisplayPanel;
    public TextMeshProUGUI roundNumberText;

    public enum SceneType { CardSelection, Field, Result }

    private List<BaseCard> allCards = new List<BaseCard>();
    private BaseCard leftZoneCard = null;
    private BaseCard rightZoneCard = null;
    private BaseCard selectedCard = null;
    private Dictionary<Player, int> finalSubmissions = new Dictionary<Player, int>();

    void Start()
    {
        switch (currentScene)
        {
            case SceneType.CardSelection: StartCardSelection(); break;
            case SceneType.Field: StartFieldScene(); break;
            case SceneType.Result: StartResultScene(); break;
        }
    }

    #region Card Selection Scene (isDraggable = false)
    void StartCardSelection()
    {
        StartCoroutine(ShowRoundNumber());
        CreateCards(isDraggable: false);
        confirmButton?.onClick.AddListener(OnConfirmClicked);
        GameManager.Instance?.GenerateAISelections();
    }

    IEnumerator ShowRoundNumber()
    {
        if (roundDisplayPanel)
        {
            roundDisplayPanel.SetActive(true);
            CanvasGroup canvasGroup = roundDisplayPanel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(FadeOut(canvasGroup, 0.5f));

            int round = GameManager.Instance?.currentRound ?? 1;
            bool isSettlement = new int[] { 3, 6, 9, 12, 18 }.Contains(round);
            roundNumberText.text = isSettlement ? $"ROUND {round}\n* SURVIVAL ROUND *" : $"ROUND {round}";
            roundDisplayPanel.SetActive(false);
        }
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

    void OnConfirmClicked()
    {
        PlayerPrefs.SetInt("LeftCard", leftZoneCard.GetCardNumber());
        PlayerPrefs.SetInt("RightCard", rightZoneCard.GetCardNumber());
        PlayerPrefs.SetInt("CurrentRound", GameManager.Instance?.currentRound ?? 1);
        
        currentScene = SceneType.Field;
        ClearScene();
        SceneManager.LoadScene("FieldScene");
        StartFieldScene();
    }
    #endregion

    #region Field Scene (isDraggable = true)
    void StartFieldScene()
    {
        CreateFieldCards();
        SetupAIPanels();
        SetupSubmitArea();
    }

    void CreateFieldCards()
    {
        int leftCard = PlayerPrefs.GetInt("LeftCard", 1);
        int rightCard = PlayerPrefs.GetInt("RightCard", 2);

        CreateCard(leftCard, new Vector3(-180, 130, 0), isDraggable: true, useCardPrefab: true);
        CreateCard(rightCard, new Vector3(180, 130, 0), isDraggable: true, useCardPrefab: true);

        // Set up field-specific events
        foreach (var card in allCards)
        {
            card.OnCardClicked += OnCardClicked_Field;
            card.OnDragStart += OnDragStart;
            card.OnDragEnd += OnDragEnd;
        }
    }

    void OnCardClicked_Field(BaseCard card)
    {
        if (card.IsDisabled()) return;
        
        if (selectedCard == card)
        {
            selectedCard = null;
            card.SetNormalState();
            var otherCard = allCards.FirstOrDefault(c => c != card);
            otherCard?.SetNormalState();
        }
        else
        {
            selectedCard = card;
            card.SetSelectedState();
            var otherCard = allCards.FirstOrDefault(c => c != card);
            otherCard?.SetDisabledState();
        }
    }

    void OnDragStart(BaseCard card)
    {
        selectedCard = card;
        if (submitAreaImage) submitAreaImage.color = highlightAreaColor;
        
        var otherCard = allCards.FirstOrDefault(c => c != card);
        otherCard?.SetDisabledState();
    }

    void OnDragEnd(BaseCard card)
    {
        Vector2 originalPos = card.GetComponent<RectTransform>().localPosition;

        if (submitAreaImage) submitAreaImage.color = normalAreaColor;
        
        if (IsCardInSubmitArea(card))
        {
            ProcessSubmission(card);
        }
        else
        {
            card.MoveToPosition(originalPos);
            var otherCard = allCards.FirstOrDefault(c => c != card);
            otherCard?.SetNormalState();
            selectedCard = null;
        }
    }

    bool IsCardInSubmitArea(BaseCard card)
    {
        if (!submitArea) return false;
        var cardRect = card.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(submitArea, cardRect.position);
    }

    void ProcessSubmission(BaseCard card)
    {
        int submitted = card.GetCardNumber();
        int temp = allCards.FirstOrDefault(c => c != card)?.GetCardNumber() ?? 0;
        
        GameManager.Instance?.ProcessSubmission(Player.Human, submitted, temp);
        card.MoveToPosition(new Vector3(0, 600, 0));
        
        ProcessAISubmissions();
        
        currentScene = SceneType.Result;
        ClearScene();
        SceneManager.LoadScene("ResultScene");
        StartResultScene();
    }

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
                finalSubmissions[player] = submitted;
            }
        }
        
        finalSubmissions[Player.Human] = selectedCard?.GetCardNumber() ?? 0;
    }

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
                aiPanels[i].Setup(player.GetDisplayName(), data.victoryTokens, leftSprite, rightSprite);
            }
        }
    }

    void SetupSubmitArea()
    {
        if (submitAreaImage) submitAreaImage.color = normalAreaColor;
    }
    #endregion

    #region Result Scene (isDraggable = false)
    void StartResultScene()
    {
        CreateResultCards();
        UpdateAIPanels();
    }

    void CreateResultCards()
    {
        var activePlayers = GameManager.Instance?.GetActivePlayers() ?? new List<Player> { Player.Human };
        
        for (int i = 0; i < activePlayers.Count; i++)
        {
            var player = activePlayers[i];
            if (finalSubmissions.ContainsKey(player))
            {
                CreateResultCard(player, finalSubmissions[player], i);
            }
        }
    }

    void CreateResultCard(Player player, int cardNumber, int position)
    {
        var cardObj = Instantiate(fieldCardPrefab, resultCardParent);
        var card = cardObj.GetComponent<BaseCard>();
        
        card.SetCardNumber(cardNumber);
        card.cardImage.sprite = normalSprites[cardNumber - 1];
        card.SetDraggable(false); // Result cards are not draggable
        card.GetComponent<Button>().interactable = false;
        
        var rect = cardObj.GetComponent<RectTransform>();
        rect.localPosition = new Vector3(-300f + position * 160f, -40f, 0f);
        
    }

    void UpdateAIPanels()
    {
        var activeAI = GameManager.Instance?.GetActivePlayers().Where(p => p.IsAI()).ToList() ?? new List<Player>();
        
        for (int i = 0; i < activeAI.Count && i < aiPanels.Length; i++)
        {
            var player = activeAI[i];
            var data = GameManager.Instance?.GetPlayerData(player);
            
            if (aiPanels[i] && data != null && finalSubmissions.ContainsKey(player))
            {
                int submitted = finalSubmissions[player];
                var leftSprite = (data.selectedLeftCard == submitted) ? 
                    selectedSprites[data.selectedLeftCard - 1] : disabledSprites[data.selectedLeftCard - 1];
                var rightSprite = (data.selectedRightCard == submitted) ? 
                    selectedSprites[data.selectedRightCard - 1] : disabledSprites[data.selectedRightCard - 1];
                
                aiPanels[i].Setup(player.GetDisplayName(), data.victoryTokens, leftSprite, rightSprite);
            }
        }
    }
    #endregion

    #region Common Methods
    void CreateCards(bool isDraggable)
    {
        var availableCards = GameManager.Instance?.GetPlayerData(Player.Human)?.GetPlayableCards() 
                           ?? new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };

        for (int i = 0; i < 8; i++)
        {
            CreateCard(i + 1, GetCardPosition(i), isDraggable);
            
            if (!availableCards.Contains(i + 1))
                allCards[i].SetDisabledState();
        }

        // Set up card selection events
        if (!isDraggable)
        {
            foreach (var card in allCards)
            {
                card.OnCardClicked += OnCardClicked_Selection;
            }
        }
    }

 void CreateCard(int cardNumber, Vector3 position, bool isDraggable, bool useCardPrefab = true)
    {
        // Choose prefab based on size requirement
        GameObject prefabToUse = useCardPrefab ? cardPrefab : fieldCardPrefab;
        var cardObj = Instantiate(prefabToUse, cardParent);
        var card = cardObj.GetComponent<BaseCard>();
        
        card.SetCardNumber(cardNumber);
        card.normalSprite = normalSprites[cardNumber - 1];
        card.selectedSprite = selectedSprites[cardNumber - 1];
        card.disabledSprite = disabledSprites[cardNumber - 1];
        card.SetDraggable(isDraggable);
        
        cardObj.GetComponent<RectTransform>().localPosition = position;
        allCards.Add(card);
    }

    Vector3 GetCardPosition(int index)
    {
        return index < 4 ? 
            new Vector3(-375f + index * 250f, 50f, 0) : 
            new Vector3(-375f + (index-4) * 250f, -300f, 0);
    }

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