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
    [SerializeField] private LevelListItem itemPrefab;   // tek satýr prefab
    [SerializeField] private Button closeButton;         // X butonu
    [SerializeField] private bool buildOnStart = true;   // oyun baþlarken oluþtur

    [Header("Progress")]
    [Tooltip("Baþlangýçta açýk olacak ilk level")]
    [SerializeField] private int firstUnlockedLevel = 1;


    // DOÐRU regex
    private readonly Regex _rxNum = new Regex(@"level_(\d+)", RegexOptions.IgnoreCase);

    // Oluþturulan item’larý ve level numaralarýný hafýzada tutalým
    private readonly List<(int level, LevelListItem item)> _items = new();
    private readonly List<int> _levels = new(); // sýralý level listesi
    private bool _built = false;

    protected override void Awake()
    {
        base.Awake(); // _canvas, _group
        if (closeButton) closeButton.onClick.AddListener(Close);
        HideInstant(); // popup baþlangýçta kapalý
    }

    private void Start()
    {
        if (buildOnStart)
        {
            BuildOrRebuildItems();
            // görünür yapmýyoruz — sadece hazýrla
            HideInstant();
        }
    }

    public void Open()
    {
        if (!_built) BuildOrRebuildItems();
        RefreshRuntimeStates(); // HighScore/Kilit durumlarýný güncelle
        Show();                 // UIController.Show()
    }

    public void Close() => Hide();

    // --- Items kurulum ---
    private void BuildOrRebuildItems()
    {
        // Güvenlik
        if (!content)
        {
            Debug.LogError("[LevelPopup] Content atanmadý.");
            return;
        }
        if (!itemPrefab)
        {
            Debug.LogError("[LevelPopup] itemPrefab atanmadý.");
            return;
        }

        // Eski çocuklarý temizle
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        _items.Clear();
        _levels.Clear();

        // 1) Resources/Levels altýndaki bütün JSON dosyalarý
        var all = Resources.LoadAll<TextAsset>("Levels");
        if (all == null || all.Length == 0)
            Debug.LogWarning("[LevelPopup] Resources/Levels altýnda TextAsset bulunamadý.");

        // 2) Dosya adlarýndan level numaralarýný çek
        foreach (var ta in all)
        {
            var m = _rxNum.Match(ta.name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int num))
                _levels.Add(num);
        }
        _levels.Sort(); // küçükten büyüðe

        // 3) Ýlerleme bilgisi
        int maxCompleted = Progress.GetMaxCompleted(firstUnlockedLevel);
        int maxPlayable = Progress.GetMaxPlayable(firstUnlockedLevel);

        // 4) Prefab’leri üret ve doldur
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

    // Her açýlýþta skor/kilit bilgilerini güncelle (item’larý yeniden yaratmadan)
    private void RefreshRuntimeStates()
    {
        int maxCompleted = Progress.GetMaxCompleted(firstUnlockedLevel);
        int maxPlayable = Progress.GetMaxPlayable(firstUnlockedLevel);

        foreach (var (levelNum, item) in _items)
        {
            // High score güncelle
            int hs = PlayerPrefs.GetInt(Progress.HighScoreKey(levelNum), 0);
            item.SetHighScore(hs);

            // Kilit durumu
            bool isLocked = levelNum > maxPlayable;

            // Tüm deðerleri tekrar uygula (title deðiþmediði için ayný string)
            item.Setup(levelNum, $"LEVEL {levelNum}", hs, isLocked, HandlePlayClicked);
        }
    }

    public void Refresh()
    {
        if (!_built)
            BuildOrRebuildItems();   // hiç kurulmadýysa oluþtur

        RefreshRuntimeStates();      // skor & kilitleri güncelle

        // (Ýsteðe baðlý) açýksa layout’u tazele
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
    //    // Eðer daha önce "max_unlocked_level" yazdýysan ve yeni anahtar yoksa migrate et
    //    const string OLD = "max_unlocked_level";
    //    if (!PlayerPrefs.HasKey(Key_MaxUnlocked) && PlayerPrefs.HasKey(OLD))
    //    {
    //        int oldUnlocked = PlayerPrefs.GetInt(OLD, firstUnlockedLevel); // bu "oynanabilir en yüksek" idi
    //        int derivedLastCompleted = Mathf.Max(firstUnlockedLevel - 1, oldUnlocked - 1);
    //        PlayerPrefs.SetInt(Key_MaxUnlocked, derivedLastCompleted);
    //        PlayerPrefs.Save();
    //    }
    //}

    // --- Play akýþý ---
    private void HandlePlayClicked(int levelNumber)
    {
        Hide();

        // skor/holder temizliði
        if (ScoreManager.InstanceExists) ScoreManager.Instance.ResetScore();
        if (LetterHolderManager.InstanceExists) LetterHolderManager.Instance.ClearAllHoldersImmediate();

        // level yükle
        LevelManager.Instance?.BuildLevel(levelNumber);
        GameFlowManager.Instance?.SetCurrentLevel(levelNumber);
    }
}
