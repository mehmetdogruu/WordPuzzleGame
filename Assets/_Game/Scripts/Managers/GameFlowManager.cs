using UnityEngine;
using TMPro;
using Helpers;

public class GameFlowManager : Singleton<GameFlowManager>
{
    [Header("Level")]
    public int currentLevelNumber = 1; // LevelManager ile senkron tut
    public int CurrentLevelNumber => currentLevelNumber;

    // BoardManager.CheckEndIfNoTiles() burayı çağıracak
    public void OnLevelCompletedNoTiles()
    {
        int totalScore = ScoreManager.InstanceExists ? ScoreManager.Instance.TotalScore : 0;

        // İlerlemeyi kaydet
        bool isNewHigh = Progress.TryUpdateHighScore(CurrentLevelNumber, totalScore);
        Progress.SetMaxCompletedIfGreater(CurrentLevelNumber);

        // Level listesi (popup) anında güncellensin
        if (LevelPopupController.Instance != null)
            LevelPopupController.Instance.Refresh();

        int nextLevel = CurrentLevelNumber + 1;
        Debug.Log($"[GameFlow] Level {CurrentLevelNumber} bitti. total={totalScore}, newHigh={isNewHigh}, nextPlayable={nextLevel}");

        if (WinUIController.Instance == null)
        {
            Debug.LogError("[GameFlow] winUI referansı atanmadı!");
            return;
        }

        WinUIController.Instance.ShowWin(totalScore, isNewHigh, nextLevel);
        GamePanelController.Instance.Hide();
    }

    //private bool SaveWinProgress(int currentLevel, int totalScore)
    //{
    //    // High Score (tek anahtar: level_{N}_highscore)
    //    string hsKey = LevelPopupController.HighScoreKey(currentLevel);
    //    int oldHS = PlayerPrefs.GetInt(hsKey, 0);
    //    bool isNewHigh = totalScore > oldHS;
    //    if (isNewHigh)
    //        PlayerPrefs.SetInt(hsKey, totalScore);

    //    // En yüksek tamamlanan level (tek anahtar: max_completed_level)
    //    int maxCompleted = PlayerPrefs.GetInt(LevelPopupController.Key_MaxCompleted, 0);
    //    if (currentLevel > maxCompleted)
    //        PlayerPrefs.SetInt(LevelPopupController.Key_MaxCompleted, currentLevel);

    //    PlayerPrefs.Save();
    //    return isNewHigh;
    //}



    // LevelManager BuildLevel’den sonra çağırmak için yardımcı (Inspector’dan da tetiklenebilir)
    public void SetCurrentLevel(int lvl) => currentLevelNumber = lvl;
}
