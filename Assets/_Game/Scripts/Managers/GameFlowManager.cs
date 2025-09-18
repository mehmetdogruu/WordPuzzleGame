using UnityEngine;
using TMPro;

public class GameFlowManager : SceneSingleton<GameFlowManager>
{
    [Header("Refs")]
    public WinUIController winUI;   // Inspector’dan ata

    [Header("Level")]
    public int currentLevelNumber = 1; // LevelManager ile senkron tut

    // BoardManager.CheckEndIfNoTiles() burayı çağıracak
    public void OnLevelCompletedNoTiles()
    {
        int total = ScoreManager.Instance != null ? ScoreManager.Instance.TotalScore : 0;

        string hsKey = $"HighScore_{currentLevelNumber}";
        int prev = PlayerPrefs.GetInt(hsKey, 0);
        bool isNewHigh = total > prev;
        if (isNewHigh) { PlayerPrefs.SetInt(hsKey, total); PlayerPrefs.Save(); }

        int nextLevel = currentLevelNumber + 1;

        Debug.Log($"[GameFlow] Level bitti. total={total}, newHigh={isNewHigh}, next={nextLevel}"); // 🔎

        if (winUI == null)
        {
            Debug.LogError("[GameFlow] winUI referansı atanmadı! Win UI açılamaz.");
            return;
        }

        winUI.ShowWin(total, isNewHigh, nextLevel);
    }


    // LevelManager BuildLevel’den sonra çağırmak için yardımcı (Inspector’dan da tetiklenebilir)
    public void SetCurrentLevel(int lvl) => currentLevelNumber = lvl;
}
