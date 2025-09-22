using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UISystem;  
using Helpers;

public class LevelPopupController : UIController<LevelPopupController>
{
    [Header("UI")]
    [SerializeField] private Transform content;          
    [SerializeField] private LevelListItem itemPrefab;   
    [SerializeField] private Button closeButton;         
    [SerializeField] private bool buildOnStart = true;   

    [Header("Progress")]
    [Tooltip("Başlangıçta açık olacak ilk level")]
    [SerializeField] private int firstUnlockedLevel = 1;


    private readonly Regex _rxNum = new Regex(@"level_(\d+)", RegexOptions.IgnoreCase);
    private readonly Dictionary<int, string> _titlesByLevel = new();


    private readonly List<(int level, LevelListItem item)> _items = new();
    private readonly List<int> _levels = new(); 
    private bool _built = false;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton) closeButton.onClick.AddListener(Close);
        HideInstant();               
    }

    void Start()
    {
        if (buildOnStart)
        {
            BuildOrRebuildItems();
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

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        _items.Clear();
        _levels.Clear();

        var all = Resources.LoadAll<TextAsset>("Levels");
        if (all == null || all.Length == 0)
            Debug.LogWarning("[LevelPopup] Resources/Levels altında TextAsset bulunamadı.");

        _titlesByLevel.Clear();
        _levels.Clear();

        foreach (var ta in all)
        {
            var m = _rxNum.Match(ta.name);
            if (!m.Success) continue;

            if (int.TryParse(m.Groups[1].Value, out int num))
            {
                _levels.Add(num);

                try
                {
                    var data = JsonUtility.FromJson<LevelData>(ta.text);
                    _titlesByLevel[num] = data != null ? (data.title ?? "") : "";
                }
                catch
                {
                    _titlesByLevel[num] = "";
                }
            }
        }
        _levels.Sort();

        (int maxCompleted, int maxPlayable) = ReadProgress();

        foreach (int levelNum in _levels)
        {
            var item = Instantiate(itemPrefab, content, false);

            string rawTitle = _titlesByLevel.TryGetValue(levelNum, out var t) ? t : "";
            int hs = PlayerPrefs.GetInt(Progress.HighScoreKey(levelNum), 0);
            bool isLocked = levelNum > maxPlayable;

            item.Setup(
                levelNumber: levelNum,
                title: rawTitle,
                highScore: hs,
                isLocked: isLocked,
                onPlay: HandlePlayClicked
            );

            _items.Add((levelNum, item));
        }

        _built = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }

    private void RefreshRuntimeStates()
    {
        int maxCompleted = Progress.GetMaxCompleted(firstUnlockedLevel);
        int maxPlayable = Progress.GetMaxPlayable(firstUnlockedLevel);

        foreach (var (levelNum, item) in _items)
        {
            int hs = PlayerPrefs.GetInt(Progress.HighScoreKey(levelNum), 0);
            bool isLocked = levelNum > maxPlayable;
            string rawTitle = _titlesByLevel.TryGetValue(levelNum, out var t) ? t : "";

            item.Setup(levelNum, rawTitle, hs, isLocked, HandlePlayClicked);
        }
    }


    public void Refresh()
    {
        if (!_built)
            BuildOrRebuildItems();  

        RefreshRuntimeStates();      

        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }
    private (int maxCompleted, int maxPlayable) ReadProgress()
    {
        int maxCompleted = Progress.GetMaxCompleted(firstUnlockedLevel);
        int maxPlayable = Progress.GetMaxPlayable(firstUnlockedLevel);
        return (maxCompleted, maxPlayable);
    }


    // --- Play akışı ---
    private void HandlePlayClicked(int levelNumber)
    {
        Hide();

        if (ScoreManager.InstanceExists) ScoreManager.Instance.ResetScore();
        if (LetterHolderManager.InstanceExists) LetterHolderManager.Instance.ClearAllHoldersImmediate();

        LevelManager.Instance?.BuildLevel(levelNumber);
        GameFlowManager.Instance?.SetCurrentLevel(levelNumber);
    }
}
