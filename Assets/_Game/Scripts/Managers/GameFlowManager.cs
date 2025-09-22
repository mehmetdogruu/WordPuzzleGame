using UnityEngine;
using Helpers;

public class GameFlowManager : Singleton<GameFlowManager>
{
    [Header("Level")]
    public int currentLevelNumber = 1;
    public int CurrentLevelNumber => currentLevelNumber;

    public void SetCurrentLevel(int lvl) => currentLevelNumber = lvl;

    public void OnLevelCompletedNoTiles()
    {
        int totalScore = ScoreManager.InstanceExists ? ScoreManager.Instance.TotalScore : 0;

        bool isNewHigh = Progress.TryUpdateHighScore(CurrentLevelNumber, totalScore);
        Progress.SetMaxCompletedIfGreater(CurrentLevelNumber);

        LevelPopupController.Instance?.Refresh();

        int nextLevel = CurrentLevelNumber + 1;
        Debug.Log($"[GameFlow] Level {CurrentLevelNumber} bitti. total={totalScore}, newHigh={isNewHigh}, nextPlayable={nextLevel}");

        if (WinUIController.Instance == null)
        {
            Debug.LogError("[GameFlow] winUI referansı atanmadı!");
            return;
        }

        WinUIController.Instance.ShowWin(totalScore, isNewHigh, nextLevel);
        GamePanelController.Instance?.Hide();
    }
}
