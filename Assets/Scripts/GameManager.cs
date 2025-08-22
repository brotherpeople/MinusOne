using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [Range(1, 3)]
    public int aiPlayerCount = 3;

    [Header("Round Settings")]
    public int currentRound = 1;
    public int maxRounds = 18;

    [Header("Player Status")]
    public int[] playerScores;
    public int[] victoryTokens;

    // AI Player card management
    [System.Serializable]
    public class PlayerCardData
    {
        public List<int> availableCards = new List<int>();
        public List<int> disabledCards = new List<int>();
        public int selectedLeftCard = 0;
        public int selectedRightCard = 0;

        public PlayerCardData()
        {
            // Initialize with cards 1-8
            for (int i = 1; i <= 8; i++)
            {
                availableCards.Add(i);
            }
        }
    }

    public PlayerCardData[] allPlayersCardData;

    public static GameManager Instance { get; private set; }

    public enum GamePhase
    {
        CardSelection,
        FieldPhase,
        ResultPhase
    }

    public GamePhase currentPhase = GamePhase.CardSelection;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeGame()
    {
        int totalPlayers = aiPlayerCount + 1;

        playerScores = new int[totalPlayers];
        victoryTokens = new int[totalPlayers];
        allPlayersCardData = new PlayerCardData[totalPlayers];

        for (int i = 0; i < totalPlayers; i++)
        {
            playerScores[i] = 0;
            victoryTokens[i] = 0;
            allPlayersCardData[i] = new PlayerCardData();
        }

        Debug.Log($"Game initialized - Total players: {totalPlayers} (AI: {aiPlayerCount})");
    }

    public void SetGamePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"Game phase changed to: {newPhase}");
    }

    public void NextRound()
    {
        currentRound++;
        SetGamePhase(GamePhase.CardSelection);
        Debug.Log($"Round {currentRound} started");
    }

    public void AddScore(int playerIndex, int score)
    {
        if (playerIndex >= 0 && playerIndex < playerScores.Length)
        {
            playerScores[playerIndex] += score;
            string playerType = (playerIndex == 0) ? "(You)" : "(AI)";
            Debug.Log($"Player {playerIndex + 1}{playerType}: +{score} points (Total: {playerScores[playerIndex]})");
        }
    }

    public void AddVictoryToken(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < victoryTokens.Length)
        {
            victoryTokens[playerIndex]++;
            string playerType = (playerIndex == 0) ? "(You)" : "(AI)";
            Debug.Log($"Player {playerIndex + 1}{playerType}: +1 victory token (Total: {victoryTokens[playerIndex]})");
        }
    }

    public int GetTotalPlayers()
    {
        return aiPlayerCount + 1;
    }

    public bool IsGameOver()
    {
        return currentRound > maxRounds;
    }

    // AI Card Selection for current round
    public void GenerateAISelections()
    {
        for (int playerIndex = 1; playerIndex < allPlayersCardData.Length; playerIndex++) // Skip human player (index 0)
        {
            PlayerCardData playerData = allPlayersCardData[playerIndex];

            // Get available cards (not disabled)
            List<int> availableCards = new List<int>();
            foreach (int card in playerData.availableCards)
            {
                if (!playerData.disabledCards.Contains(card))
                {
                    availableCards.Add(card);
                }
            }

            if (availableCards.Count >= 2)
            {
                // Simple AI: randomly select 2 different available cards
                int firstIndex = Random.Range(0, availableCards.Count);
                int firstCard = availableCards[firstIndex];
                availableCards.RemoveAt(firstIndex);

                int secondIndex = Random.Range(0, availableCards.Count);
                int secondCard = availableCards[secondIndex];

                playerData.selectedLeftCard = firstCard;
                playerData.selectedRightCard = secondCard;

                Debug.Log($"AI Player {playerIndex} selected: {firstCard}, {secondCard} (Available: {availableCards.Count + 2})");
            }
            else
            {
                Debug.LogWarning($"AI Player {playerIndex} doesn't have enough available cards!");
            }
        }
    }

    // Process card submission for a player
    public void ProcessCardSubmission(int playerIndex, int submittedCard, int tempStorageCard)
    {
        if (playerIndex >= 0 && playerIndex < allPlayersCardData.Length)
        {
            PlayerCardData playerData = allPlayersCardData[playerIndex];

            // Remove submitted card from available cards
            playerData.availableCards.Remove(submittedCard);

            // Add temp storage card to disabled list
            if (!playerData.disabledCards.Contains(tempStorageCard))
            {
                playerData.disabledCards.Add(tempStorageCard);
            }

            string playerType = (playerIndex == 0) ? "(You)" : "(AI)";
            Debug.Log($"Player {playerIndex + 1}{playerType}: Submitted {submittedCard}, Temp storage {tempStorageCard}");
        }
    }

    // Clear disabled cards for next round (after one round restriction)
    public void ClearDisabledCards()
    {
        foreach (PlayerCardData playerData in allPlayersCardData)
        {
            playerData.disabledCards.Clear();
        }
        Debug.Log("All disabled cards cleared for new round");
    }

    // Get player's available cards
    public List<int> GetAvailableCards(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < allPlayersCardData.Length)
        {
            PlayerCardData playerData = allPlayersCardData[playerIndex];
            List<int> available = new List<int>();

            foreach (int card in playerData.availableCards)
            {
                if (!playerData.disabledCards.Contains(card))
                {
                    available.Add(card);
                }
            }
            return available;
        }
        return new List<int>();
    }
}