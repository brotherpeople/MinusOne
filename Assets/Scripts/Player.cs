using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Player : MonoBehaviour
{
    public int id;
    public bool isHuman;
    public List<int> cards;
    public List<int> tempStorage;
    public int score;
    public int victoryTokens;

    public Player(int playerId, bool human)
    {
        id = playerId;
        isHuman = human;
        cards = Enumerable.Range(1, 8).ToList();
        tempStorage = new List<int>();
        score = 0;
        victoryTokens = 0;
    }

    public List<int> GetAvailableCards()
    {
        return cards.Where(card => !tempStorage.Contains(card)).ToList();
    }

    public void ResetCards()
    {
        cards = Enumerable.Range(1, 8).ToList();
    }

    public void ClearTempStorage()
    {
        tempStorage.Clear();
    }
}
