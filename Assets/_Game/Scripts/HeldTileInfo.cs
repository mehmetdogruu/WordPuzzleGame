using UnityEngine;

[System.Serializable]
public class HeldTileInfo
{
    public int tileIndex;
    public int tileId;
    public char letter;
    public bool wasOpen;

    public Vector2 sourceAnchoredPos;     // boardRoot uzay�nda
    public RectTransform sourceParent;    // genelde BoardRoot
    public TileViewController view;       // g�rsel referans�
}
