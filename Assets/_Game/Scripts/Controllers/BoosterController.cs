using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Helpers;
using TMPro;

public class BoosterController : Singleton<BoosterController>
{
    [Header("UI")]
    [SerializeField] private Button boosterButton;
    [SerializeField] private int maxUses = 2;
    [SerializeField] private TMP_Text useCountLeftText;

    [Header("Tween")]
    [SerializeField] private float moveSpeed = 900f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    private int usesLeft;
    private bool isRunning;
    bool lastBusyCache = false;

    void Awake()
    {
        usesLeft = maxUses;

        if (boosterButton)
        {
            boosterButton.onClick.RemoveAllListeners();
            boosterButton.onClick.AddListener(OnBoosterClicked);
        }

        RefreshButtonState(force: true);
    }

    void Update()
    {
        var lhm = LetterHolderManager.Instance;
        bool busy = lhm != null && lhm.HasAnyOccupied();
        if (busy != lastBusyCache)
        {
            lastBusyCache = busy;
            RefreshButtonState();
        }
    }

    public void ResetUses()
    {
        isRunning = false;
        usesLeft = maxUses;
        lastBusyCache = LetterHolderManager.Instance != null && LetterHolderManager.Instance.HasAnyOccupied();
        RefreshButtonState(force: true);
    }

    void RefreshButtonState(bool force = false)
    {
        if (!boosterButton) return;

        bool hasOccupied = LetterHolderManager.Instance != null && LetterHolderManager.Instance.HasAnyOccupied();
        bool interactable = !isRunning && usesLeft > 0 && !hasOccupied;

        if (force || boosterButton.interactable != interactable)
            boosterButton.interactable = interactable;
        useCountLeftText.text = usesLeft.ToString();
    }

    void OnBoosterClicked()
    {
        if (isRunning || usesLeft <= 0) return;

        if (LetterHolderManager.Instance != null && LetterHolderManager.Instance.HasAnyOccupied())
        {
            RefreshButtonState();
            return;
        }

        var solver = AutoSolver.Instance;
        if (solver == null || !solver.TryFindBestOpenWord(out string word, out List<int> path))
        {
            Debug.Log("Booster: uygun kelime yok.");
            return;
        }

        var bm = BoardManager.Instance;
        if (bm == null) return;

        var tilesToUse = new List<TileViewController>(path.Count);
        foreach (var idx in path)
        {
            var tv = bm.GetViewByTileIndex(idx);
            if (tv != null) tilesToUse.Add(tv);
        }

        if (tilesToUse.Count == 0)
        {
            Debug.Log("Booster: path boş/erişilemez.");
            return;
        }

        StartCoroutine(RunBoosterSequence(tilesToUse));
    }

    IEnumerator RunBoosterSequence(List<TileViewController> tilesInOrder)
    {
        isRunning = true;
        LetterHolderManager.Instance?.SetExternalInputLock(true);
        RefreshButtonState();

        yield return StartCoroutine(AutoPlaceTilesSequential(tilesInOrder));

        usesLeft = Mathf.Max(0, usesLeft - 1);
        isRunning = false;

        LetterHolderManager.Instance?.SetExternalInputLock(false);
        RefreshButtonState();
    }

    public IEnumerator AutoPlaceTilesSequential(List<TileViewController> tiles)
    {
        if (tiles == null || tiles.Count == 0) yield break;

        var lhm = LetterHolderManager.Instance;
        if (lhm == null || lhm.holders == null) yield break;

        int placed = CountOccupied(lhm.holders);

        foreach (var tv in tiles)
        {
            if (!tv) continue;

            yield return StartCoroutine(MoveTileToNextHolder(tv));
            yield return new WaitUntil(() => CountOccupied(lhm.holders) >= ++placed);
            yield return null;
        }
    }

    private IEnumerator MoveTileToNextHolder(TileViewController tv)
    {
        var lhm = LetterHolderManager.Instance;
        if (lhm == null || tv == null || lhm.holders == null || lhm.boardRoot == null) yield break;

        LetterHolderController holder = null;
        int slotIndex = -1;
        while (!lhm.TryReserveAtCursor(out slotIndex, out holder))
            yield return null;

        var rt = (RectTransform)tv.transform;
        var sourcePos = rt.anchoredPosition;
        var targetPos = lhm.GetTargetPosInBoardSpace(holder);

        holder.SetIncoming(new HeldTileInfo
        {
            tileIndex = tv.tileIndex,
            tileId = tv.tileId,
            letter = tv.letter,
            wasOpen = tv.frontFace && tv.frontFace.activeSelf,
            sourceAnchoredPos = sourcePos,
            sourceParent = (RectTransform)tv.transform.parent,
            view = tv
        });

        BoardManager.Instance?.OnTilePickingBegin(tv.tileIndex);

        tv.SetRaycastEnabled(false);
        if (tv.button) tv.button.interactable = false;

        float dist = Vector2.Distance(sourcePos, targetPos);
        float dur = Mathf.Max(0.05f, dist / Mathf.Max(1f, moveSpeed));

        bool committed = false;
        rt.DOAnchorPos(targetPos, dur)
          .SetEase(moveEase)
          .OnComplete(() =>
          {
              lhm.Commit(slotIndex, tv, sourcePos);
              committed = true;
          });

        yield return new WaitUntil(() => committed);
    }

    private int CountOccupied(LetterHolderController[] holders)
    {
        if (holders == null) return 0;
        int c = 0;
        foreach (var h in holders)
        {
            if (h == null || !h.IsOccupied || h.Current == null) break;
            c++;
        }
        return c;
    }
}
