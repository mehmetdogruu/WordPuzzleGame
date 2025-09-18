using UnityEngine;
using DG.Tweening;
using Helpers;

/// <summary>
/// LetterHolder'ları yönetir: rezerv (yalnızca _insertCursor), commit ve geri dönüş (return).
/// Return animasyonları sırasında input'u kilitler.
/// </summary>
public class LetterHolderManager : Singleton<LetterHolderManager>
{
    [Header("UI Refs")]
    [Tooltip("Board üzerindeki tüm tile'ların parent'ı (UI). DOAnchorPos için referans uzay.")]
    public RectTransform boardRoot;

    [Tooltip("Sahnedeki LetterHolderController'lar (soldan sağa sırayla).")]
    public LetterHolderController[] holders;

    [Header("Anim")]
    [Tooltip("Geri dönüş hız sabiti (px/sn).")]
    public float returnSpeed = 900f;

    [Tooltip("Geri dönüş easing'i.")]
    public Ease returnEase = Ease.Linear;

    [Header("Runtime/Debug")]
    [SerializeField, Tooltip("Bir sonraki yerleşimin hedef index'i.")]
    private int _insertCursor = 0;

    [SerializeField, Tooltip("Return animasyonu sırasında true olur; tıklamalar devre dışı.")]
    private bool inputLocked = false;
    public bool InputLocked => inputLocked;

    // ====================== LIFECYCLE ======================
    private void Awake()
    {

        // Dizi sırasını slotIndex ile senkronla (güvence)
        if (holders != null)
        {
            for (int i = 0; i < holders.Length; i++)
                if (holders[i] != null) holders[i].slotIndex = i;
        }

        // Başlangıçta ilk hedef 0
        _insertCursor = 0;
    }

    // ====================== REZERV / YERLEŞİM ======================

    /// <summary>
    /// Sadece _insertCursor'daki holder'ı rezerve eder. Uygun değilse false döner.
    /// </summary>
    public bool TryReserveAtCursor(out int slotIndex, out LetterHolderController holder)
    {
        holder = null;
        slotIndex = _insertCursor;

        if (!InRange(slotIndex)) return false;

        var h = holders[slotIndex];
        if (h != null && h.TryReserve())
        {
            holder = h;
            return true;
        }
        return false; // hedef slot dolu/rezerv → bekle
    }

    /// <summary>
    /// Tween başarıyla bittiğinde çağır. Holder'ı occupy yapar ve cursor'ı ilerletir.
    /// </summary>
    public void Commit(int slotIndex, TileViewController tv, Vector2 sourceAnchoredPos)
    {
        if (!InRange(slotIndex) || tv == null) return;
        var h = holders[slotIndex];
        if (h == null) return;

        // Eğer Incoming dışarıdan set edilmediyse güvenlik için doldur.
        if (h.Incoming == null)
        {
            var info = new HeldTileInfo
            {
                tileIndex = tv.tileIndex,
                tileId = tv.tileId,
                letter = tv.letter,
                wasOpen = tv.frontFace != null && tv.frontFace.activeSelf,
                sourceAnchoredPos = sourceAnchoredPos,
                sourceParent = (RectTransform)tv.transform.parent,
                view = tv
            };
            h.SetIncoming(info);
        }

        h.CommitIncoming(); // IsOccupied = true, Current = Incoming, tile raycast kapat (LHC içinde)

        // Eğer yerleşim cursor'da gerçekleştiyse sıradaki hedefe ilerle
        if (slotIndex == _insertCursor)
            _insertCursor = Mathf.Min(_insertCursor + 1, holders.Length - 1);

        AnswerManager.Instance?.RecomputeCurrentAnswer(holders);

    }

    /// <summary>
    /// UI hedefini boardRoot uzayına çevir (Tween için güvenli).
    /// </summary>
    public Vector2 GetTargetPosInBoardSpace(LetterHolderController holder)
    {
        if (holder == null || boardRoot == null) return Vector2.zero;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(null, holder.slotRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPt, null, out var local);
        return local;
    }

    // ====================== RETURN (GERİ DÖNÜŞ) ======================

