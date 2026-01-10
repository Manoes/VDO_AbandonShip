using UnityEngine;

public static class HighScoreSystem
{
    private static HighScoreService highScoreService;

    public static HighScoreService HighScoreService
    {
        get
        {
            if(highScoreService == null)
                highScoreService = new HighScoreService();
            return highScoreService;
        }
    }

    public static void Reload() => HighScoreService.Load();
}
