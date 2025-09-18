using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinUIController : MonoBehaviour
{
    [Header("UI Refs")]
    public CanvasGroup root;          // Win panel (CanvasGroup)
    public TMP_Text titleText;        // "Yeni En Yüksek Skor!" / "Level Tamamlandý"
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
            titleText.text = isNewHigh ? "Yeni En Yüksek Skor!" : "Level Tamamlandý";
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
        // 1) Skoru sýfýrla
        ScoreManager.Instance?.ResetScore();

        // 2) LetterHolder’larý temizle (varsa)
        LetterHolderManager.Instance?.ClearAllHoldersImmediate();

        // 3) Bir sonraki level’ý yükle
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.BuildLevel(_pendingNextLevel);
            GameFlowManager.Instance?.SetCurrentLevel(_pendingNextLevel);
        }
        else
        {
            Debug.LogError("LevelManager bulunamadý. Next level yüklenemedi.");
        }

        // 4) Win UI kapat
        Hide();
    }
}
