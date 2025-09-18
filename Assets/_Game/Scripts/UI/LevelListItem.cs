using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelListItem : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;     // "Level 1 - ANIMALS"
    [SerializeField] private TMP_Text highScoreText; // "High Score: 999"
    [SerializeField] private Button playButton;      // yeşil Play
    [SerializeField] private GameObject lockGroup;   // kilitli ise görünen gri grup

    private int _level;
    private Action<int> _onPlay;

    public void Setup(int levelNumber, string title, int highScore, bool isLocked, Action<int> onPlay)
    {
        _level = levelNumber;
        _onPlay = onPlay;

        if (titleText) titleText.text = title;
        if (highScoreText) highScoreText.text = highScore > 0 ? $"High Score: {highScore}" : "High Score:";

        if (playButton)
        {
            // Sadece tıklanabilirliğini değiştir
            playButton.interactable = !isLocked;
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => _onPlay?.Invoke(_level));
            // ❌ Artık SetActive ile kapatmıyoruz
            // playButton.gameObject.SetActive(!isLocked);
        }

        //if (lockGroup) lockGroup.SetActive(isLocked);
    }

    // İstersen runtime'da skor güncellemek için:
    public void SetHighScore(int value)
    {
        if (highScoreText) highScoreText.text = value > 0 ? $"High Score: {value}" : "High Score:";
    }
}
