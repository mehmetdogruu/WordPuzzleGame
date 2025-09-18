using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UISystem;

public class WinUIController : UIController<WinUIController>
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button menuButton;          // Artık "Menu" butonu

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;   // Ana menü panel
    [SerializeField] private CanvasGroup mainMenuCanvas; // (opsiyonel)

    [Header("FX (opsiyonel)")]
    [SerializeField] private ParticleSystem highScoreFX;
    [SerializeField] private ParticleSystem normalEndFX;

    private int _pendingNextLevel;

    protected override void Awake()
    {
        base.Awake(); // _canvas ve _group burada ayarlanıyor
        if (menuButton) menuButton.onClick.AddListener(OnMenuClicked);
    }

    /// <summary>
    /// Kazanma ekranını skor ve highscore bilgileriyle gösterir.
    /// </summary>
    public void ShowWin(int totalScore, bool isNewHigh, int nextLevelNumber)
    {
        _pendingNextLevel = nextLevelNumber;

        if (titleText)
            titleText.text = isNewHigh ? "Yeni En Yüksek Skor!" : "Level Tamamlandı";

        if (scoreText)
            scoreText.text = $"Score: {totalScore}";

        // ✅ High Score / Level kilidi artık GameFlowManager tarafından yazılıyor.
        // Burada sadece efekt ve UI açma işlemleri yapılır.

        if (highScoreFX) highScoreFX.gameObject.SetActive(isNewHigh);
        if (normalEndFX) normalEndFX.gameObject.SetActive(!isNewHigh);

        Show(); // UIController.Show()
    }


    private void OnMenuClicked()
    {
        // Skor/holder temizliği
        ScoreManager.Instance?.ResetScore();
        LetterHolderManager.Instance?.ClearAllHoldersImmediate();

        // Win paneli kapat
        Hide();

        // Ana menüyü aç
        if (mainMenuCanvas)
        {
            mainMenuCanvas.alpha = 1f;
            mainMenuCanvas.blocksRaycasts = true;
            mainMenuCanvas.interactable = true;
            if (!mainMenuCanvas.gameObject.activeSelf)
                mainMenuCanvas.gameObject.SetActive(true);
        }
        else if (mainMenuPanel)
        {
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("WinUIController: mainMenuPanel / mainMenuCanvas atanmadı.");
        }
    }
}
