using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UISystem;   // UIController<T>
using Helpers;

public class LevelPopupController : UIController<LevelPopupController>
{
    [Header("UI")]
    [SerializeField] private Transform content;          // ScrollView/Content
    [SerializeField] private LevelListItem itemPrefab;   // tek sat�r prefab
    [SerializeField] private Button closeButton;         // X butonu
    [SerializeField] private bool buildOnStart = true;   // oyun ba�larken olu�tur

    [Header("Progress")]
    [Tooltip("Ba�lang��ta a��k olacak ilk level")]
    [SerializeField] private int firstUnlockedLevel = 1;


    // DO�RU regex
    private readonly Regex _rxNum = new Regex(@"level_(\d+)", RegexOptions.IgnoreCase);

    // Olu�turulan item�lar� ve level numaralar�n� haf�zada tutal�m
    private readonly List<(int level, LevelListItem item)> _items = new();
    private readonly List<int> _levels = new(); // s�ral� level listesi
    private bool _built = false;

    protected override void Awake()
    {
        base.Awake(); // _canvas, _group
        if (closeButton) closeButton.onClick.AddListener(Close);
        HideInstant(); // popup ba�lang��ta kapal�
    }

    private void Start()
    {
        if (buildOnStart)
        {
            BuildOrRebuildItems();
            // g�r�n�r yapm�yoruz � sadece haz�rla
            HideInstant();
        }
    }

    public void Open()
    {
        if (!_built) BuildOrRebuildItems();
        RefreshRuntimeStates(); // HighScore/Kilit durumlar�n� g�ncelle
        Show();                 // UIController.Show()
    }

    public void Close() => Hide();

    // --- Items kurulum ---
    private void BuildOrRebuildItems()
    {
        // G�venlik
        if (!content)
        {
            Debug.LogError("[LevelPopup] Content atanmad�.");
            return;
        }
        if (!itemPrefab)
        {
            Debug.LogError("[LevelPopup] itemPrefab atanmad�.");
            return;
        }

        // Eski �ocuklar� temizle
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        _items.Clear();
        _levels.Clear();

        // 1) Resources/Levels alt�ndaki b�t�n JSON dosyalar�
        var all = Resources.LoadAll<TextAsset>("Levels");
        if (all == null || all.Length == 0)
            Debug.LogWarning("[LevelPopup] Resources/Levels alt�nda TextAsset bulunamad�.");

        // 2) Dosya adlar�ndan level numaralar�n� �ek
        foreach (var ta in all)
        {
            var m = _rxNum.Match(ta.name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int num))
                _levels.Add(num);
        }
        _levels.Sort(); // k���kten b�y��e

        // 3) �lerleme bilgisi
        int maxCompleted = Progress.GetMaxCompleted(firstUnlockedLevel);
        int maxPlayable = Progress.GetMaxPlayable(firstUnlockedLevel);

        // 4) Prefab�leri �ret ve doldur
        foreach (int levelNum in _levels)
        {
            var item = Instantiate(itemPrefab, content, false);

            string title = $"LEVEL {levelNum}";
            int hs = PlayerPrefs.GetInt(Progress.HighScoreKey(levelNum), 0);
            bool isLocked = levelNum > maxPlayable;

            item.Setup(
                levelNumber: levelNum,
                title: title,
                highScore: hs,
                isLocked: isLocked,
                onPlay: HandlePlayClicked
            );

            _items.Add((levelNum, item));
        }

        _built = true;

        // (opsiyonel) Layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);

        Debug.Log($"[LevelPopup] Toplam {all.Length} JSON bulundu, {_items.Count} item kuruldu. " +
                  $"maxCompleted={maxCompleted}, maxPlayable={maxPlayable}");
    }

    // Her a��l��ta skor/kilit bilgilerini g�ncelle (item�lar� yeniden yaratmadan)
    private void RefreshRuntimeStates()
    {
        int maxCompleted = Progress.GetMaxCompleted(firstUnlockedLevel);
        int maxPlayable = Progress.GetMaxPlayable(firstUnlockedLevel);

        foreach (var (levelNum, item) in _items)
        {
            // High score g�ncelle
            int hs = PlayerPrefs.GetInt(Progress.HighScoreKey(levelNum), 0);
            item.SetHighScore(hs);

            // Kilit durumu
            bool isLocked = levelNum > maxPlayable;

            // T�m de�erleri tekrar uygula (title de�i�medi�i i�in ayn� string)
            item.Setup(levelNum, $"LEVEL {levelNum}", hs, isLocked, HandlePlayClicked);
        }
    }

    public void Refresh()
    {
        if (!_built)
            BuildOrRebuildItems();   // hi� kurulmad�ysa olu�tur

        RefreshRuntimeStates();      // skor & kilitleri g�ncelle

        // (�ste�e ba�l�) a��ksa layout�u tazele
        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }
    private (int maxCompleted, int maxPlayable) ReadProgress()
    {
        int maxCompleted = Progress.GetMaxCompleted(firstUnlockedLevel);
        int maxPlayable = Progress.GetMaxPlayable(firstUnlockedLevel);
        return (maxCompleted, maxPlayable);
    }


    //private void MigrateOldPrefsIfNeeded()
    //{
    //    // E�er daha �nce "max_unlocked_level" yazd�ysan ve yeni anahtar yoksa migrate et
    //    const string OLD = "max_unlocked_level";
    //    if (!PlayerPrefs.HasKey(Key_MaxUnlocked) && PlayerPrefs.HasKey(OLD))
    //    {
    //        int oldUnlocked = PlayerPrefs.GetInt(OLD, firstUnlockedLevel); // bu "oynanabilir en y�ksek" idi
    //        int derivedLastCompleted = Mathf.Max(firstUnlockedLevel - 1, oldUnlocked - 1);
    //        PlayerPrefs.SetInt(Key_MaxUnlocked, derivedLastCompleted);
    //        PlayerPrefs.Save();
    //    }
    //}

    // --- Play ak��� ---
    private void HandlePlayClicked(int levelNumber)
    {
        Hide();

        // skor/holder temizli�i
        if (ScoreManager.InstanceExists) ScoreManager.Instance.ResetScore();
        if (LetterHolderManager.InstanceExists) LetterHolderManager.Instance.ClearAllHoldersImmediate();

        // level y�kle
        LevelManager.Instance?.BuildLevel(levelNumber);
        GameFlowManager.Instance?.SetCurrentLevel(levelNumber);
    }
}
