/// <summary>
/// (Topdown) Tilemap Generator - TileData class
///
/// A TileData is the representation of a Cell in the Map of the Tilemap Generator.
///
/// When instantiated from the Timemap Generator, it will contain the following values already filled up:
/// - x         X position in the Map
/// - y         Y position in the Map
/// - worldX    X position in World Space
/// - worldY    Y position in World Space
/// - z         The result of the Perlin Noise calculation
/// - zLayer    Layer of the Map, corresponding to the index of the Rule Tiles list
///
/// You can add any other field to this class based on your game requirements, for easy access using the Tilemap Generator API,
/// but remember to update the constructor methods accordingly.
/// </summary>
[System.Serializable]
public class TileData
{
    public int x;
    public int y;
    public float worldX;
    public float worldY;
    public float z;
    public int zLayer;

    public TileData(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public TileData(TileData source)
    {
        x = source.x;
        y = source.y;
        z = source.z;
        zLayer = source.zLayer;
    }


    // You can add any other field or method below, based on your game requirements, but remember to update the constructor methods accordingly.

}
