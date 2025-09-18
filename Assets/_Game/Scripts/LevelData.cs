using System;

[Serializable]
public class LevelData
{
    public string title;
    public TileData[] tiles;
}

[Serializable]
public class TileData
{
    public int id;
    public Position position; // {x,y,z}
    public String character;    // harf
    public int[] children;    // bu taþýn engelledikleri (kapama iliþkisi)
}

[Serializable]
public class Position { public float x, y, z; }
