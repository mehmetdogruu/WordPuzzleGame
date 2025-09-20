using UnityEngine;
using UISystem;
using TMPro;
using UnityEngine.UI;

public class GamePanelController : UIController<GamePanelController>
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button exitButton;

    protected override void Awake()
    {
        base.Awake();
        HideInstant();   // Oyun baþýnda gizli baþlasýn
        if (exitButton) exitButton.onClick.AddListener(ExitButton);

    }

    // UIController.Show / Hide zaten animasyonlu aç-kapa içeriyor
    // Gerekirse burada ekstra hazýrlýk yapabilirsin.
    public override void Show()
    {
        base.Show();
        Debug.Log("[GamePanel] Gösterildi");
        scoreText.gameObject.SetActive(true);

    }
    public override void Hide()
    {
        base.Hide();
        scoreText.gameObject.SetActive(false);

    }
    private void ExitButton()
    {
        Hide();
        MainMenuController.Instance.Show();
        LetterHolderManager.Instance.ResetAll();

    }
}
