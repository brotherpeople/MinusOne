using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public List<Player> activePlayers = new List<Player> { Player.Human, Player.AI_1, Player.AI_2, Player.AI_3 };
    public int currentRound = 1;
    public int maxRounds = 6;
    [Header("Survival Tracking")]
    private Player lastWinner = Player.Human;
    [Header("Round Data")]
    private Dictionary<Player, int> currentRoundSubmissions = new Dictionary<Player, int>();

    // Unified player data storage
    private Dictionary<Player, PlayerData> playerData = new Dictionary<Player, PlayerData>();

    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerData
    {
        public int points = 0;
        public int victoryTokens = 0;
        public bool isEliminated = false;
        public bool isSurvivor = false;
        public List<int> availableCards = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
        public List<int> disabledCards = new List<int>();
        public int selectedLeftCard = 0;
        public int selectedRightCard = 0;

        public List<int> GetPlayableCards() => availableCards.Where(c => !disabledCards.Contains(c)).ToList();

    }

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
        PlayerPrefs.DeleteKey("CurrentRound");
        PlayerPrefs.DeleteKey("LeftCard");
        PlayerPrefs.DeleteKey("RightCard");
        PlayerPrefs.Save();

        currentRound = 1;

        foreach (Player player in activePlayers)
        {
            playerData[player] = new PlayerData();
            Debug.Log($"Initialized {player.GetDisplayName()} with cards: {string.Join(", ", playerData[player].availableCards)}");
        }
    }

    // Reset game completely (for new game)
    public void ResetGame()
    {
        // Clear all PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        currentRound = 1;
        currentRoundSubmissions.Clear();

        // Reset all player data
        foreach (Player player in activePlayers)
        {
            playerData[player] = new PlayerData();
        }

        Debug.Log("Game completely reset");
    }
    public bool IsGameOver()
    {
        return currentRound > maxRounds && !playerData[Player.Human].isSurvivor;
    }

    public bool IsHumanSurvivor()
    {
        return playerData[Player.Human].isSurvivor;
    }

    public bool IsGameCompleted()
    {
        return IsHumanSurvivor() || IsGameOver();
    }

    public bool ShouldShowSurvivalRound()
    {
        return currentRound == 3 || currentRound == 6;
    }

    // Unified player access methods
    public List<Player> GetActivePlayers()
    {
        return activePlayers.Where(p => !playerData[p].isEliminated && !playerData[p].isSurvivor).ToList();
    }
    public PlayerData GetPlayerData(Player player) => playerData.ContainsKey(player) ? playerData[player] : null;
    public void EliminatePlayer(Player player) => playerData[player].isEliminated = true;
    public void SetLastWinner(Player winner) => lastWinner = winner;
    public Player GetLastWinner() => lastWinner;


    public void AddPoint(Player player, int points)
    {
        playerData[player].points += points;
        Debug.Log($"{player.GetDisplayName()}: +{points} points (Total: {playerData[player].points})");
    }

    public void GenerateAISelections()
    {
        foreach (Player player in GetActivePlayers().Where(p => p.IsAI()))
        {
            var data = playerData[player];
            var playable = data.GetPlayableCards();

            // Debug.Log($"AI {player.GetDisplayName()} playable cards: {string.Join(", ", playable)}");

            if (playable.Count >= 2)
            {
                // Use HashSet to avoid duplicates
                var selected = new HashSet<int>();
                while (selected.Count < 2)
                {
                    int randomCard = playable[Random.Range(0, playable.Count)];
                    selected.Add(randomCard);
                }

                var selectedArray = selected.ToArray();
                data.selectedLeftCard = selectedArray[0];
                data.selectedRightCard = selectedArray[1];

                Debug.Log($"AI {player.GetDisplayName()} selected: Left={data.selectedLeftCard}, Right={data.selectedRightCard}");
            }
        }
    }

    public void ProcessSubmission(Player player, int submittedCard, int tempCard)
    {
        var data = playerData[player];

        // Debug.Log($"=== {player.GetDisplayName()} Submission Processing ===");
        // Debug.Log($"Before - Available: [{string.Join(",", data.availableCards)}]");
        // Debug.Log($"Before - Disabled: [{string.Join(",", data.disabledCards)}]");
        // Debug.Log($"Submitted: {submittedCard}, Temp: {tempCard}");

        bool removed = data.availableCards.Remove(submittedCard);
        Debug.Log($"Removed {submittedCard} from available: {removed}");

        data.disabledCards.Clear();
        Debug.Log("Cleared previous disabled cards");

        if (tempCard > 0)
        {
            data.disabledCards.Add(tempCard);
            Debug.Log($"Added {tempCard} to disabled (next round only)");
        }

        Debug.Log($"After - Available: [{string.Join(",", data.availableCards)}]");
        Debug.Log($"After - Disabled: [{string.Join(",", data.disabledCards)}]");
        Debug.Log($"=== End {player.GetDisplayName()} Processing ===");
    }
    public void ClearDisabledCards()
    {
        foreach (var kvp in playerData)
        {
            Debug.Log($"Clearing disabled cards for {kvp.Key.GetDisplayName()}: [{string.Join(",", kvp.Value.disabledCards)}]");
            kvp.Value.disabledCards.Clear();
        }
        Debug.Log("All disabled cards cleared for next round");
    }
    public void SetPlayerSubmission(Player player, int cardNumber)
    {
        currentRoundSubmissions[player] = cardNumber;
        Debug.Log($"{player.GetDisplayName()} submitted card: {cardNumber}");
    }
    public Dictionary<Player, int> GetCurrentSubmissions()
    {
        return new Dictionary<Player, int>(currentRoundSubmissions);
    }
    public void ClearSubmissions()
    {
        currentRoundSubmissions.Clear();
    }

    public void SetPlayerAsSurvivor(Player player)
    {
        playerData[player].isSurvivor = true;
    }

    public void ResetNonSurvivorPoints()
    {
        foreach (var player in GetActivePlayers())
        {
            playerData[player].points = 0;
        }
    }

    public void ResetAllCards()
    {
        foreach (var player in GetActivePlayers())
        {
            var data = playerData[player];
            data.availableCards = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            data.disabledCards.Clear();
        }
    }


}
