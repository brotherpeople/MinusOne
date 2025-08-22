using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI victoryTokenText;
    public Image leftCardImage;
    public Image rightCardImage;

    public void SetupPlayer(string playerName, int victoryTokens, Sprite leftCardSprite, Sprite rightCardSprite)
    {
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        // Set victory tokens
        if (victoryTokenText != null)
        {
            victoryTokenText.text = $"Tokens: {victoryTokens}";
        }

        // Set actual card sprites
        if (leftCardImage != null && leftCardSprite != null)
        {
            leftCardImage.sprite = leftCardSprite;
        }

        if (rightCardImage != null && rightCardSprite != null)
        {
            rightCardImage.sprite = rightCardSprite;
        }
    }

    // Overload method for backward compatibility (using card back)
    public void SetupPlayer(string playerName, int victoryTokens, Sprite cardBackSprite)
    {
        SetupPlayer(playerName, victoryTokens, cardBackSprite, cardBackSprite);
    }
}