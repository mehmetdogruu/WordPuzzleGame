using UnityEngine;

namespace Helpers
{
    /// <summary>
    /// Oyuncu ilerleme ve skor kay�tlar�n� tek noktadan y�neten s�n�f.
    /// </summary>
    public static class Progress
    {
        public const string KeyMaxCompleted = "max_completed_level";

        public static string HighScoreKey(int level) => $"level_{level}_highscore";

        /// <summary>�imdiye kadar bitirilen en y�ksek level</summary>
        public static int GetMaxCompleted(int firstUnlockedLevel)
            => PlayerPrefs.GetInt(KeyMaxCompleted, firstUnlockedLevel - 1);

        /// <summary>Oynanabilir en y�ksek level (tamamlanan + 1)</summary>
        public static int GetMaxPlayable(int firstUnlockedLevel)
        {
            int maxCompleted = GetMaxCompleted(firstUnlockedLevel);
            return Mathf.Max(firstUnlockedLevel, maxCompleted + 1);
        }

        /// <summary>Mevcut level tamamland�ysa ve daha y�ksekse kaydeder.</summary>
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

        /// <summary>High Score de�erini g�nceller.</summary>
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
