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
    public Position position; 
    public String character;    
    public int[] children;   
}

[Serializable]
public class Position { public float x, y, z; }
