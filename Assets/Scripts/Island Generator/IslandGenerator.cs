using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class IslandGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct TilemapLayer
    {
        public RuleTile ruleTile;
        public float weight;
    }
    public TilemapLayer[] tilemapLayers;
    public Tilemap sourceTilemap;
    public int width;
    public int height;
    public bool randomSeed;
    public string seed;
    [Range(0.01f, 1f)]
    public float scale;
    public bool fadeOut;
    public int islandWaterBorder;

    [Header("Progressive Generator")]
    public int tilePoints;
    public int pointsPerFrame;

    TileData[,] _tiles;
    Tilemap[] _tilemaps;
    Tilemap _sourceTilemapCopy;
    int _zLayers;
    float _totalWeight;
    float _xSeed;
    float _ySeed;
    string _seed;
    int _pointsDone;
    int _pointsPrev;
    bool _generated;

    [HideInInspector]
    public Grid grid;

    public bool Generated
    {
        get => _generated;
        set => _generated = value;
    }

    /// <summary>
    /// The total amount of computational points needed to generate the island
    /// </summary>
    public int TotalAmount
    {
        get
        {
            IEnumerator passiveGenerate = ProgressiveGenerate(false);
            while (passiveGenerate.MoveNext())
            {
            }
            return _pointsDone;
        }
    }

    /// <summary>
    /// TileData matrix representing the (calculated) tiles
    /// </summary>
    public TileData[,] Map => _tiles;

    /// <summary>
    /// Distance from tile World Position to the actual center
    /// </summary>
    /// <returns>
    /// A Vector3 containing the offset to add to the tile position for getting its center
    /// </returns>
    public Vector3 TileOffset => new Vector3(grid.cellSize.x / 2f, grid.cellSize.y / 2f, 0f);

    /// <summary>
    /// Converts a (x, y) position from Map to Grid
    /// </summary>
    /// <param name="x">Map X positon</param>
    /// <param name="y">Map Y positon</param>
    /// <returns>
    /// A Vector3Int representing the requested position
    /// </returns>
    public Vector3Int MapToGrid(int x, int y)
    {
        return new Vector3Int(x - width / 2, y - height / 2 , 0);
    }

    /// <summary>
    /// Converts a Vector3Int position from Grid to Map
    /// </summary>
    /// <param name="pos">Cell position in the grid</param>
    /// <returns>
    /// A Vector3Int representing the requested position
    /// </returns>
    public Vector3Int GridToMap(Vector3Int pos)
    {
        return new Vector3Int(pos.x + width / 2, pos.y + height / 2, 0);
    }

    /// <summary>
    /// Converts a (x, y) position from Grid to Map
    /// </summary>
    /// <param name="x">Cell X positon in the grid</param>
    /// <param name="y">Cell Y positon in the grid</param>
    /// <returns>
    /// A Vector3Int representing the requested position
    /// </returns>
    public Vector3Int GridToMap(int x, int y)
    {
        return new Vector3Int(x + width / 2, y + height / 2, 0);
    }

    /// <summary>
    /// Returns a TileData object from the specified position in the Map
    /// </summary>
    /// <param name="pos">Map positon</param>
    /// <returns>
    /// The requested TileData, or "null" if not found
    /// </returns>
    public TileData GetTileData(Vector3Int pos)
    {
        if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
        {
            return _tiles[pos.x, pos.y];
        }
        return null;
    }

    /// <summary>
    /// Returns a TileData object from the specified Cell position on the grid
    /// </summary>
    /// <param name="pos">Cells positon</param>
    /// <returns>
    /// The requested TileData, or "null" if not found
    /// </returns>
    public TileData GetTileDataGrid(Vector3Int pos)
    {
        return GetTileData(GridToMap(pos));
    }

    /// <summary>
    /// Returns a TileData object from the specified World coordinates
    /// </summary>
    /// <param name="pos">World-Space positon</param>
    /// <returns>
    /// The requested TileData, or "null" if not found
    /// </returns>
    public TileData GetTileDataWorld(Vector3 pos)
    {
        return GetTileDataGrid(grid.WorldToCell(pos));
    }

    /// <summary>
    /// Utility method to calculate an ellipse
    /// </summary>
    /// <param name="y">y coordinates</param>
    /// <returns>
    /// The "x" value for the specified "y" coordinate
    /// </returns>
    public int EllipseX(int y)
    {
        float a = width / 2f;
        float b = height / 2f;
        float fY = y;

        return Mathf.RoundToInt(Mathf.Sqrt((1f - (fY * fY) / (b * b)) * (a * a)));
    }

    /// <summary>
    /// The first tile of specified layer from and edge of an ellipse surrounding the island
    /// </summary>
    /// <param name="side">Quadrant (0 Up Right, 1 Down Right, 2 Down Left, 3 Up Left), default random</param>
    /// <param name="y">Y coodinate at the specified side (quadrant), default random</param>
    /// <param name="layer">Layer to check for, default 1</param>
    /// <returns>
    /// The requested tile
    /// </returns>
    public TileData EdgeTile(int side = -1, int y = -1, int layer = 1)
    {
        if (side == -1)
        {
            side = Random.Range(0, 4);
        }
        if (y == -1)
        {
            y = Random.Range(0, height / 2);
        }
        int x = EllipseX(y);
        TileData res = null;

        if (side == 0)
        {
            Vector3Int mapPosition = GridToMap(x, y);
            int xMap = mapPosition.x;
            int yMap = mapPosition.y;
            while (_tiles[xMap, yMap].zLayer < layer)
            {
                xMap--;
                yMap--;
            }
            res = _tiles[xMap, yMap];
        }
        else if (side == 1)
        {
            Vector3Int mapPosition = GridToMap(x, -y);
            int xMap = mapPosition.x;
            int yMap = mapPosition.y;
            while (_tiles[xMap, yMap].zLayer < layer)
            {
                xMap--;
                yMap++;
            }
            res = _tiles[xMap, yMap];
        }
        else if (side == 2)
        {
            Vector3Int mapPosition = GridToMap(-x, -y);
            int xMap = mapPosition.x;
            int yMap = mapPosition.y;
            while (_tiles[xMap, yMap].zLayer < layer)
            {
                xMap++;
                yMap++;
            }
            res = _tiles[xMap, yMap];
        }
        else if (side == 3)
        {
            Vector3Int mapPosition = GridToMap(-x, y);
            int xMap = mapPosition.x;
            int yMap = mapPosition.y;
            while (_tiles[xMap, yMap].zLayer < layer)
            {
                xMap++;
                yMap--;
            }
            res = _tiles[xMap, yMap];
        }

        return res;
    }

    /// <summary>
    /// Generates the island
    /// </summary>
    public void Generate()
    {
        _tiles = null;
        IEnumerator passiveGenerate = ProgressiveGenerate();
        while (passiveGenerate.MoveNext())
        {
        }
    }

    /// <summary>
    /// A coroutine which generates the island, executing a limited amount of operations each frame
    /// </summary>
    /// <param name="active">Use "false" to just simulate the generation. Default: true</param>
    /// <param name="callback">Action that will be executed each frame, passing the current amount of calculated points as parameter. Default: null</param>
    public IEnumerator ProgressiveGenerate(bool active = true, System.Action<int> callback = null)
    {
        _pointsDone = 0;
        _pointsPrev = 0;
        if (grid == null)
        {
            grid = GetComponent<Grid>();
        }
        GetSeedValues();
        if (active)
        {
            GenerateTilemaps();
        }
        _pointsDone++;
        if (_pointsDone - _pointsPrev > pointsPerFrame)
        {
            _pointsPrev = _pointsDone;
            if (callback != null) callback(_pointsDone);
            yield return null;
        }

        // Calculate "raw" perlin noise for all the tiles
        _tiles = new TileData[width, height];

        float zMin = 1f;
        float zMax = -1f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                _tiles[x, y] = new TileData(x, y);

                Vector3 worldPos = grid.CellToWorld(MapToGrid(x, y));
                _tiles[x, y].worldX = worldPos.x;
                _tiles[x, y].worldY = worldPos.y;
                float d = Mathf.Abs((float) x / width - .5f) + Mathf.Abs((float) y / height - .5f);
                _tiles[x, y].z = Mathf.PerlinNoise((x + _xSeed) / width / scale, (y + _ySeed) / height / scale);
                if (islandWaterBorder > 0)
                {
                    _tiles[x, y].z -= d;
                }

                if (_tiles[x, y].z < zMin)
                {
                    zMin = _tiles[x, y].z;
                }
                else if (_tiles[x, y].z > zMax)
                {
                    zMax = _tiles[x, y].z;
                }
            }
            _pointsDone++;
            if (_pointsDone - _pointsPrev > pointsPerFrame)
            {
                _pointsPrev = _pointsDone;
                if (callback != null) callback(_pointsDone);
                yield return null;
            }
        }


        // Normalize perlin noise
        _totalWeight = 0f;
        foreach (TilemapLayer tilemapLayer in tilemapLayers)
        {
            _totalWeight += tilemapLayer.weight;
        }

        _zLayers = tilemapLayers.Length > 1 ? tilemapLayers.Length : 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float z = (_tiles[x, y].z - zMin) / (zMax - zMin);

                _tiles[x, y].z = z;
                float amount = 0f;
                int zLayer = 0;
                float weight = zLayer < tilemapLayers.Length ? tilemapLayers[zLayer].weight : 1f;
                while (amount + weight < z * _totalWeight)
                {
                    amount += weight;
                    zLayer++;
                    weight = zLayer < tilemapLayers.Length ? tilemapLayers[zLayer].weight : 1f;
                }

                _tiles[x, y].zLayer = zLayer < tilemapLayers.Length ? zLayer : tilemapLayers.Length - 1;
            }
            _pointsDone++;
            if (_pointsDone - _pointsPrev > pointsPerFrame)
            {
                _pointsPrev = _pointsDone;
                if (callback != null) callback(_pointsDone);
                yield return null;
            }
        }

        if (islandWaterBorder > 0)
        {
            FadeBorders();
            _pointsDone++;
            if (_pointsDone - _pointsPrev > pointsPerFrame)
            {
                _pointsPrev = _pointsDone;
                if (callback != null) callback(_pointsDone);
                yield return null;
            }
        }

        // Draw tiles
        for (int y = 0; y < height; y++)
        {
            // place main tiles for each layer
            for (int x = 0; x < width; x++)
            {
                Vector3Int pos = MapToGrid(x, y);
                int index;
                if (_zLayers == 0)
                {
                    index = 0;
                }
                else
                {
                    index = _tiles[x, y].zLayer < _zLayers ? _tiles[x, y].zLayer : _zLayers - 1;
                }

                SetTile(index, pos, active);
                if (_pointsDone - _pointsPrev > pointsPerFrame)
                {
                    _pointsPrev = _pointsDone;
                    if (callback != null) callback(_pointsDone);
                    yield return null;
                }
            }
        }

        if (fadeOut)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int pos = MapToGrid(x, y);
                    int index = _tiles[x, y].zLayer;

                    // up
                    if ((y < height - 1) && (_tiles[x, y + 1].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x, pos.y + 1, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }

                    // up-right
                    if ((y < height - 1) && (x < width - 1) && (_tiles[x + 1, y + 1].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x + 1, pos.y + 1, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }

                    // right
                    if ((x < width - 1) && (_tiles[x + 1, y].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x + 1, pos.y, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }

                    // right-down
                    if ((y > 0) && (x < width - 1) && (_tiles[x + 1, y - 1].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x + 1, pos.y - 1, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }

                    // down
                    if ((y > 0) && (x < width - 1) && (_tiles[x, y - 1].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x, pos.y - 1, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }

                    // down-left
                    if ((x > 0) && (y > 0) && (_tiles[x - 1, y - 1].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x - 1, pos.y - 1, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }

                    // left
                    if ((x > 0) && (_tiles[x - 1, y].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x - 1, pos.y, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }

                    // left-up
                    if ((y < height - 1) && (x > 0) && (_tiles[x - 1, y + 1].zLayer == index - 1))
                    {
                        SetTile(index, new Vector3Int(pos.x - 1, pos.y + 1, pos.z), active);
                        if (_pointsDone - _pointsPrev > pointsPerFrame)
                        {
                            _pointsPrev = _pointsDone;
                            if (callback != null) callback(_pointsDone);
                            yield return null;
                        }
                    }
                }
            }
        }

        if (islandWaterBorder > 0)
        {
            for (int x = -islandWaterBorder; x < width + islandWaterBorder; x++)
            {
                for (int y = -islandWaterBorder; y < 0; y++)
                {
                    SetTile(0, MapToGrid(x, y), active);
                    if (_pointsDone - _pointsPrev > pointsPerFrame)
                    {
                        _pointsPrev = _pointsDone;
                        if (callback != null) callback(_pointsDone);
                        yield return null;
                    }

                    Vector3Int bPos = new Vector3Int(x - width / 2, y - height / 2 + islandWaterBorder + height, 0);
                    SetTile(0, bPos, active);
                    if (_pointsDone - _pointsPrev > pointsPerFrame)
                    {
                        _pointsPrev = _pointsDone;
                        if (callback != null) callback(_pointsDone);
                        yield return null;
                    }
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = -islandWaterBorder; x < 0; x++)
                {
                    SetTile(0, MapToGrid(x, y), active);
                    if (_pointsDone - _pointsPrev > pointsPerFrame)
                    {
                        _pointsPrev = _pointsDone;
                        if (callback != null) callback(_pointsDone);
                        yield return null;
                    }

                    Vector3Int rPos = new Vector3Int(x - width / 2 + islandWaterBorder + width, y - height / 2, 0);
                    SetTile(0, rPos, active);
                    if (_pointsDone - _pointsPrev > pointsPerFrame)
                    {
                        _pointsPrev = _pointsDone;
                        if (callback != null) callback(_pointsDone);
                        yield return null;
                    }
                }
            }
        }
    }

    void GetSeedValues()
    {
        if (!_generated)
        {
            if (randomSeed || seed.Length == 0)
            {
                seed = RandomSeed(Random.Range(5, 11));
                seed = seed[0].ToString().ToUpper() + seed.Substring(1);
            }

            _seed = seed;
            if (_seed.Length == 1)
            {
                _seed += seed;
            }
            _generated = true;
        }

        string xStringSeed = _seed.Substring(0, _seed.Length / 2);
        _xSeed = 0f;
        for (int i = 0; i < xStringSeed.Length; i++)
        {
            _xSeed += xStringSeed[i];
        }

        string yStringSeed = _seed.Substring(_seed.Length / 2, _seed.Length / 2 - (_seed.Length % 2 == 0 ? 0 : 1));
        _ySeed = 0f;
        for (int i = 0; i < yStringSeed.Length; i++)
        {
            _ySeed += yStringSeed[i];
        }
    }

    void GenerateTilemaps()
    {
        // remove old (generated) tilemaps
        if (sourceTilemap.transform.childCount > 0)
        {
            for (int i = sourceTilemap.transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(sourceTilemap.transform.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(sourceTilemap.transform.GetChild(i).gameObject);
                }
            }
        }

        // Instantiate new tilemaps
        _tilemaps = new Tilemap[tilemapLayers.Length];

        _sourceTilemapCopy = Instantiate(sourceTilemap, grid.transform.position, grid.transform.rotation, grid.transform);
        _sourceTilemapCopy.gameObject.name = "Empty Tilemap";
        for (int i = 0; i < tilemapLayers.Length; i++)
        {
            //tilemaps[i] = Instantiate(sourceTilemap, grid.transform.position, grid.transform.rotation, grid.transform);
            _tilemaps[i] = Instantiate(_sourceTilemapCopy);
            _tilemaps[i].gameObject.name = "Tilemap " + i;
            _tilemaps[i].transform.SetParent(sourceTilemap.transform);
            _tilemaps[i].GetComponent<TilemapRenderer>().sortingOrder = _tilemaps.Length - i;
            _tilemaps[i].ClearAllTiles();
        }

        _sourceTilemapCopy.transform.SetParent(sourceTilemap.transform);
    }

    void FadeBorders()
    {
        // top side
        float zAmount = 0f;
        int zLayer = 0;
        for (int y = height - 1; y > height - _zLayers - 2; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (_tiles[x, y].z * _totalWeight > zAmount)
                {
                    _tiles[x, y].z = zAmount / _totalWeight;
                    _tiles[x, y].zLayer = zLayer;
                }
            }

            zAmount += zLayer < tilemapLayers.Length ? tilemapLayers[zLayer].weight : 1f;
            zLayer++;
        }

        // right side
        zAmount = 0f;
        zLayer = 0;
        for (int x = width - 1; x > width - _zLayers - 2; x--)
        {
            for (int y = 0; y < height; y++)
            {
                if (_tiles[x, y].z * _totalWeight > zAmount)
                {
                    _tiles[x, y].z = zAmount / _totalWeight;
                    _tiles[x, y].zLayer = zLayer;
                }
            }

            zAmount += zLayer < tilemapLayers.Length ? tilemapLayers[zLayer].weight : 1f;
            zLayer++;
        }

        // bottom side
        zAmount = 0f;
        zLayer = 0;
        for (int y = 0; y < _zLayers - 1; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (_tiles[x, y].z * _totalWeight > zAmount)
                {
                    _tiles[x, y].z = zAmount / _totalWeight;
                    _tiles[x, y].zLayer = zLayer;
                }
            }

            zAmount += zLayer < tilemapLayers.Length ? tilemapLayers[zLayer].weight : 1f;
            zLayer++;
        }

        // left side
        zAmount = 0f;
        zLayer = 0;
        for (int x = 0; x < _zLayers - 1; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_tiles[x, y].z * _totalWeight > zAmount)
                {
                    _tiles[x, y].z = zAmount / _totalWeight;
                    _tiles[x, y].zLayer = zLayer;
                }
            }

            zAmount += zLayer < tilemapLayers.Length ? tilemapLayers[zLayer].weight : 1f;
            zLayer++;
        }
    }

    void SetTile(int index, Vector3Int pos, bool active = true)
    {
        if (active)
        {
            _tilemaps[index].SetTile(pos, tilemapLayers[index].ruleTile);
        }
        _pointsDone += tilePoints;
    }

    static string RandomSeed(int length)
    {
        const string vowels = "aeiou";
        const string consonants = "bcdfghjklmnpqrstvwxyz";

        System.Random rnd = new System.Random(); 

        length = length % 2 == 0 ? length : length + 1;

        char[] newSeed = new char[length];

        for (var i = 0; i < length; i += 2)
        {
            newSeed[i] = vowels[rnd.Next(vowels.Length)];
            newSeed[i + 1] = consonants[rnd.Next(consonants.Length)];
        }

        string res = new string(newSeed);
        if (Random.value < .5f)
        {
            res = consonants[rnd.Next(consonants.Length)] + res;
        }
        return res;
    }
}