    /// <summary>
    /// Dolu bir holder'a tıklanınca çağrılır: tıklanan index'ten SONRAKİ tüm doluları geri döndür.
    /// Ayrıca yeni yerleşimler tıklanan index'ten başlamalı (cursor ayarı).
    /// </summary>
    public void OnHolderClicked(LetterHolderController clicked)
    {
        if (clicked == null || holders == null) return;

        int start = -1;
        for (int i = 0; i < holders.Length; i++)
        {
            if (holders[i] == clicked) { start = i; break; }
        }
        if (start < 0) return;

        // Bundan sonra yerleşimler tam bu index'ten başlayacak
        _insertCursor = start;

        // Sadece start..son aralığını boşalt (return animasyonu boyunca input kilitli)
        ReturnRange(start, holders.Length - 1);
    }

    private void ReturnRange(int startIndex, int endIndex)
    {
        if (holders == null) return;

        SetInputLock(true); // 🔒 input kapat

        int activeTweens = 0;

        for (int i = startIndex; i <= endIndex && i < holders.Length; i++)
        {
            var h = holders[i];
            if (h == null || !h.IsOccupied || h.Current == null) continue;

            activeTweens++;
            ReturnOne(h, () =>
            {
                activeTweens--;
                if (activeTweens <= 0)
                    SetInputLock(false); // 🔓 hepsi bitince input aç
            });
        }

        if (activeTweens == 0)
            SetInputLock(false); // dönecek kart yoksa hemen aç
    }

    /// <summary>
    /// Tek bir holder'daki kartı kaynak board pozisyonuna tween'leyip boşaltır.
    /// </summary>
    private void ReturnOne(LetterHolderController h, System.Action onComplete)
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
              // Holder'ı boşalt
              h.Release();

              // Tile tekrar board'da tıklanabilsin
              if (info.view != null)
              {
                  info.view.SetRaycastEnabled(true);
                  if (info.view.button) info.view.button.interactable = true;
              }

              // Board mantığını geri al (alive/indegree/open)
              BoardManager.Instance?.OnTileReturned(info.tileIndex);
              AnswerManager.Instance?.RecomputeCurrentAnswer(holders);


              // Cursor'ı gerekirse geriye çek (sol tarafa boşluk açıldıysa)
              if (h.slotIndex < _insertCursor)
                  _insertCursor = h.slotIndex;

              onComplete?.Invoke();
          });
    }

    // ====================== INPUT LOCK ======================
    private void SetInputLock(bool state)
    {
        inputLocked = state;
        // İstersen burada global UI interactivity kapatma/açma da yapabilirsin.
        // Şimdilik TileMovementController tarafında InputLocked kontrolü yeterli.
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ====================== Yardımcılar ======================
    public void CancelReserve(int slotIndex)
    {
        if (!InRange(slotIndex)) return;
        var h = holders[slotIndex];
        if (h != null && h.IsReserved) h.CancelIncoming();
    }

    public bool IsSlotBusy(int slotIndex)
    {
        if (!InRange(slotIndex)) return true;
        var h = holders[slotIndex];
        return h != null && (h.IsReserved || h.IsOccupied);
    }
    public void ClearAllHoldersImmediate()
    {
        if (holders == null) return;
        foreach (var h in holders)
        {
            if (h == null) continue;
            if (h.Current != null && h.Current.view != null)
                Destroy(h.Current.view.gameObject);
            h.Release(); // IsReserved/IsOccupied reset + alanı boşalt
        }
    }


        private bool InRange(int i) => holders != null && i >= 0 && i < holders.Length;
    /// <summary>
    /// Baştan itibaren 'count' adet dolu holder'daki tile'ları fade edip yok eder, holder'ları boşaltır.
    /// </summary>
    public void ConsumeFromStart(int count, float fadeDur = 0.15f)
    {
        if (holders == null || count <= 0) return;

        for (int i = 0; i < count && i < holders.Length; i++)
        {
            var h = holders[i];
            if (h == null || !h.IsOccupied || h.Current == null) break;

            var view = h.Current.view;
            if (view != null)
            {
                // Raycast zaten kapalı olmalı; görseli yumuşakça yok edelim
                var cg = view.canvasGroup;
                if (cg != null)
                {
                    cg.DOFade(0f, fadeDur).OnComplete(() =>
                    {
                        Destroy(view.gameObject);
                    });
                }
                else
                {
                    Destroy(view.gameObject);
                }
            }

            // Holder'ı boşalt (BoardManager'a haber YOK—bu harfler tüketildi)
            h.Release();
        }

        // Submit sonrası yeni eklemeler baştan başlayacağı için cursor'ı sıfırla
        _insertCursor = 0;

        // Cevabı yeniden hesaplat (boş)
        AnswerManager.Instance?.RecomputeCurrentAnswer(holders);
    }
}
