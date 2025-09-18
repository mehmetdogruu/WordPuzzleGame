using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : SceneSingleton<ScoreManager>
{
    [Header("Runtime/Debug")]
    [SerializeField] private int _totalScore = 0;
    public int TotalScore => _totalScore;

    public System.Action<int> OnScoreChanged;

    // --- Harf puan tablosu ---
    private Dictionary<char, int> _letterPoints;

    protected override void Awake()
    {
        base.Awake();
        BuildLetterPoints();
    }

    private void BuildLetterPoints()
    {
        _letterPoints = new Dictionary<char, int>
        {
            // 1 Point
            {'E',1},{'A',1},{'O',1},{'N',1},{'R',1},{'T',1},{'L',1},{'S',1},{'U',1},{'I',1},
            // 2 Points
            {'D',2},{'G',2},
            // 3 Points
            {'B',3},{'C',3},{'M',3},{'P',3},
            // 4 Points
            {'F',4},{'H',4},{'V',4},{'W',4},{'Y',4},
            // 5 Points
            {'K',5},
            // 8 Points
            {'J',8},{'X',8},
            // 10 Points
            {'Q',10},{'Z',10}
        };
    }

    /// <summary>
    /// Kelime içindeki harf puanlarını toplayıp toplam skora ekler.
    /// </summary>
    public void AddWordScore(string word)
    {
        if (string.IsNullOrEmpty(word)) return;

        int score = 0;
        foreach (char c in word.ToUpperInvariant())
        {
            if (_letterPoints.TryGetValue(c, out int val))
                score += val;
            else
                Debug.LogWarning($"PointManager: '{c}' harfi için puan tablosu yok.");
        }

        _totalScore += score;
        Debug.Log($"[PointManager] \"{word}\" kelimesi için {score} puan → Toplam = {_totalScore}");
        OnScoreChanged?.Invoke(_totalScore);
    }
    public int ComputeWordScore(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0;
        int s = 0;
        foreach (var ch in word.ToUpperInvariant())
            if (_letterPoints.TryGetValue(ch, out var v)) s += v;
        return s;
    }

    /// <summary>
    /// Toplam puanı sıfırlar (yeni level vs.).
    /// </summary>
    public void ResetScore()
    {
        _totalScore = 0;
        OnScoreChanged?.Invoke(_totalScore);
    }
}
