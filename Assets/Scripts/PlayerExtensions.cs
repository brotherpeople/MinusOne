public enum Player
{
    Human = 0,
    AI_1 = 1,
    AI_2 = 2,
    AI_3 = 3
}

public static class PlayerExtensions
{
    // check if player is AI (not human)
    public static bool IsAI(this Player player) => player != Player.Human;

    // get full display name for player
    public static string GetDisplayName(this Player player) => player switch
    {
        Player.Human => "YOU",
        Player.AI_1 => "PLAYER 1",
        Player.AI_2 => "PLAYER 2",
        Player.AI_3 => "PLAYER 3",
        _ => "UNKNOWN"
    };

    // get short name for player (used in UI)
    public static string GetShortName(this Player player) => player switch
    {
        Player.Human => "YOU",
        _ => $"P{(int)player}"
    };
}