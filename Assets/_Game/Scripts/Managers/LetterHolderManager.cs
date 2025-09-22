using UnityEngine;
using DG.Tweening;
using Helpers;

public class LetterHolderManager : Singleton<LetterHolderManager>
{
    [Header("UI Refs")]
    public RectTransform boardRoot;
    public LetterHolderController[] holders;

    [Header("Anim")]
    public float returnSpeed = 3000f;
    public Ease returnEase = Ease.InOutSine;

    [Header("Runtime/Debug")]
    [SerializeField] int _insertCursor = 0;

    [SerializeField] bool inputLocked = false;

    [SerializeField] bool externalInputLock = false;

    public bool InputLocked => inputLocked || externalInputLock;

    void Awake()
    {
        if (holders != null)
            for (int i = 0; i < holders.Length; i++)
                if (holders[i] != null) holders[i].slotIndex = i;

        _insertCursor = 0;
    }

    // ---------- Public: Harici kilit kontrolü ----------
    public void SetExternalInputLock(bool state)
    {
        externalInputLock = state;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ---------- Reserve / Commit ----------
    public bool TryReserveAtCursor(out int slotIndex, out LetterHolderController holder)
    {
        holder = null;
        slotIndex = _insertCursor;
        if (!InRange(slotIndex)) return false;

        var h = holders[slotIndex];
        if (h != null && h.TryReserve()) { holder = h; return true; }
        return false;
    }

    public void Commit(int slotIndex, TileViewController tv, Vector2 sourceAnchoredPos)
    {
        if (!InRange(slotIndex) || tv == null) return;
        var h = holders[slotIndex];
        if (h == null) return;

        if (h.Incoming == null)
        {
            h.SetIncoming(new HeldTileInfo
            {
                tileIndex = tv.tileIndex,
                tileId = tv.tileId,
                letter = tv.letter,
                wasOpen = tv.frontFace && tv.frontFace.activeSelf,
                sourceAnchoredPos = sourceAnchoredPos,
                sourceParent = (RectTransform)tv.transform.parent,
                view = tv
            });
        }

        h.CommitIncoming();

        if (slotIndex == _insertCursor)
            _insertCursor = Mathf.Min(_insertCursor + 1, holders.Length - 1);

        AnswerManager.Instance?.RecomputeCurrentAnswer(holders);
    }

    public Vector2 GetTargetPosInBoardSpace(LetterHolderController holder)
    {
        if (holder == null || boardRoot == null) return Vector2.zero;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(null, holder.slotRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPt, null, out var local);
        return local;
    }

    // ---------- Return (range / one / undo) ----------
    public void OnHolderClicked(LetterHolderController clicked)
    {
        if (InputLocked) return; 
        if (clicked == null || holders == null) return;

        int start = clicked.slotIndex;
        if (!InRange(start)) return;

        _insertCursor = start;
        ReturnRange(start, holders.Length - 1);
    }

    void ReturnRange(int startIndex, int endIndex)
    {
        if (holders == null) return;

        SetInternalInputLock(true);
        int active = 0;

        for (int i = startIndex; i <= endIndex && i < holders.Length; i++)
        {
            var h = holders[i];
            if (h == null || !h.IsOccupied || h.Current == null) continue;

            active++;
            ReturnOne(h, () =>
            {
                if (--active <= 0) SetInternalInputLock(false);
            });
        }
        if (active == 0) SetInternalInputLock(false);
    }

    void ReturnOne(LetterHolderController h, System.Action onComplete)
    {
        var info = h.Current;
        if (info?.view == null) { h.Release(); onComplete?.Invoke(); return; }

        var rt = (RectTransform)info.view.transform;
        float dist = Vector2.Distance(rt.anchoredPosition, info.sourceAnchoredPos);
        float dur = Mathf.Max(0.05f, dist / Mathf.Max(1f, returnSpeed));

        rt.DOAnchorPos(info.sourceAnchoredPos, dur)
          .SetEase(returnEase)
          .OnComplete(() =>
          {
              h.Release();

              if (info.view)
              {
                  info.view.SetRaycastEnabled(true);
                  if (info.view.button) info.view.button.interactable = true;
              }

              BoardManager.Instance?.OnTileReturned(info.tileIndex);
              AnswerManager.Instance?.RecomputeCurrentAnswer(holders);

              if (h.slotIndex < _insertCursor)
                  _insertCursor = h.slotIndex;

              onComplete?.Invoke();
          });
    }

    // ---------- Internal input lock helper ----------
    void SetInternalInputLock(bool state)
    {
        inputLocked = state;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ---------- Helpers ----------
    bool InRange(int i) => holders != null && i >= 0 && i < holders.Length;

    public void ClearAllHoldersImmediate()
    {
        if (holders == null) return;
        foreach (var h in holders)
        {
            if (h == null) continue;
            if (h.Current?.view) Destroy(h.Current.view.gameObject);
            h.Release();
        }
    }

    public void ConsumeFromStart(int count, float fadeDur = 0.15f)
    {
        if (holders == null || count <= 0) return;

        for (int i = 0; i < count && i < holders.Length; i++)
        {
            var h = holders[i];
            if (h == null || !h.IsOccupied || h.Current == null) break;

            var view = h.Current.view;
            if (view)
            {
                var cg = view.canvasGroup;
                if (cg) cg.DOFade(0f, fadeDur).OnComplete(() => Destroy(view.gameObject));
                else Destroy(view.gameObject);
            }
            h.Release();
        }

        _insertCursor = 0;
        AnswerManager.Instance?.RecomputeCurrentAnswer(holders);
        BoardManager.Instance?.CheckEndAfterSubmit();
    }

    public void ConsumeFromStartAnimated(int count, float punch = 0.25f, float dur = 0.15f, System.Action onComplete = null)
    {
        if (holders == null || count <= 0) { onComplete?.Invoke(); return; }

        int done = 0, total = 0;
        for (int i = holders.Length - 1; i >= 0 && total < count; i--)
        {
            var h = holders[i];
            if (h == null || !h.IsOccupied || h.Current == null) continue;
            total++;

            ((RectTransform)h.Current.view.transform)
                .DOPunchScale(Vector3.one * punch, dur, 1, 0f)
                .SetDelay((total - 1) * dur * 0.2f)
                .OnComplete(() =>
                {
                    if (h.Current.view) Destroy(h.Current.view.gameObject);
                    h.Release();

                    if (++done == total)
                    {
                        _insertCursor = 0;
                        AnswerManager.Instance?.RecomputeCurrentAnswer(holders);
                        BoardManager.Instance?.CheckEndAfterSubmit();
                        onComplete?.Invoke();
                    }
                });
        }

        if (total == 0)
        {
            _insertCursor = 0;
            AnswerManager.Instance?.RecomputeCurrentAnswer(holders);
            BoardManager.Instance?.CheckEndAfterSubmit();
            onComplete?.Invoke();
        }
    }

    public int GetRightmostOccupiedIndex()
    {
        if (holders == null) return -1;
        for (int i = holders.Length - 1; i >= 0; i--)
        {
            var h = holders[i];
            if (h != null && h.IsOccupied && h.Current != null) return i;
        }
        return -1;
    }

    public bool HasAnyOccupied() => GetRightmostOccupiedIndex() >= 0;

    public void UndoLastMove()
    {
        if (holders == null) return;
        if (InputLocked) return; 

        int idx = GetRightmostOccupiedIndex();
        if (idx < 0) return;

        _insertCursor = idx;
        SetInternalInputLock(true);
        ReturnOne(holders[idx], () => SetInternalInputLock(false));
    }

    public void ResetAll()
    {
        ClearAllHoldersImmediate();
        _insertCursor = 0;
        AnswerManager.Instance?.RecomputeCurrentAnswer(holders);
    }
}
