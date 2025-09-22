using UnityEngine;

namespace Helpers
{
    public static class Progress
    {
        public const string KeyMaxCompleted = "max_completed_level";

        public static string HighScoreKey(int level) => $"level_{level}_highscore";

        public static int GetMaxCompleted(int firstUnlockedLevel)
            => PlayerPrefs.GetInt(KeyMaxCompleted, firstUnlockedLevel - 1);

        public static int GetMaxPlayable(int firstUnlockedLevel)
        {
            int maxCompleted = GetMaxCompleted(firstUnlockedLevel);
            return Mathf.Max(firstUnlockedLevel, maxCompleted + 1);
        }

        public static bool SetMaxCompletedIfGreater(int level)
        {
            int old = PlayerPrefs.GetInt(KeyMaxCompleted, 0);
            if (level > old)
            {
                PlayerPrefs.SetInt(KeyMaxCompleted, level);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public static bool TryUpdateHighScore(int level, int score)
        {
            string key = HighScoreKey(level);
            int old = PlayerPrefs.GetInt(key, 0);
            if (score > old)
            {
                PlayerPrefs.SetInt(key, score);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }
    }
}
