using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinUIController : MonoBehaviour
{
    [Header("UI Refs")]
    public CanvasGroup root;          // Win panel (CanvasGroup)
    public TMP_Text titleText;        // "Yeni En Y�ksek Skor!" / "Level Tamamland�"
    public TMP_Text scoreText;        // "Score: 123"
    public Button nextLevelButton;    // Next Level

    [Header("FX (opsiyonel)")]
    public ParticleSystem highScoreFX;
    public ParticleSystem normalEndFX;

    // Dahili state
    private int _pendingNextLevel = 0;

    void Awake()
    {
        SetVisible(false);
        if (nextLevelButton) nextLevelButton.onClick.AddListener(OnNextLevelClicked);
    }

    public void ShowWin(int totalScore, bool isNewHigh, int nextLevelNumber)
    {
        _pendingNextLevel = nextLevelNumber;

        if (titleText)
            titleText.text = isNewHigh ? "Yeni En Y�ksek Skor!" : "Level Tamamland�";
        if (scoreText)
            scoreText.text = $"Score: {totalScore}";

        //if (isNewHigh) { if (normalEndFX) normalEndFX.Stop(); if (highScoreFX) highScoreFX.Play(); }
        //else { if (highScoreFX) highScoreFX.Stop(); if (normalEndFX) normalEndFX.Play(); }

        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    private void SetVisible(bool show)
    {
        if (!root) return;
        root.alpha = show ? 1f : 0f;
        root.blocksRaycasts = show;
        root.interactable = show;
    }

    private void OnNextLevelClicked()
    {
        // 1) Skoru s�f�rla
        ScoreManager.Instance?.ResetScore();

        // 2) LetterHolder�lar� temizle (varsa)
        LetterHolderManager.Instance?.ClearAllHoldersImmediate();

        // 3) Bir sonraki level�� y�kle
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.BuildLevel(_pendingNextLevel);
            GameFlowManager.Instance?.SetCurrentLevel(_pendingNextLevel);
        }
        else
        {
            Debug.LogError("LevelManager bulunamad�. Next level y�klenemedi.");
        }

        // 4) Win UI kapat
        Hide();
    }
}
