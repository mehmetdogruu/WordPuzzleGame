using System.Collections;
using System.Collections.Generic;

public sealed class BoardState
{
    readonly TileData[] _tiles;
    readonly int[] _inDeg;
    readonly BitArray _alive;
    readonly Dictionary<int, int> _idToIndex;

    public BoardState(TileData[] tiles)
    {
        _tiles = tiles;
        _inDeg = new int[tiles.Length];
        _alive = new BitArray(tiles.Length, true);
        _idToIndex = new Dictionary<int, int>(tiles.Length);
        for (int i = 0; i < tiles.Length; i++) _idToIndex[tiles[i].id] = i;

        // indegree: beni KİM kapatıyor?
        for (int i = 0; i < tiles.Length; i++)
        {
            var t = tiles[i];
            if (t.children == null) continue;
            foreach (var childId in t.children)
            {
                if (_idToIndex.TryGetValue(childId, out var ci))
                    _inDeg[ci]++; // i, child'ı kapatıyor → child'ın üstünde biri var
            }
        }
    }

    public IEnumerable<int> OpenIndices()
    {
        for (int i = 0; i < _tiles.Length; i++)
            if (_alive[i] && _inDeg[i] == 0) yield return i;
    }
    public bool IsOpenIndex(int i) => _alive[i] && _inDeg[i] == 0;
    public bool IsAliveIndex(int i) => _alive[i];

    public struct PickAction
    {
        public int tileIndex;
        public (int childIdx, int prev)[] indegChanges;
    }

    /// Kartı tahtadan al: alive=false; child'ların indegree'ini azalt
    public PickAction Pick(int tileIndex)
    {
        _alive[tileIndex] = false;
        var t = _tiles[tileIndex];
        var list = new List<(int, int)>();
        if (t.children != null)
        {
            foreach (var childId in t.children)
            {
                if (_idToIndex.TryGetValue(childId, out var ci))
                {
                    int prev = _inDeg[ci];
                    _inDeg[ci] = prev - 1;
                    list.Add((ci, prev));
                }
            }
        }
        return new PickAction { tileIndex = tileIndex, indegChanges = list.ToArray() };
    }
}
