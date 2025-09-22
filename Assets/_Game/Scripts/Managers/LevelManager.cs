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
    public Vector2 boardOffset = new(-200f, -300f);

    [Header("Prefab")]
    public TileViewController tilePrefab;

    [Header("Debug")]
    public bool showIndegreeOnLetter = false;

    void Start()
    {
        if (!boardRoot || !tilePrefab)
            Debug.LogError("LevelManager: eksik referans.");
    }

    public void BuildLevel(int lvl)
    {
        levelNumber = lvl;

        var ta = Resources.Load<TextAsset>($"Levels/level_{lvl}");
        if (ta == null) { Debug.LogError($"[LevelManager] JSON yok: level_{lvl}.json"); return; }

        var level = JsonUtility.FromJson<LevelData>(ta.text);
        if (level?.tiles == null || level.tiles.Length == 0)
        { Debug.LogError($"[LevelManager] parse/boş: level_{lvl}.json"); return; }

        foreach (Transform c in boardRoot) Destroy(c.gameObject);

        int n = level.tiles.Length;

        var idToIndex = new Dictionary<int, int>(n);
        for (int i = 0; i < n; i++) idToIndex[level.tiles[i].id] = i;

        var order = new List<int>(n);
        for (int i = 0; i < n; i++) order.Add(i);
        order.Sort((a, b) => level.tiles[b].position.z.CompareTo(level.tiles[a].position.z));

        var viewsByIndex = new TileViewController[n];
        var rtsByIndex = new RectTransform[n];

        foreach (int i in order)
        {
            var t = level.tiles[i];
            var tv = Instantiate(tilePrefab, boardRoot, false);

            char ch = !string.IsNullOrEmpty(t.character) ? char.ToUpperInvariant(t.character.Trim()[0]) : '?';
            tv.Setup(i, t.id, ch);

            var rt = (RectTransform)tv.transform;
            rt.anchoredPosition = boardOffset + new Vector2(t.position.x, t.position.y) * unitSize;

            viewsByIndex[i] = tv;
            rtsByIndex[i] = rt;
        }

        for (int s = 0; s < order.Count; s++)
            rtsByIndex[order[s]].SetSiblingIndex(s);

        var views = new List<TileViewController>(n);
        for (int i = 0; i < n; i++) views.Add(viewsByIndex[i]);

        BoardManager.Instance?.Initialize(level, views, idToIndex);
        GameFlowManager.Instance?.SetCurrentLevel(levelNumber);

        if (AnswerManager.InstanceExists)
        {
            AnswerManager.Instance.OnLevelStarted(levelNumber);
            BoosterController.Instance?.ResetUses();
            AnswerManager.Instance.ForceNotify();
        }
    }
}
