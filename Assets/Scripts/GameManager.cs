using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public List<Player> activePlayers = new List<Player> { Player.Human, Player.AI_1, Player.AI_2, Player.AI_3 };
    public int currentRound = 1;
    public int maxRounds = 18;
    
    // Unified player data storage
    private Dictionary<Player, PlayerData> playerData = new Dictionary<Player, PlayerData>();
    
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerData
    {
        public int score = 0;
        public int victoryTokens = 0;
        public bool isEliminated = false;
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
        foreach (Player player in activePlayers)
        {
            playerData[player] = new PlayerData();
        }
    }

    // Unified player access methods
    public List<Player> GetActivePlayers() => activePlayers.Where(p => !playerData[p].isEliminated).ToList();
    public PlayerData GetPlayerData(Player player) => playerData.ContainsKey(player) ? playerData[player] : null;
    public void EliminatePlayer(Player player) => playerData[player].isEliminated = true;
    
    public void AddScore(Player player, int score)
    {
        playerData[player].score += score;
        Debug.Log($"{player.GetDisplayName()}: +{score} points");
    }

    public void GenerateAISelections()
    {
        foreach (Player player in GetActivePlayers().Where(p => p.IsAI()))
        {
            var data = playerData[player];
            var playable = data.GetPlayableCards();
            
            if (playable.Count >= 2)
            {
                var selected = playable.OrderBy(x => Random.value).Take(2).ToArray();
                data.selectedLeftCard = selected[0];
                data.selectedRightCard = selected[1];
            }
        }
    }

    public void ProcessSubmission(Player player, int submittedCard, int tempCard)
    {
        var data = playerData[player];
        data.availableCards.Remove(submittedCard);
        data.disabledCards.Add(tempCard);
    }

    public void ClearDisabledCards()
    {
        foreach (var data in playerData.Values)
        {
            data.disabledCards.Clear();
        }
    }
}
