using UnityEngine;
using UnityEngine.UI;
using UISystem;

public class MainMenuController : UIController<MainMenuController>
{
    [Header("UI Refs")]
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button closePopupButton;

    protected override void Awake()
    {
        base.Awake();
        if (levelsButton) levelsButton.onClick.AddListener(OpenLevelPopup);
        if (closePopupButton) closePopupButton.onClick.AddListener(CloseLevelPopup);
    }

    void Start()
    {
        // ✅ Oyun açılışında ana menü paneli açık
        ShowInstant();
    }

    private void OpenLevelPopup()
    {
        Hide();
        LevelPopupController.Instance?.Open(); // Build + Show
    }

    private void CloseLevelPopup()
    {
        LevelPopupController.Instance?.Hide();
        Show(); // menüye geri dön
    }
}
