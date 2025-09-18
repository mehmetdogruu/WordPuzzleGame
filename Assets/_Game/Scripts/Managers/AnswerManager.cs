using System.Collections.Generic;
using UnityEngine;

public class AnswerManager : SceneSingleton<AnswerManager>
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


    private HashSet<string> _words;    // tam eşleşme kümesi
    private HashSet<string> _prefixes; // önek kümesi (performans için)

    protected override void Awake()
    {
        base.Awake();
        LoadDictionary();
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
    /// Geçerli kelimeyi submit eder: debug'a yazar ve holder'lardaki harfleri yok eder.
    /// </summary>
    public void SubmitCurrentWord()
    {
        if (!_isCurrentValid) return;

        Debug.Log($"SUBMIT: \"{_currentAnswer}\"");

        // 🔴 Puanı ekle
        ScoreManager.Instance?.AddWordScore(_currentAnswer);

        // Harfleri tüket ve cevabı sıfırla
        int count = 0;
        foreach (var h in LetterHolderManager.Instance.holders)
        {
            if (h == null || !h.IsOccupied || h.Current == null) break;
            count++;
        }

        LetterHolderManager.Instance.ConsumeFromStart(count);
        SetAnswer("", false);
        BoardManager.Instance?.CheckEndAfterSubmit();

    }

    public void ForceNotify()
    {
        OnAnswerChanged?.Invoke(_currentAnswer, _isCurrentValid);
    }
}
