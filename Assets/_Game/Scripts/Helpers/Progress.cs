using UnityEngine;

namespace Helpers
{
    /// <summary>
    /// Oyuncu ilerleme ve skor kayýtlarýný tek noktadan yöneten sýnýf.
    /// </summary>
    public static class Progress
    {
        public const string KeyMaxCompleted = "max_completed_level";

        public static string HighScoreKey(int level) => $"level_{level}_highscore";

        /// <summary>Þimdiye kadar bitirilen en yüksek level</summary>
        public static int GetMaxCompleted(int firstUnlockedLevel)
            => PlayerPrefs.GetInt(KeyMaxCompleted, firstUnlockedLevel - 1);

        /// <summary>Oynanabilir en yüksek level (tamamlanan + 1)</summary>
        public static int GetMaxPlayable(int firstUnlockedLevel)
        {
            int maxCompleted = GetMaxCompleted(firstUnlockedLevel);
            return Mathf.Max(firstUnlockedLevel, maxCompleted + 1);
        }

        /// <summary>Mevcut level tamamlandýysa ve daha yüksekse kaydeder.</summary>
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

        /// <summary>High Score deðerini günceller.</summary>
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
