using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UISystem;
using Helpers;

public class WinUIController : UIController<WinUIController>
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button menuButton;          

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;   
    [SerializeField] private CanvasGroup mainMenuCanvas; 

    [Header("FX (opsiyonel)")]
    [SerializeField] private ParticleSystem[] highScoreFX;
    [SerializeField] private ParticleSystem[] normalEndFX;

    private int _pendingNextLevel;

    protected override void Awake()
    {
        base.Awake(); 
        if (menuButton) menuButton.onClick.AddListener(OnMenuClicked);
    }

    public void ShowWin(int totalScore, bool isNewHigh, int nextLevelNumber)
    {
        _pendingNextLevel = nextLevelNumber;

        if (titleText)
            titleText.text = isNewHigh ? "NEW HIGH SCORE!" : "Level Completed";

        if (scoreText)
            scoreText.text = $"Score: {totalScore}";

        Show(); 
        if (isNewHigh) TriggerParticles(highScoreFX);
        else TriggerParticles(normalEndFX);
    }


    private void OnMenuClicked()
    {
        ScoreManager.Instance?.ResetScore();
        LetterHolderManager.Instance?.ClearAllHoldersImmediate();

        Hide();

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
    private void TriggerParticles(ParticleSystem[] particles)
    {
      
        {
            foreach (var item in particles)
            {
                item.Play();
            }
        }
    }
}
