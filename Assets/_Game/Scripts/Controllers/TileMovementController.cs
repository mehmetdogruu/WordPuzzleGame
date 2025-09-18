using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(TileViewController))]
public class TileMovementController : MonoBehaviour
{
    [Header("Motion")]
    public float moveSpeed = 900f;
    public Ease ease = Ease.Linear;

    public event Action<TileViewController, int> OnTileMoved; // BoardManager dinleyecek

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
        if (lhm != null && lhm.InputLocked) return;
        if (_isMoving || tv == null || tv.button == null || !tv.button.interactable) return;
        if (lhm == null || lhm.boardRoot == null) { Debug.LogWarning("LetterHolderManager yok/eksik."); return; }

        // 1) Yalnızca cursor’daki holder’ı rezerve et
        if (!lhm.TryReserveAtCursor(out int slotIndex, out var holder)) return;

        // 2) Hedef pozisyon (boardRoot uzayına)
        Vector2 target = lhm.GetTargetPosInBoardSpace(holder);

        // 3) Holder’a metadata’yı hareket BAŞLAMADAN ver
        var info = new HeldTileInfo
        {
            tileIndex = tv.tileIndex,
            tileId = tv.tileId,
            letter = tv.letter,
            wasOpen = tv.frontFace != null && tv.frontFace.activeSelf,
            sourceAnchoredPos = _rt.anchoredPosition,
            sourceParent = (RectTransform)tv.transform.parent,
            view = tv
        };
        holder.SetIncoming(info);

        // 4) 🔴 HAREKET BAŞLARKEN board mantığını güncelle → diğer kartlar HEMEN açılır
        BoardManager.Instance?.OnTilePickingBegin(tv.tileIndex);

        // 5) Tween
        float dist = Vector2.Distance(_rt.anchoredPosition, target);
        float dur = Mathf.Max(0.05f, dist / Mathf.Max(1f, moveSpeed));

        tv.button.interactable = false;
        _isMoving = true;

        _rt.DOAnchorPos(target, dur)
           .SetEase(ease)
           .OnComplete(() =>
           {
               _isMoving = false;

           // Holder occupy + cursor ilerlemesi
           lhm.Commit(slotIndex, tv, info.sourceAnchoredPos);

           // (Artık BoardManager’a “pick bitti” dememize gerek yok; mantık zaten başta uygulandı)
           // OnTileMoved?.Invoke(tv, holder.slotIndex); // İSTERSEN kaldır (artık gerekmez)
       })
           .OnKill(() =>
           {
               if (_isMoving)
               {
               // Rezerv iptal et ve mantığı GERİ AL (kart yerinden oynatılmamış say)
               holder.CancelIncoming();

                   BoardManager.Instance?.OnTilePickCanceled(tv.tileIndex);

                   _isMoving = false;
                   tv.button.interactable = true;
               }
           });
    }

}
