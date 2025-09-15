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

    // ensure singleton instance and initialize game
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

    // initialize game data and clear saved preferences
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
        }
    }

    // reset entire game to initial state
    public void ResetGame()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        currentRound = 1;
        currentRoundSubmissions.Clear();

        foreach (Player player in activePlayers)
        {
            playerData[player] = new PlayerData();
        }
    }

    // check if game is over (human failed to survive)
    public bool IsGameOver() => currentRound > maxRounds && !playerData[Player.Human].isSurvivor;

    // check if human player survived
    public bool IsHumanSurvivor() => playerData[Player.Human].isSurvivor;

    // check if game is completed (win or lose)
    public bool IsGameCompleted() => IsHumanSurvivor() || IsGameOver();

    // check if current round is a survival round
    public bool ShouldShowSurvivalRound() => currentRound == 3 || currentRound == 6;

    // get list of players who are still active (not eliminated or survived)
    public List<Player> GetActivePlayers()
    {
        return activePlayers.Where(p => !playerData[p].isEliminated && !playerData[p].isSurvivor).ToList();
    }

    // get player data for specific player
    public PlayerData GetPlayerData(Player player) => playerData.ContainsKey(player) ? playerData[player] : null;

    // eliminate player from game
    public void EliminatePlayer(Player player) => playerData[player].isEliminated = true;

    // set last round winner
    public void SetLastWinner(Player winner) => lastWinner = winner;

    // get last round winner
    public Player GetLastWinner() => lastWinner;

    // add points to player
    public void AddPoint(Player player, int points)
    {
        playerData[player].points += points;
    }

    // generate random card selections for AI players
    public void GenerateAISelections()
    {
        foreach (Player player in GetActivePlayers().Where(p => p.IsAI()))
        {
            var data = playerData[player];
            var playable = data.GetPlayableCards();

            if (playable.Count >= 2)
            {
                var selected = new HashSet<int>();
                while (selected.Count < 2)
                {
                    int randomCard = playable[Random.Range(0, playable.Count)];
                    selected.Add(randomCard);
                }

                var selectedArray = selected.ToArray();
                data.selectedLeftCard = selectedArray[0];
                data.selectedRightCard = selectedArray[1];
            }
        }
    }

    // process player's card submission (remove used card, add temp card to disabled)
    public void ProcessSubmission(Player player, int submittedCard, int tempCard)
    {
        var data = playerData[player];

        data.availableCards.Remove(submittedCard);
        data.disabledCards.Clear();

        if (tempCard > 0)
        {
            data.disabledCards.Add(tempCard);
        }
    }

    // clear disabled cards for all players (used between rounds)
    public void ClearDisabledCards()
    {
        foreach (var kvp in playerData)
        {
            kvp.Value.disabledCards.Clear();
        }
    }

    // set player's submitted card for current round
    public void SetPlayerSubmission(Player player, int cardNumber)
    {
        currentRoundSubmissions[player] = cardNumber;
    }

    // get all current round submissions
    public Dictionary<Player, int> GetCurrentSubmissions()
    {
        return new Dictionary<Player, int>(currentRoundSubmissions);
    }

    // clear all round submissions
    public void ClearSubmissions()
    {
        currentRoundSubmissions.Clear();
    }

    // mark player as survivor
    public void SetPlayerAsSurvivor(Player player)
    {
        playerData[player].isSurvivor = true;
    }

    // reset points for all non-survivor players
    public void ResetNonSurvivorPoints()
    {
        foreach (var player in GetActivePlayers())
        {
            playerData[player].points = 0;
        }
    }

    // reset all players' cards to initial state (1-8)
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