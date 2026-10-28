using UnityEngine;

public static class HighScoreRecord
{
    public static void Load(string levelKey, out int score, out float timeSeconds)
    {
        score = PlayerPrefs.GetInt(ScoreKey(levelKey), 0);
        timeSeconds = PlayerPrefs.GetFloat(TimeKey(levelKey), 0f);
    }

    public static bool SaveIfBetter(string levelKey, int score, float timeSeconds)
    {
        string scoreKey = ScoreKey(levelKey);
        bool hasRecord = PlayerPrefs.HasKey(scoreKey);
        int bestScore = PlayerPrefs.GetInt(scoreKey, 0);
        float bestTime = PlayerPrefs.GetFloat(TimeKey(levelKey), 0f);
        if (hasRecord && score < bestScore)
            return false;
        if (hasRecord && score == bestScore && timeSeconds >= bestTime)
            return false;

        PlayerPrefs.SetInt(scoreKey, score);
        PlayerPrefs.SetFloat(TimeKey(levelKey), timeSeconds);
        PlayerPrefs.Save();
        return true;
    }

    public static string FormatTime(float timeSeconds)
    {
        int centiseconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds * 100f));
        int hundredths = centiseconds % 100;
        int totalSeconds = centiseconds / 100;
        int seconds = totalSeconds % 60;
        int minutes = totalSeconds / 60;
        return minutes.ToString("00") + ":" + seconds.ToString("00") + ":" + hundredths.ToString("00");
    }

    static string ScoreKey(string levelKey)
    {
        return levelKey + ".score";
    }

    static string TimeKey(string levelKey)
    {
        return levelKey + ".time";
    }
}
