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
    private LevelData _level;
    private BoardState _board;
    private TileViewController[] _tileViews;     // index == tileIndex
    private Dictionary<int, int> _idToIndex;

    void Start()
    {
        if (!boardRoot || !tilePrefab) { Debug.LogError("LevelManager: eksik referans."); return; }
        BuildLevel(levelNumber);
    }

    public void BuildLevel(int lvl)
    {
        levelNumber = lvl;
        string resPath = $"Levels/level_{lvl}";
        var ta = Resources.Load<TextAsset>(resPath);
        if (ta == null) { Debug.LogError($"Level JSON yok: {resPath}.json"); return; }

        var level = JsonUtility.FromJson<LevelData>(ta.text);
        if (level == null || level.tiles == null || level.tiles.Length == 0)
        { Debug.LogError($"Level parse/boş: {resPath}.json"); return; }

        // 1) Board'u temizle
        foreach (Transform c in boardRoot) Destroy(c.gameObject);

        int n = level.tiles.Length;

        // ID -> index
        var idToIndex = new Dictionary<int, int>(n);
        for (int i = 0; i < n; i++) idToIndex[level.tiles[i].id] = i;

        // z küçük -> büyük
        var order = new List<int>(n);
        for (int i = 0; i < n; i++) order.Add(i);
        order.Sort((a, b) => level.tiles[b].position.z.CompareTo(level.tiles[a].position.z));

        // 2) Instantiate: görünüm dizisini TILE INDEX'e göre doldur
        var viewsByIndex = new TileViewController[n];
        var rtsByIndex = new RectTransform[n];

        foreach (int i in order)
        {
            var t = level.tiles[i];
            var tv = Instantiate(tilePrefab, boardRoot, false);

            char ch = !string.IsNullOrEmpty(t.character)
                        ? char.ToUpperInvariant(t.character.Trim()[0]) : '?';
            tv.Setup(i, t.id, ch);

            var rt = (RectTransform)tv.transform;
            rt.anchoredPosition = boardOffset
                                + new Vector2(t.position.x, t.position.y) * unitSize;
            viewsByIndex[i] = tv;    // 🔴 index == tileIndex
            rtsByIndex[i] = rt;
        }

        // 3) SiblingIndex'i z-sırasına göre ver (görünüşte üst/alt doğru olsun)
        for (int s = 0; s < order.Count; s++)
            rtsByIndex[order[s]].SetSiblingIndex(s);

        // 4) BoardManager'a ver: listeyi TILE INDEX sırasına göre oluştur
        var views = new List<TileViewController>(n);
        for (int i = 0; i < n; i++) views.Add(viewsByIndex[i]);

        BoardManager.Instance?.Initialize(level, views, idToIndex);

        // 5) GameFlow için seviye bilgisi ve answer reset
        GameFlowManager.Instance?.SetCurrentLevel(levelNumber);
        AnswerManager.Instance?.RecomputeCurrentAnswer(null);
        AnswerManager.Instance?.OnLevelStarted(levelNumber);

    }


    // -------- indegree auto-detect + geometrik yedek --------
    private int[] ComputeIndegreeAuto(LevelData level, Dictionary<int, int> idToIndex,
                                      float overlapDx, float overlapDy)
    {
        int n = level.tiles.Length;
        var indegA = new int[n]; // A: t.children = alttakiler (child indegree++)
        var indegB = new int[n]; // B: t.children = üstündekiler (t indegree++)
        int agreeA = 0, agreeB = 0;

        for (int i = 0; i < n; i++)
        {
            var ti = level.tiles[i];
            if (ti.children == null) continue;

            foreach (var otherId in ti.children)
            {
                if (!idToIndex.TryGetValue(otherId, out int j)) continue;

                // A yorumu
                indegA[j]++;
                if (ti.position.z >= level.tiles[j].position.z) agreeA++; else agreeA--;

                // B yorumu
                indegB[i]++;
                if (level.tiles[j].position.z >= ti.position.z) agreeB++; else agreeB--;
            }
        }

        if (agreeA > agreeB + 1) return indegA;
        if (agreeB > agreeA + 1) return indegB;

        // Geometrik yedek: daha büyük z ve x/y örtüşmesi "üstte" sayılır
        var indegG = new int[n];
        for (int i = 0; i < n; i++)
        {
            var a = level.tiles[i];
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var b = level.tiles[j];
                if (b.position.z <= a.position.z) continue;

                if (Mathf.Abs(a.position.x - b.position.x) <= overlapDx &&
                    Mathf.Abs(a.position.y - b.position.y) <= overlapDy)
                {
                    indegG[i]++;
                }
            }
        }

        // En kısıtlayıcıyı seç (çoğu durumda çocuk bilgisi varsa bu ikisinden biri baskın çıkar)
        int sumA = 0, sumB = 0, sumG = 0;
        for (int k = 0; k < n; k++) { sumA += indegA[k]; sumB += indegB[k]; sumG += indegG[k]; }

        if (sumA >= sumB && sumA >= sumG) return indegA;
        if (sumB >= sumA && sumB >= sumG) return indegB;
        return indegG;
    }
}
