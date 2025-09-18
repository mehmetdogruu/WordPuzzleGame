using System.Collections.Generic;
using UnityEngine;

public class BoardManager : SceneSingleton<BoardManager>
{
    [Header("Algılama")]
    public float overlapDx = 14f;
    public float overlapDy = 14f;

    [Header("Debug")]
    [SerializeField] private List<int> openTileIds = new();
    [SerializeField] private List<int> closedTileIds = new();

    private LevelData _level;
    private List<TileViewController> _views;   // 🔴 index == tileIndex
    private Dictionary<int, int> _idToIndex;

    private int[] _indegree;
    private bool[] _alive;
    private List<int>[] _blocks;

    private readonly HashSet<int> _picked = new();
    private readonly HashSet<int> _inFlight = new();

    private enum Mode { A, B, Geometric }
    private Mode _mode;

    public void Initialize(LevelData level,
                           List<TileViewController> tileViews,
                           Dictionary<int, int> idToIndex)
    {
        _level = level;
        _views = tileViews;   // 🔴 artık index == tileIndex
        _idToIndex = idToIndex;

        int n = _level.tiles.Length;

        _indegree = ComputeIndegreeAndMode(_level, _idToIndex, overlapDx, overlapDy, out _mode);
        _blocks = BuildBlocksGraph(_level, _idToIndex, _mode, overlapDx, overlapDy);

        _alive = new bool[n];
        for (int i = 0; i < n; i++) _alive[i] = true;

        _picked.Clear();
        _inFlight.Clear();

        RefreshAllOpenStates();
        RefreshDebugLists();
    }

    // --- Tile lifecycle ---

    public void OnTilePickingBegin(int tileIndex)
    {
        if (!IsValidIndex(tileIndex) || !_alive[tileIndex]) return;

        _alive[tileIndex] = false;
        _picked.Add(tileIndex);
        _inFlight.Add(tileIndex);

        var list = _blocks[tileIndex];
        if (list != null)
            for (int k = 0; k < list.Count; k++)
                _indegree[list[k]] = Mathf.Max(0, _indegree[list[k]] - 1);

        if (list != null)
            for (int k = 0; k < list.Count; k++) ApplyOpenState(list[k]);

        RefreshDebugLists();
    }

    public void OnTilePickCanceled(int tileIndex)
    {
        if (!IsValidIndex(tileIndex) || !_inFlight.Contains(tileIndex)) return;

        _inFlight.Remove(tileIndex);
        _picked.Remove(tileIndex);
        _alive[tileIndex] = true;

        FullResync();
    }

    public void OnTileReturned(int tileIndex)
    {
        if (!IsValidIndex(tileIndex)) return;

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
            for (int k = 0; k < list.Count; k++)
                _indegree[list[k]]++;
        }

        RefreshAllOpenStates();
        RefreshDebugLists();
    }

    public void CheckEndAfterSubmit()
    {
        if (IsBoardEmpty())
            GameFlowManager.Instance?.OnLevelCompletedNoTiles();
    }

    public bool IsBoardEmpty()
    {
        if (_alive == null) return true;
        for (int i = 0; i < _alive.Length; i++) if (_alive[i]) return false;
        return true;
    }

    // --- Open/Close ---

    private void RefreshAllOpenStates()
    {
        if (_views == null) return;
        for (int i = 0; i < _views.Count; i++) ApplyOpenState(i);
    }

    private void ApplyOpenState(int tileIndex)
    {
        if (!IsValidIndex(tileIndex)) return;
        var view = _views[tileIndex];
        if (!view) return;

        if (_picked.Contains(tileIndex)) return; // holder'da açık kalsın
        if (!_alive[tileIndex]) return;

        view.SetOpen(_indegree[tileIndex] == 0);
    }

    private bool IsValidIndex(int i) => _views != null && i >= 0 && i < _views.Count;

    private void RefreshDebugLists()
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

    // --- Graph / mode ---

    private int[] ComputeIndegreeAndMode(LevelData level, Dictionary<int, int> idToIndex,
                                         float dx, float dy, out Mode mode)
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

    private List<int>[] BuildBlocksGraph(LevelData level, Dictionary<int, int> idToIndex,
                                         Mode mode, float dx, float dy)
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
                    if (idToIndex.TryGetValue(childId, out int ci))
                        blocks[i].Add(ci);
            }
        }
        else if (mode == Mode.B)
        {
            for (int i = 0; i < n; i++)
            {
                var t = level.tiles[i];
                if (t.children == null) continue;
                foreach (var parentId in t.children)
                    if (idToIndex.TryGetValue(parentId, out int pi))
                        blocks[pi].Add(i);
            }
        }
        else // geometric
        {
            for (int i = 0; i < n; i++)
            {
                var a = level.tiles[i];
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    var b = level.tiles[j];
                    if (b.position.z >= a.position.z) continue; // b üstte ise b.z < a.z
                    if (Mathf.Abs(a.position.x - b.position.x) <= dx &&
                        Mathf.Abs(a.position.y - b.position.y) <= dy)
                    {
                        blocks[j].Add(i); // aynen kalsın: b (üstte) i (altta) bloklar
                    }
                }
            }
        }

        return blocks;
    }
}
