using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI gameLogText;

    private int numPlayers = 4;
    private int currentRound = 1;
    private List<Player> players = new List<Player>();
    private List<int> settlementRounds = new List<int> { 3, 6, 9, 12, 18 };
    private List<int> resetRounds = new List<int> { 6, 12 };
    private List<int> survivors = new List<int>();
    private int lastWinner = -1;

    private List<CardSelection> currentSelections = new List<CardSelection>();
    private bool roundInProgress = false;

    // Start is called before the first frame update
    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        for (int i = 0; i < numPlayers; i++)
        {
            Player player = new Player(i, i == 0);
            players.Add(player);
            CreatePlayerStatusUI(player);
        }

        UpdateUI();
        StartNewRound();
    }

    void StartNewRound()
    {
        if (currentRound > 18 || GetActivePlayers().Cound <= 1)
        {
            EndGame();
            return;
        }

        roundInProgress = true;
        ClearTempStorage();

        roundText.text = $"Round {currentRound}";
        AddToLog($"=== Round {currentRound} Start ===");

        DisplayPlayerHand();

        ProcessAISelections();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
