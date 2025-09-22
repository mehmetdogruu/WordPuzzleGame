using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class AnswerManager : Singleton<AnswerManager>
{
    [Header("Dictionary")]
    public string dictionaryResourcePath = "_Game/WordDictionary";
    public int minWordLength = 2;

    [Header("Runtime/Debug")]
    [SerializeField] string _currentAnswer = "";
    [SerializeField] bool _isCurrentValid = false;

    public System.Action<string, bool> OnAnswerChanged;
    public string CurrentAnswer => _currentAnswer;
    public bool IsCurrentValid => _isCurrentValid;

    HashSet<string> _words;
    HashSet<string> _prefixes;

    int _currentLevelCache = -1;
    readonly HashSet<string> _submittedThisLevel = new();   // AutoSolver için güvenli yardımcı API
    public bool IsWord(string w)
        => !string.IsNullOrEmpty(w) && _words != null && _words.Contains(w.ToUpperInvariant());

    public bool IsPrefix(string p)
        => !string.IsNullOrEmpty(p) && _prefixes != null && _prefixes.Contains(p.ToUpperInvariant());

    public int MinWordLength => minWordLength;

    void Awake() => LoadDictionary();

    public void OnLevelStarted(int levelNumber)
    {
        _currentLevelCache = levelNumber;
        _submittedThisLevel.Clear();
#if UNITY_EDITOR
        Debug.Log($"[AnswerManager] Level {levelNumber} başladı → submit geçmişi temizlendi.");
#endif
    }

    void LoadDictionary()
    {
        _words = new HashSet<string>();
        _prefixes = new HashSet<string>();
        var ta = Resources.Load<TextAsset>(dictionaryResourcePath);
        if (ta == null) { Debug.LogError("Sözlük bulunamadı"); return; }

        var lines = ta.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in lines)
        {
            var w = raw.Trim().ToUpperInvariant();
            if (w.Length == 0) continue;
            _words.Add(w);
            for (int i = 1; i <= w.Length; i++) _prefixes.Add(w[..i]);
        }
        Debug.Log($"AnswerManager: {_words.Count} kelime yüklendi.");
    }

    public void RecomputeCurrentAnswer(LetterHolderController[] holders)
    {
        if (holders == null || holders.Length == 0) { SetAnswer("", false); return; }

        var sb = new System.Text.StringBuilder();
        foreach (var h in holders)
        {
            if (h == null || !h.IsOccupied || h.Current == null) break;
            sb.Append(char.ToUpperInvariant(h.Current.letter));
        }

        string word = sb.ToString();
        bool ok = word.Length >= minWordLength && _words.Contains(word);
        SetAnswer(word, ok);
    }

    void SetAnswer(string word, bool valid)
    {
        if (_currentAnswer == word && _isCurrentValid == valid) return;
        _currentAnswer = word;
        _isCurrentValid = valid;
        OnAnswerChanged?.Invoke(_currentAnswer, _isCurrentValid);
        if (valid) Debug.Log($"Kelime:\"{_currentAnswer}\"");
    }

    public void SubmitCurrentWord()
    {
        if (!_isCurrentValid) return;

        int lvl = GameFlowManager.Instance ? GameFlowManager.Instance.CurrentLevelNumber : _currentLevelCache;
        if (lvl != _currentLevelCache) OnLevelStarted(lvl);

        string upper = _currentAnswer.ToUpperInvariant();
        if (_submittedThisLevel.Contains(upper))
        {
            Debug.Log($"[AnswerManager] \"{upper}\" zaten bu levelda submit edildi.");
            return;
        }

        _submittedThisLevel.Add(upper);

        ScoreManager.Instance?.AddWordScore(_currentAnswer);

        int count = 0;
        var lhm = LetterHolderManager.Instance;
        if (lhm?.holders != null)
        {
            foreach (var h in lhm.holders)
            {
                if (h == null || !h.IsOccupied || h.Current == null) break;
                count++;
            }

            if (count > 0)
            {
                if (lhm.GetType().GetMethod("ConsumeFromStartAnimated") != null)
                {
                    lhm.ConsumeFromStartAnimated(count, dur: 0.28f, onComplete: null);
                }
                else
                {
                    lhm.ConsumeFromStart(count);
                    BoardManager.Instance?.CheckEndAfterSubmit();
                }
            }
            else BoardManager.Instance?.CheckEndAfterSubmit();
        }

        SetAnswer("", false);
    }

    public bool IsAlreadySubmittedThisLevel(string word)
        => !string.IsNullOrEmpty(word) && _submittedThisLevel.Contains(word.ToUpperInvariant());

    public void ForceNotify() => OnAnswerChanged?.Invoke(_currentAnswer, _isCurrentValid);
}
