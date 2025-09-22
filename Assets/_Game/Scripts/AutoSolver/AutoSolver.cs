using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Helpers; 

public class AutoSolver : Singleton<AutoSolver>
{
    public bool TryFindBestOpenWord(out string bestWord)
        => TryFindBestOpenWord(out bestWord, out _);

    public bool TryFindBestOpenWord(out string bestWord, out List<int> bestPath)
    {
        bestWord = null;
        bestPath = null;

        var bm = BoardManager.Instance;
        var am = AnswerManager.Instance;
        var sm = ScoreManager.Instance;

        if (bm == null || am == null) return false;

        var openTiles = bm.GetOpenTilesSnapshot(); 
        if (openTiles == null || openTiles.Count == 0) return false;

        var letters = new List<(char ch, int idx)>(openTiles.Count);
        foreach (var v in openTiles)
            letters.Add((char.ToUpperInvariant(v.letter), v.tileIndex));

        int bestLenLocal = 0;
        int bestScoreLocal = -1;
        string bestWordLocal = null;
        List<int> bestPathLocal = null;

        var used = new bool[letters.Count];
        var sb = new StringBuilder(letters.Count);
        var pathIdx = new List<int>(letters.Count);

        void TryUpdateBestLocal()
        {
            string w = sb.ToString();
            if (w.Length < am.MinWordLength) return;
            if (!am.IsWord(w)) return;

            int score = sm != null ? sm.ComputeWordScore(w) : w.Length;
            if (w.Length > bestLenLocal || (w.Length == bestLenLocal && score > bestScoreLocal))
            {
                bestLenLocal = w.Length;
                bestScoreLocal = score;
                bestWordLocal = w;
                bestPathLocal = new List<int>(pathIdx);
            }
        }

        void Dfs()
        {
            string cur = sb.ToString();
            if (cur.Length > 0 && !am.IsPrefix(cur)) return;

            TryUpdateBestLocal();

            for (int i = 0; i < letters.Count; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                sb.Append(letters[i].ch);
                pathIdx.Add(letters[i].idx);

                Dfs();

                pathIdx.RemoveAt(pathIdx.Count - 1);
                sb.Length -= 1;
                used[i] = false;
            }
        }

        Dfs();

        bestWord = bestWordLocal;
        bestPath = bestPathLocal;
        return !string.IsNullOrEmpty(bestWord);
    }
}
