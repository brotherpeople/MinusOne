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

    public void Setup(string playerName, int tokens, Sprite leftSprite, Sprite rightSprite)
    {
        if (playerNameText) playerNameText.text = playerName;
        if (victoryTokenText) victoryTokenText.text = $"TOKENS: {tokens}";
        if (leftCardImage) leftCardImage.sprite = leftSprite;
        if (rightCardImage) rightCardImage.sprite = rightSprite;
    }
}
