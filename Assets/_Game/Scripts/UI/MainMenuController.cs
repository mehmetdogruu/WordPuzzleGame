using UnityEngine;
using UnityEngine.UI;
using UISystem;   // UIController<T> i�in

public class MainMenuController : UIController<MainMenuController>
{
    [Header("UI Refs")]
    [SerializeField] private GameObject levelPopup; // Level se�me popup��
    [SerializeField] private Button levelsButton;   // �Levels� butonu
    [SerializeField] private Button closePopupButton; // (opsiyonel) kapatma butonu

    protected override void Awake()
    {
        base.Awake(); // _canvas ve _group burada haz�rlan�r

        if (levelsButton)
            levelsButton.onClick.AddListener(OpenLevelPopup);

        if (closePopupButton)
            closePopupButton.onClick.AddListener(CloseLevelPopup);

        //// Ba�lang��ta popup kapal�
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
        //    Debug.LogWarning("MainMenuController: LevelPopup atanmad�.");
    }

    private void CloseLevelPopup()
    {
        LevelPopupController.Instance.Hide();

        //if (levelPopup)
        //    levelPopup.SetActive(false);
    }

    /// <summary>
    /// Ana men�y� ekranda g�ster.
    /// </summary>
    public override void Show()
    {
        base.Show(); // UIController.Show() ile canvas aktif + alpha animasyonu
    }

    /// <summary>
    /// Ana men�y� gizle.
    /// </summary>
    public override void Hide()
    {
        base.Hide(); // UIController.Hide() ile animasyonlu kapan��
    }
}
