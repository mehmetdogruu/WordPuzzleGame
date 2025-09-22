using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Helpers;

public class BoardManager : Singleton<BoardManager>
{
    [Header("Algılama")]
    public float overlapDx = 14f;
    public float overlapDy = 14f;

    [Header("Debug")]
    [SerializeField] private List<int> openTileIds = new();
    [SerializeField] private List<int> closedTileIds = new();

    LevelData _level;
    List<TileViewController> _views;     
    Dictionary<int, int> _idToIndex;

    int[] _indegree;
    bool[] _alive;
    List<int>[] _blocks;

    readonly HashSet<int> _picked = new();
    readonly HashSet<int> _inFlight = new();

    enum Mode { A, B, Geometric }
    Mode _mode;

    bool _winTriggered = false;

    // ---------------- API ----------------
    public void Initialize(LevelData level, List<TileViewController> tileViews, Dictionary<int, int> idToIndex)
    {
        _level = level;
        _views = tileViews;
        _idToIndex = idToIndex;

        int n = _level.tiles.Length;
        _indegree = ComputeIndegreeAndMode(_level, _idToIndex, overlapDx, overlapDy, out _mode);
        _blocks = BuildBlocksGraph(_level, _idToIndex, _mode, overlapDx, overlapDy);

        _alive = new bool[n];
        for (int i = 0; i < n; i++) _alive[i] = true;

        _picked.Clear();
        _inFlight.Clear();
        _winTriggered = false;

        RefreshAllOpenStates();
        RefreshDebugLists();
    }

    public void OnTilePickingBegin(int tileIndex)
    {
        if (!Valid(tileIndex) || !_alive[tileIndex]) return;

        _alive[tileIndex] = false;
        _picked.Add(tileIndex);
        _inFlight.Add(tileIndex);

        var list = _blocks[tileIndex];
        if (list != null)
        {
            for (int k = 0; k < list.Count; k++) _indegree[list[k]] = Mathf.Max(0, _indegree[list[k]] - 1);
            for (int k = 0; k < list.Count; k++) ApplyOpenState(list[k]);
        }

        RefreshDebugLists();
    }

    public void OnTilePickCanceled(int tileIndex)
    {
        if (!Valid(tileIndex) || !_inFlight.Contains(tileIndex)) return;

        _inFlight.Remove(tileIndex);
        _picked.Remove(tileIndex);
        _alive[tileIndex] = true;

        FullResync();
    }

    public void OnTileReturned(int tileIndex)
    {
        if (!Valid(tileIndex)) return;

        _inFlight.Remove(tileIndex);
        _picked.Remove(tileIndex);
        _alive[tileIndex] = true;

        FullResync();
    }

    public void FullResync()
    {
        if (_indegree == null || _blocks == null || _alive == null) return;

        System.Array.Clear(_indegree, 0, _indegree.Length);

        for (int i = 0; i < _alive.Length; i++)
        {
            if (!_alive[i]) continue;
            var list = _blocks[i];
            if (list == null) continue;
            for (int k = 0; k < list.Count; k++) _indegree[list[k]]++;
        }

        RefreshAllOpenStates();
        RefreshDebugLists();
    }


    public void CheckEndAfterSubmit()
    {
        if (_winTriggered) return;

        if (IsBoardEmpty() || !ExistsAnyValidWordFromOpenTiles())
        {
            TriggerWin();
        }
    }

    public bool IsBoardEmpty()
    {
        if (_alive == null) return true;
        for (int i = 0; i < _alive.Length; i++) if (_alive[i]) return false;
        return true;
    }

    void TriggerWin()
    {
        if (_winTriggered) return;
        _winTriggered = true;

        // Holder’ları temizle + cursor sıfırla
        LetterHolderManager.Instance?.ResetAll();

        // Tek yerden kazanma akışı
        GameFlowManager.Instance?.OnLevelCompletedNoTiles();
    }

    // ---------------- AutoSolver-vari kontrol ----------------
    bool ExistsAnyValidWordFromOpenTiles()
    {
        var am = AnswerManager.Instance;
        if (am == null) return false;

        var openTiles = GetOpenTilesSnapshot();
        if (openTiles.Count == 0) return false;

        // Açık taşların harf listesi (aynı harf birden fazla olabilir)
        var letters = new List<char>(openTiles.Count);
        foreach (var v in openTiles) letters.Add(char.ToUpperInvariant(v.letter));

        var used = new bool[letters.Count];
        var sb = new StringBuilder(letters.Count);

        bool Dfs()
        {
            string cur = sb.ToString();

            if (cur.Length > 0 && !am.IsPrefix(cur)) return false;

            if (cur.Length >= am.MinWordLength && am.IsWord(cur)) return true;

            for (int i = 0; i < letters.Count; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                sb.Append(letters[i]);

                if (Dfs()) return true;

                used[i] = false;
                sb.Length -= 1;
            }
            return false;
        }

        return Dfs();
    }

