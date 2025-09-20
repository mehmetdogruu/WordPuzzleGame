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
    [SerializeField] private LevelListItem itemPrefab;   // tek satır prefab
    [SerializeField] private Button closeButton;         // X butonu
    [SerializeField] private bool buildOnStart = true;   // oyun başlarken oluştur

    [Header("Progress")]
    [Tooltip("Başlangıçta açık olacak ilk level")]
    [SerializeField] private int firstUnlockedLevel = 1;


    // DOĞRU regex
    private readonly Regex _rxNum = new Regex(@"level_(\d+)", RegexOptions.IgnoreCase);

    // Oluşturulan item’ları ve level numaralarını hafızada tutalım
    private readonly List<(int level, LevelListItem item)> _items = new();
    private readonly List<int> _levels = new(); // sıralı level listesi
    private bool _built = false;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton) closeButton.onClick.AddListener(Close);
        HideInstant();                 // ✅ açılışta gizli
    }

    void Start()
    {
        if (buildOnStart)
        {
            BuildOrRebuildItems();
            // Show();   // ❌ istemiyoruz
        }
    }

    public void Open()
    {
        if (!_built) BuildOrRebuildItems();
        RefreshRuntimeStates();
        Show();
    }

    public void Close()
    {
        Hide();
        MainMenuController.Instance.Show();
    }

    // --- Items kurulum ---
    private void BuildOrRebuildItems()
    {
        // Güvenlik
        if (!content)
        {
            Debug.LogError("[LevelPopup] Content atanmadı.");
            return;
        }
        if (!itemPrefab)
        {
            Debug.LogError("[LevelPopup] itemPrefab atanmadı.");
            return;
        }

        // Eski çocukları temizle
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        _items.Clear();
        _levels.Clear();

        // 1) Resources/Levels altındaki bütün JSON dosyaları
        var all = Resources.LoadAll<TextAsset>("Levels");
        if (all == null || all.Length == 0)
            Debug.LogWarning("[LevelPopup] Resources/Levels altında TextAsset bulunamadı.");

        // 2) Dosya adlarından level numaralarını çek
        foreach (var ta in all)
        {
            var m = _rxNum.Match(ta.name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int num))
                _levels.Add(num);
        }
        _levels.Sort(); // küçükten büyüğe

        // 3) İlerleme bilgisi
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

    // Her açılışta skor/kilit bilgilerini güncelle (item’ları yeniden yaratmadan)
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
            if (isLocked)
            {
                item.lockImage.SetActive(true);
                item.playText.text = "";
            }
            else
            {
                item.lockImage.SetActive(false);
                item.playText.text = "Play";

            }

            // Tüm değerleri tekrar uygula (title değişmediği için aynı string)
            item.Setup(levelNum, $"LEVEL {levelNum}", hs, isLocked, HandlePlayClicked);
        }
    }

    public void Refresh()
    {
        if (!_built)
            BuildOrRebuildItems();   // hiç kurulmadıysa oluştur

        RefreshRuntimeStates();      // skor & kilitleri güncelle

        // (İsteğe bağlı) açıksa layout’u tazele
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
    //    // Eğer daha önce "max_unlocked_level" yazdıysan ve yeni anahtar yoksa migrate et
    //    const string OLD = "max_unlocked_level";
    //    if (!PlayerPrefs.HasKey(Key_MaxUnlocked) && PlayerPrefs.HasKey(OLD))
    //    {
    //        int oldUnlocked = PlayerPrefs.GetInt(OLD, firstUnlockedLevel); // bu "oynanabilir en yüksek" idi
    //        int derivedLastCompleted = Mathf.Max(firstUnlockedLevel - 1, oldUnlocked - 1);
    //        PlayerPrefs.SetInt(Key_MaxUnlocked, derivedLastCompleted);
    //        PlayerPrefs.Save();
    //    }
    //}

    // --- Play akışı ---
    private void HandlePlayClicked(int levelNumber)
    {
        Hide();

        // skor/holder temizliği
        if (ScoreManager.InstanceExists) ScoreManager.Instance.ResetScore();
        if (LetterHolderManager.InstanceExists) LetterHolderManager.Instance.ClearAllHoldersImmediate();

        // level yükle
        LevelManager.Instance?.BuildLevel(levelNumber);
        GameFlowManager.Instance?.SetCurrentLevel(levelNumber);
    }
}
