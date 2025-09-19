using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class AnswerManager : Singleton<AnswerManager>
{
    [Header("Dictionary")]
    [Tooltip("Resources içindeki sözlük dosyası yolu (uzantısız).")]
    public string dictionaryResourcePath = "_Game/WordDictionary";
    [Tooltip("Sözlükte min. kelime uzunluğu")]
    public int minWordLength = 2;

    [Header("Runtime/Debug")]
    [SerializeField] private string _currentAnswer = "";
    [SerializeField] private bool _isCurrentValid = false;

    public System.Action<string, bool> OnAnswerChanged;

    public string CurrentAnswer => _currentAnswer;
    public bool IsCurrentValid => _isCurrentValid;

    // --- sözlük ---
    private HashSet<string> _words;    // tam eşleşme kümesi
    private HashSet<string> _prefixes; // önek kümesi (performans için)

    // --- DUPLICATE KONTROL: level-bazlı kayıt ---
    private int _currentLevelCache = -1;
    private readonly HashSet<string> _submittedThisLevel = new HashSet<string>(); // UPPERCASE

    private void Awake()
    {
        LoadDictionary();
    }

    /// <summary>Level değiştiğinde çağır. Bu level için submit edilen kelimeler temizlenir.</summary>
    public void OnLevelStarted(int levelNumber)
    {
        if (levelNumber != _currentLevelCache)
        {
            _currentLevelCache = levelNumber;
            _submittedThisLevel.Clear();
#if UNITY_EDITOR
            Debug.Log($"[AnswerManager] Level {levelNumber} başladı → submit geçmişi temizlendi.");
#endif
        }
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
            for (int i = 1; i <= w.Length; i++) _prefixes.Add(w.Substring(0, i));
        }
        Debug.Log($"AnswerManager: {_words.Count} kelime yüklendi.");
    }

    public void RecomputeCurrentAnswer(LetterHolderController[] holders)
    {
        if (holders == null || holders.Length == 0) { SetAnswer("", false); return; }

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < holders.Length; i++)
        {
            var h = holders[i];
            if (h == null || !h.IsOccupied || h.Current == null) break;
            sb.Append(char.ToUpperInvariant(h.Current.letter));
        }

        string word = sb.ToString();
        bool valid = word.Length >= minWordLength && _words.Contains(word);
        SetAnswer(word, valid);
    }

    private void SetAnswer(string word, bool valid)
    {
        // sadece değiştiyse event at
        if (_currentAnswer == word && _isCurrentValid == valid) return;

        _currentAnswer = word;
        _isCurrentValid = valid;

        OnAnswerChanged?.Invoke(_currentAnswer, _isCurrentValid);

        if (valid)
            Debug.Log($"Anlamlı kelime oluşturuldu:\"{_currentAnswer}\"");
    }

    /// <summary>
    /// Geçerli kelimeyi submit eder: aynı level içinde aynı kelime ikinci kez submit edilemez.
    /// </summary>
    public void SubmitCurrentWord()
    {
        if (!_isCurrentValid) return;

        // Level numarasını yakala (GameFlow yoksa cache'i kullan)
        int lvl = GameFlowManager.Instance != null
                    ? GameFlowManager.Instance.CurrentLevelNumber
                    : _currentLevelCache;

        // güvence: cache'lenmiş level yanlışsa resetle
        if (lvl != _currentLevelCache) OnLevelStarted(lvl);

        string upper = _currentAnswer.ToUpperInvariant();

        // 🔒 aynı levelda tekrar submit engeli
        if (_submittedThisLevel.Contains(upper))
        {
            Debug.Log($"[AnswerManager] \"{upper}\" zaten bu levelda submit edildi. Yoksayılıyor.");
            return;
        }

        // kayıt altına al
        _submittedThisLevel.Add(upper);

        Debug.Log($"SUBMIT: \"{_currentAnswer}\"");

        // 🔴 Puanı ekle
        ScoreManager.Instance?.AddWordScore(_currentAnswer);

        // Harfleri tüket ve cevabı sıfırla
        int count = 0;
        var lhm = LetterHolderManager.Instance;
        if (lhm != null && lhm.holders != null)
        {
            foreach (var h in lhm.holders)
            {
                if (h == null || !h.IsOccupied || h.Current == null) break;
                count++;
            }
            // Projende ConsumeFromStart varsa onu kullan; yoksa ClearAllHoldersImmediate çağır.
            if (count > 0)
            {
                if (lhm.GetType().GetMethod("ConsumeFromStart") != null)
                    lhm.ConsumeFromStart(count);
                else
                    lhm.ClearAllHoldersImmediate();
            }
        }

        SetAnswer("", false);
        BoardManager.Instance?.CheckEndAfterSubmit();
    }
    public bool IsAlreadySubmittedThisLevel(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        return _submittedThisLevel != null && _submittedThisLevel.Contains(word.ToUpperInvariant());
    }

    public void ForceNotify()
    {
        OnAnswerChanged?.Invoke(_currentAnswer, _isCurrentValid);
    }
}
