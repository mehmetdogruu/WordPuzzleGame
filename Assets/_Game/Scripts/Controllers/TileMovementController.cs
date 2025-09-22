using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(TileViewController))]
public class TileMovementController : MonoBehaviour
{
    [Header("Motion")]
    public float moveSpeed = 3000f;
    public Ease ease = Ease.InOutSine;

    public event Action<TileViewController, int> OnTileMoved; 

    TileViewController _tile;
    RectTransform _rt;
    bool _isMoving; 

    void Awake()
    {
        _tile = GetComponent<TileViewController>();
        _rt = (RectTransform)transform;
        _tile.OnClicked += HandleTileClicked;
    }

    void OnDestroy()
    {
        if (_tile != null) _tile.OnClicked -= HandleTileClicked;
    }

    void HandleTileClicked(TileViewController tv)
    {
        var lhm = LetterHolderManager.Instance;
        if (lhm == null || lhm.boardRoot == null) { Debug.LogWarning("LetterHolderManager yok/eksik."); return; }
        if (lhm.InputLocked || tv == null || tv.button == null || !tv.button.interactable) return;
        if (_isMoving) return;

        if (!lhm.TryReserveAtCursor(out int slotIndex, out var holder)) return;

        Vector2 sourcePos = _rt.anchoredPosition;
        Vector2 targetPos = lhm.GetTargetPosInBoardSpace(holder);

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

        // Tween
        float dist = Vector2.Distance(sourcePos, targetPos);
        float dur  = Mathf.Max(0.05f, dist / Mathf.Max(1f, moveSpeed));

        _isMoving = true;

        _rt.DOAnchorPos(targetPos, dur)
           .SetEase(ease)
           .OnComplete(() =>
           {
               _isMoving = false;

               lhm.Commit(slotIndex, tv, sourcePos);

               OnTileMoved?.Invoke(tv, slotIndex);
           })
           .OnKill(() =>
           {
               if (_isMoving)
               {
                   holder.CancelIncoming();
                   BoardManager.Instance?.OnTilePickCanceled(tv.tileIndex);

                   _isMoving = false;
                   tv.SetRaycastEnabled(true);
                   if (tv.button) tv.button.interactable = true;
               }
           });
    }
}
