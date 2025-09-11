using System.Collections.Generic;

public enum Player
{
    Human = 0,
    AI_1 = 1,
    AI_2 = 2,
    AI_3 = 3
}

public static class PlayerExtensions
{
    public static bool IsAI(this Player player) => player != Player.Human;
    public static string GetDisplayName(this Player player) => player switch
    {
        Player.Human => "YOU",
        Player.AI_1 => "PLAYER 1",
        Player.AI_2 => "PLAYER 2", 
        Player.AI_3 => "PLAYER 3",
        _ => "UNKNOWN"
    };
    
    public static string GetShortName(this Player player) => player switch
    {
        Player.Human => "YOU",
        _ => $"P{(int)player}"
    };
}
