using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI victoryTokenText;
    public TextMeshProUGUI pointText;
    public Image leftCardImage;
    public Image rightCardImage;

    // setup panel with player information and card sprites
    public void Setup(string playerName, int tokens, int points, Sprite leftSprite, Sprite rightSprite)
    {
        if (playerNameText) playerNameText.text = playerName;
        if (victoryTokenText) victoryTokenText.text = $"Tokens: {tokens}";
        if (pointText) pointText.text = $"Points: {points}";
        if (leftCardImage) leftCardImage.sprite = leftSprite;
        if (rightCardImage) rightCardImage.sprite = rightSprite;
    }
}