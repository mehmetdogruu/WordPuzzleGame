using UnityEngine;
using UnityEngine.EventSystems;

public class LetterHolderController : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot")]
    public int slotIndex;
    public RectTransform slotRect;

    public bool IsReserved { get; private set; }
    public bool IsOccupied { get; private set; }
    public HeldTileInfo Incoming { get; private set; }
    public HeldTileInfo Current { get; private set; }

    void Awake()
    {
        if (!slotRect) slotRect = GetComponent<RectTransform>();
    }

    public bool TryReserve()
    {
        if (IsReserved || IsOccupied) return false;
        IsReserved = true; return true;
    }

    public void SetIncoming(HeldTileInfo info) => Incoming = info;

    public void CommitIncoming()
    {
        IsReserved = false;
        IsOccupied = true;
        Current = Incoming;
        Incoming = null;

        if (Current != null && Current.view != null)
            Current.view.SetRaycastEnabled(false);
    }

    public void CancelIncoming()
    {
        IsReserved = false; Incoming = null;
    }

    public void Release()
    {
        IsReserved = false; IsOccupied = false;
        Incoming = null; Current = null;
    }

    public Vector2 GetTargetPosInBoardSpace(RectTransform boardRoot)
    {
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(null, slotRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPt, null, out var local);
        return local;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsOccupied) return;

        var mgr = LetterHolderManager.Instance;
        if (mgr == null) return;

        if (mgr.InputLocked) return;

        mgr.OnHolderClicked(this);
    }
}
