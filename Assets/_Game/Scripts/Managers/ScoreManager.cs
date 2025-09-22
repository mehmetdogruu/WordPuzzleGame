using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class ScoreManager : Singleton<ScoreManager>
{
    [Header("Runtime/Debug")]
    [SerializeField] int _totalScore = 0;
    public int TotalScore => _totalScore;

    public System.Action<int> OnScoreChanged;

    Dictionary<char, int> _letterPoints;

    void Awake() => BuildLetterPoints();

    void BuildLetterPoints()
    {
        _letterPoints = new Dictionary<char, int>
        {
            {'E',1},{'A',1},{'O',1},{'N',1},{'R',1},{'T',1},{'L',1},{'S',1},{'U',1},{'I',1},
            {'D',2},{'G',2},
            {'B',3},{'C',3},{'M',3},{'P',3},
            {'F',4},{'H',4},{'V',4},{'W',4},{'Y',4},
            {'K',5},
            {'J',8},{'X',8},
            {'Q',10},{'Z',10}
        };
    }

    public void AddWordScore(string word)
    {
        if (string.IsNullOrEmpty(word)) return;

        int score = 0;
        foreach (char c in word.ToUpperInvariant())
            if (_letterPoints.TryGetValue(c, out int v)) score += v;

        _totalScore += score;
        OnScoreChanged?.Invoke(_totalScore);
        Debug.Log($"[Score] \"{word}\" = {score} → Total = {_totalScore}");
    }

    public int ComputeWordScore(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0;
        int s = 0;
        foreach (var ch in word.ToUpperInvariant())
            if (_letterPoints.TryGetValue(ch, out var v)) s += v;
        return s;
    }

    public void ResetScore()
    {
        _totalScore = 0;
        OnScoreChanged?.Invoke(_totalScore);
    }
}
