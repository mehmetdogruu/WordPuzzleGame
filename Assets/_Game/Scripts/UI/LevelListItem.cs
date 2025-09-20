using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelListItem : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text highScoreText;
    public Button playButton;
    public GameObject lockImage;
    public TMP_Text playText;

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
            playButton.interactable = !isLocked;
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                // Önce mevcut onPlay callback’i (level yükleme vb.)
                _onPlay?.Invoke(_level);

                // ✅ Ardından GamePanel’i aç
                GamePanelController.Instance?.Show();
            });
        }
    }

    public void SetHighScore(int value)
    {
        if (highScoreText) highScoreText.text = value > 0 ? $"High Score: {value}" : "High Score:";
    }
}
