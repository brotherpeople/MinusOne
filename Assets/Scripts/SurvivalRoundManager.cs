using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SurvivalRoundManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject survivalPanel;
    public Button continueButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI resultText;

    [Header("Player Stat UI")]
    public Transform statsParent;
    public GameObject statPrefab;

    [Header("Animation Settings")]
    public float animationSpeed = 1f;
    public float textSpeed = 0.05f;

    [Header("Game Result UI")]
    public GameObject gameResultPanel;
    public TextMeshProUGUI gameResultText;
    public Button newGameButton;

    private readonly int[] SURVIVAL_ROUNDS = { 3, 6 };

    // show survival calculation panel and start the process
    public void ShowSurvivalCalculation()
    {
        survivalPanel.SetActive(true);
        continueButton.gameObject.SetActive(false);
        StartCoroutine(ProcessSurvival());
    }

    // process survival calculation with animations
    private IEnumerator ProcessSurvival()
    {
        CreatePlayerStats();
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(AnimateTokenConversion());

        Player survivor = DetermineSurvivor();
        HighlightSurvivor(survivor);
        yield return StartCoroutine(ShowSurvivorText(survivor));

        if (survivor == Player.Human)
        {
            yield return new WaitForSeconds(1f);
            ShowGameResult(true);
        }
        else if (GameManager.Instance.currentRound >= 6)
        {
            yield return new WaitForSeconds(1f);
            ShowGameResult(false);
        }
        else
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    // show final game result (win/lose)
    private void ShowGameResult(bool humanWon)
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);

            if (gameResultText != null)
            {
                if (humanWon)
                {
                    gameResultText.text = "=== YOU SURVIVED! ===\n\nCongratulations!\nYou have successfully survived the elimination rounds!";
                    gameResultText.color = Color.green;
                }
                else
                {
                    gameResultText.text = "=== GAME OVER ===\n\nYou failed to survive...\nBetter luck next time!";
                    gameResultText.color = Color.red;
                }
            }

            if (newGameButton != null)
            {
                newGameButton.gameObject.SetActive(true);
                newGameButton.onClick.RemoveAllListeners();
                newGameButton.onClick.AddListener(StartNewGame);
            }
        }
    }

    // start new game by resetting everything
    public void StartNewGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }

        SceneManager.LoadScene("CardSelectionScene");
    }

    // create player stat UI elements
    private void CreatePlayerStats()
    {
        for (int i = statsParent.childCount - 1; i >= 0; i--)
            Destroy(statsParent.GetChild(i).gameObject);

        var players = GameManager.Instance.GetActivePlayers();
        foreach (var player in players)
        {
            var statUI = Instantiate(statPrefab, statsParent);
            var data = GameManager.Instance.GetPlayerData(player);

            statUI.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text = player.GetDisplayName();
            statUI.transform.Find("Tokens").GetComponent<TextMeshProUGUI>().text = $"Tokens: {data.victoryTokens}";
            statUI.transform.Find("Points").GetComponent<TextMeshProUGUI>().text = $"Points: {data.points}";
            statUI.transform.Find("Total").GetComponent<TextMeshProUGUI>().text = $"Total: {data.points}";
        }
    }

    // animate victory token conversion to points
    private IEnumerator AnimateTokenConversion()
    {
        var players = GameManager.Instance.GetActivePlayers();

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var data = GameManager.Instance.GetPlayerData(player);
            var statUI = statsParent.GetChild(i);

            if (data.victoryTokens > 0)
            {
                var tokenText = statUI.Find("Tokens").GetComponent<TextMeshProUGUI>();
                tokenText.color = Color.yellow;
                tokenText.text = $"Tokens: +{data.victoryTokens}";

                var totalText = statUI.Find("Total").GetComponent<TextMeshProUGUI>();
                int startPoints = data.points;
                int endPoints = data.points + data.victoryTokens;

                float elapsed = 0f;
                while (elapsed < animationSpeed)
                {
                    elapsed += Time.deltaTime;
                    int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, endPoints, elapsed / animationSpeed));
                    totalText.text = $"Total: {currentPoints}";
                    yield return null;
                }

                data.points = endPoints;
                totalText.text = $"Total: {endPoints}";
                tokenText.color = Color.white;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    // determine survivor based on points, tokens, and last winner
    private Player DetermineSurvivor()
    {
        var players = GameManager.Instance.GetActivePlayers();
        var playerList = players.Select(p => new { Player = p, Data = GameManager.Instance.GetPlayerData(p) }).ToList();

        int maxPoints = playerList.Max(p => p.Data.points);
        var topScorers = playerList.Where(p => p.Data.points == maxPoints).ToList();
        if (topScorers.Count == 1) return topScorers[0].Player;

        int maxTokens = topScorers.Max(p => p.Data.victoryTokens);
        var topTokens = topScorers.Where(p => p.Data.victoryTokens == maxTokens).ToList();
        if (topTokens.Count == 1) return topTokens[0].Player;

        var lastWinner = GameManager.Instance.GetLastWinner();
        var winnerInContention = topTokens.FirstOrDefault(p => p.Player == lastWinner);
        if (winnerInContention != null) return winnerInContention.Player;

        return topTokens[0].Player;
    }

    // highlight survivor with green background
    private void HighlightSurvivor(Player survivor)
    {
        var players = GameManager.Instance.GetActivePlayers();
        for (int i = 0; i < players.Count; i++)
        {
            var statUI = statsParent.GetChild(i);
            var background = statUI.GetComponent<Image>();

            if (players[i] == survivor)
            {
                background.color = Color.green;
            }
        }
    }

    // show survivor announcement with typewriter effect
    private IEnumerator ShowSurvivorText(Player survivor)
    {
        string message;

        if (survivor == Player.Human)
        {
            message = $"{survivor.GetDisplayName()} SURVIVED!\n\nYou have secured your survival!";
        }
        else
        {
            message = $"{survivor.GetDisplayName()} SURVIVES!\n\nAll other players' points reset.\nTokens are preserved.";
        }

        resultText.text = "";
        for (int i = 0; i < message.Length; i++)
        {
            resultText.text += message[i];
            yield return new WaitForSeconds(textSpeed);
        }
    }

    // continue to next round after survival calculation
    public void OnContinueClicked()
    {
        Player survivor = DetermineSurvivor();

        GameManager.Instance.SetPlayerAsSurvivor(survivor);
        GameManager.Instance.ResetNonSurvivorPoints();

        int round = GameManager.Instance.currentRound;
        if (round == 6)
        {
            GameManager.Instance.ResetAllCards();
        }

        GameManager.Instance?.ClearSubmissions();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentRound++;
        }

        survivalPanel.SetActive(false);

        var sceneManager = FindObjectOfType<UnifiedSceneManager>();
        if (sceneManager != null)
        {
            sceneManager.nextRoundButton.SetActive(false);
        }
        SceneManager.LoadScene("CardSelectionScene");
    }
}