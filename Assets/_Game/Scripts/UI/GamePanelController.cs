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
        HideInstant();   // Oyun ba��nda gizli ba�las�n
        if (exitButton) exitButton.onClick.AddListener(ExitButton);

    }

    // UIController.Show / Hide zaten animasyonlu a�-kapa i�eriyor
    // Gerekirse burada ekstra haz�rl�k yapabilirsin.
    public override void Show()
    {
        base.Show();
        Debug.Log("[GamePanel] G�sterildi");
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
