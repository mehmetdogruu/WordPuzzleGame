using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Level")]
    public int levelNumber = 1;

    [Header("Yerleşim")]
    public RectTransform boardRoot;
    public float unitSize = 20f;
    public Vector2 boardOffset = new Vector2(-200f, -300f);

    [Header("Prefab")]
    public TileViewController tilePrefab;

    [Header("Debug")]
    public bool showIndegreeOnLetter = false;

    // runtime
    private TileViewController[] _tileViews;     // index == tileIndex
    private Dictionary<int, int> _idToIndex;

    void Start()
    {
        if (!boardRoot || !tilePrefab)
        {
            Debug.LogError("LevelManager: eksik referans.");
            return;
        }
        // Başlangıçta level açılışı artık LevelPopup üzerinden geliyor.
        // Eğer test için otomatik yüklemek istersen:
        // BuildLevel(levelNumber);
    }

    /// <summary>
    /// İstenen level dosyasını okur, board’u yeniden kurar ve tüm runtime
    /// state’leri (BoardManager, GameFlowManager, AnswerManager) günceller.
    /// </summary>
    public void BuildLevel(int lvl)
    {
        levelNumber = lvl;
        string resPath = $"Levels/level_{lvl}";
        var ta = Resources.Load<TextAsset>(resPath);
        if (ta == null)
        {
            Debug.LogError($"[LevelManager] Level JSON yok: {resPath}.json");
            return;
        }

        var level = JsonUtility.FromJson<LevelData>(ta.text);
        if (level == null || level.tiles == null || level.tiles.Length == 0)
        {
            Debug.LogError($"[LevelManager] Level parse/boş: {resPath}.json");
            return;
        }

        // 1) Board’u temizle
        foreach (Transform c in boardRoot)
            Destroy(c.gameObject);

        int n = level.tiles.Length;

        // 2) ID -> index haritası
        var idToIndex = new Dictionary<int, int>(n);
        for (int i = 0; i < n; i++)
            idToIndex[level.tiles[i].id] = i;

        // 3) z küçük → büyük (üstte olan son sırada)
        var order = new List<int>(n);
        for (int i = 0; i < n; i++) order.Add(i);
        order.Sort((a, b) => level.tiles[b].position.z.CompareTo(level.tiles[a].position.z));

        // 4) Instantiate ve dizileri hazırla
        var viewsByIndex = new TileViewController[n];
        var rtsByIndex = new RectTransform[n];

        foreach (int i in order)
        {
            var t = level.tiles[i];
            var tv = Instantiate(tilePrefab, boardRoot, false);

            char ch = !string.IsNullOrEmpty(t.character)
                        ? char.ToUpperInvariant(t.character.Trim()[0])
                        : '?';
            tv.Setup(i, t.id, ch);

            var rt = (RectTransform)tv.transform;
            rt.anchoredPosition = boardOffset + new Vector2(t.position.x, t.position.y) * unitSize;

            viewsByIndex[i] = tv;
            rtsByIndex[i] = rt;
        }

        // 5) SiblingIndex (görsel üst/alt sırası)
        for (int s = 0; s < order.Count; s++)
            rtsByIndex[order[s]].SetSiblingIndex(s);

        // 6) BoardManager’a ver (index == tileIndex)
        var views = new List<TileViewController>(n);
        for (int i = 0; i < n; i++) views.Add(viewsByIndex[i]);

        BoardManager.Instance?.Initialize(level, views, idToIndex);

        // 7) Game akışı ve Answer reset
        GameFlowManager.Instance?.SetCurrentLevel(levelNumber);

        // Bu level için daha önce submit edilen kelimeleri sıfırla
        if (AnswerManager.InstanceExists)
        {
            AnswerManager.Instance.OnLevelStarted(levelNumber);
            // Yeni levelde UI’ın hemen güncellenmesi için mevcut cevabı boş olarak bildir
            AnswerManager.Instance.ForceNotify();
        }
    }
}
