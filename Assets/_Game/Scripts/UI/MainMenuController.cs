using UnityEngine;
using UnityEngine.UI;
using UISystem;   // UIController<T> için

public class MainMenuController : UIController<MainMenuController>
{
    [Header("UI Refs")]
    [SerializeField] private GameObject levelPopup; // Level seçme popup’ý
    [SerializeField] private Button levelsButton;   // “Levels” butonu
    [SerializeField] private Button closePopupButton; // (opsiyonel) kapatma butonu

    protected override void Awake()
    {
        base.Awake(); // _canvas ve _group burada hazýrlanýr

        if (levelsButton)
            levelsButton.onClick.AddListener(OpenLevelPopup);

        if (closePopupButton)
            closePopupButton.onClick.AddListener(CloseLevelPopup);

        //// Baþlangýçta popup kapalý
        //if (levelPopup)
        //    levelPopup.SetActive(false);
    }

    private void OpenLevelPopup()
    {
        Hide();
        LevelPopupController.Instance.Show();
        //if (levelPopup)
        //    levelPopup.SetActive(true);
        //else
        //    Debug.LogWarning("MainMenuController: LevelPopup atanmadý.");
    }

    private void CloseLevelPopup()
    {
        LevelPopupController.Instance.Hide();

        //if (levelPopup)
        //    levelPopup.SetActive(false);
    }

    /// <summary>
    /// Ana menüyü ekranda göster.
    /// </summary>
    public override void Show()
    {
        base.Show(); // UIController.Show() ile canvas aktif + alpha animasyonu
    }

    /// <summary>
    /// Ana menüyü gizle.
    /// </summary>
    public override void Hide()
    {
        base.Hide(); // UIController.Hide() ile animasyonlu kapanýþ
    }
}
