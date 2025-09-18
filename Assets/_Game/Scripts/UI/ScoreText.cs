using TMPro;
using UnityEngine;

public class ScoreText : MonoBehaviour
{
    public TMP_Text scoreText;

    void OnEnable()
    {
        ScoreManager.Instance.OnScoreChanged += HandleScore;
        HandleScore(ScoreManager.Instance.TotalScore); // an�nda g�ncelle
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= HandleScore;
    }

    void HandleScore(int newScore)
    {
        if (scoreText) scoreText.text = $"Score: {newScore}";
    }
}