    public List<TileViewController> GetOpenTilesSnapshot()
    {
        var result = new List<TileViewController>();
        if (_views == null || _alive == null || _indegree == null) return result;

        for (int i = 0; i < _views.Count; i++)
        {
            if (!_alive[i]) continue;
            if (_picked.Contains(i)) continue;
            if (_indegree[i] != 0) continue;

            var v = _views[i];
            if (!v) continue;

            result.Add(v);
        }
        return result;
    }

    // ---------------- Open/Close ----------------
    void RefreshAllOpenStates()
    {
        if (_views == null) return;
        for (int i = 0; i < _views.Count; i++) ApplyOpenState(i);
    }

    void ApplyOpenState(int tileIndex)
    {
        if (!Valid(tileIndex)) return;

        var view = _views[tileIndex];
        if (!view || _picked.Contains(tileIndex) || !_alive[tileIndex]) return;

        bool shouldOpen = _indegree[tileIndex] == 0;

        if (view.IsCurrentlyOpen is null)
        {
            view.IsCurrentlyOpen = shouldOpen;
            view.SetOpen(shouldOpen);
            return;
        }

        if (view.IsCurrentlyOpen != shouldOpen)
        {
            view.IsCurrentlyOpen = shouldOpen;
            if (shouldOpen) view.AnimateOpen();
            else view.AnimateClose();
        }
    }

    bool Valid(int i) => _views != null && i >= 0 && i < _views.Count;

    void RefreshDebugLists()
    {
        openTileIds.Clear();
        closedTileIds.Clear();
        if (_level == null || _views == null || _alive == null) return;

        for (int i = 0; i < _views.Count; i++)
        {
            if (!_alive[i]) continue;
            int id = _level.tiles[i].id;
            if (_indegree[i] == 0) openTileIds.Add(id); else closedTileIds.Add(id);
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ---------------- Graph ----------------
    int[] ComputeIndegreeAndMode(LevelData level, Dictionary<int, int> idToIndex, float dx, float dy, out Mode mode)
    {
        int n = level.tiles.Length;
        var indegA = new int[n];
        var indegB = new int[n];
        int agreeA = 0, agreeB = 0;

        for (int i = 0; i < n; i++)
        {
            var ti = level.tiles[i];
            if (ti.children == null) continue;

            foreach (var otherId in ti.children)
            {
                if (!idToIndex.TryGetValue(otherId, out int j)) continue;

                indegA[j]++;
                agreeA += (ti.position.z <= level.tiles[j].position.z) ? 1 : -1;

                indegB[i]++;
                agreeB += (level.tiles[j].position.z <= ti.position.z) ? 1 : -1;
            }
        }

        if (agreeA > agreeB + 1) { mode = Mode.A; return indegA; }
        if (agreeB > agreeA + 1) { mode = Mode.B; return indegB; }

        var indegG = new int[n];
        for (int i = 0; i < n; i++)
        {
            var a = level.tiles[i];
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var b = level.tiles[j];
                if (b.position.z >= a.position.z) continue;

                if (Mathf.Abs(a.position.x - b.position.x) <= dx &&
                    Mathf.Abs(a.position.y - b.position.y) <= dy)
                {
                    indegG[i]++;
                }
            }
        }

        int sumA = 0, sumB = 0, sumG = 0;
        for (int k = 0; k < n; k++) { sumA += indegA[k]; sumB += indegB[k]; sumG += indegG[k]; }

        if (sumA >= sumB && sumA >= sumG) { mode = Mode.A; return indegA; }
        if (sumB >= sumA && sumB >= sumG) { mode = Mode.B; return indegB; }
        mode = Mode.Geometric; return indegG;
    }

    List<int>[] BuildBlocksGraph(LevelData level, Dictionary<int, int> idToIndex, Mode mode, float dx, float dy)
    {
        int n = level.tiles.Length;
        var blocks = new List<int>[n];
        for (int i = 0; i < n; i++) blocks[i] = new List<int>(4);

        if (mode == Mode.A)
        {
            for (int i = 0; i < n; i++)
            {
                var t = level.tiles[i];
                if (t.children == null) continue;
                foreach (var childId in t.children)
                    if (idToIndex.TryGetValue(childId, out int ci)) blocks[i].Add(ci);
            }
            return blocks;
        }

        if (mode == Mode.B)
        {
            for (int i = 0; i < n; i++)
            {
                var t = level.tiles[i];
                if (t.children == null) continue;
                foreach (var parentId in t.children)
                    if (idToIndex.TryGetValue(parentId, out int pi)) blocks[pi].Add(i);
            }
            return blocks;
        }

        // geometric
        for (int i = 0; i < n; i++)
        {
            var a = level.tiles[i];
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var b = level.tiles[j];
                if (b.position.z >= a.position.z) continue;
                if (Mathf.Abs(a.position.x - b.position.x) <= dx &&
                    Mathf.Abs(a.position.y - b.position.y) <= dy)
                {
                    blocks[j].Add(i); // b (üstte) i (altta) bloklar
                }
            }
        }
        return blocks;
    }
    public TileViewController GetViewByTileIndex(int tileIndex)
    {
        if (_views == null || tileIndex < 0 || tileIndex >= _views.Count) return null;
        return _views[tileIndex];
    }
}
