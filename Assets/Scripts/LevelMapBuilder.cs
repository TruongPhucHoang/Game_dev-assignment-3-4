using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class LevelMapBuilder : MonoBehaviour
{
    [Header("Sprites (Stage_object legend)")]
    [SerializeField] Sprite Outside_corner;   // 1
    [SerializeField] Sprite Outside_wall;     // 2
    [SerializeField] Sprite Inside_corner;    // 3
    [SerializeField] Sprite Inside_wall;      // 4
    [SerializeField] Sprite Coin;             // 5
    [SerializeField] Sprite Power_up;         // 6
    [SerializeField] Sprite T_junction;       // 7
    [SerializeField] Sprite Enemy_exit;       // 8 

    [Header("Optional animators")]
    [SerializeField] RuntimeAnimatorController Coins;
    [SerializeField] RuntimeAnimatorController Power_up_controller;

    [Header("Layout")]
    [SerializeField] float tileSize = 1f;
    [SerializeField] Camera targetCamera;
    [SerializeField] bool addSideTunnels = true;

    static readonly int[,] LevelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };

    Tilemap _walls;
    Transform _pickups;
    readonly Dictionary<int, Tile> _tiles = new Dictionary<int, Tile>();
    bool _building;

    void OnEnable()
    {
        if (!Application.isPlaying)
            Build();
    }

    void Start()
    {
        Build();
    }

    [ContextMenu("Rebuild Level")]
    public void Build()
    {
        if (_building) return;
        _building = true;
        try
        {
            EnsureGrid();
            ClearGenerated();

            int[,] full = BuildFullMap(out int rows, out int cols);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                    PlaceCell(full, r, c, rows, cols);
            }

            FitCamera(rows, cols);
        }
        finally
        {
            _building = false;
        }
    }

    int[,] BuildFullMap(out int rows, out int cols)
    {
        int qRows = LevelMap.GetLength(0);
        int qCols = LevelMap.GetLength(1);
        // Keep both copies of the last column 
        rows = qRows * 2 - 1;
        int mazeCols = qCols * 2;
        int tunnel = addSideTunnels ? 1 : 0;
        cols = mazeCols + tunnel * 2;

        var full = new int[rows, cols];
        for (int r = 0; r < qRows; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                int id = LevelMap[r, c];
                full[r, tunnel + c] = id;
                full[r, tunnel + mazeCols - 1 - c] = id;
            }
        }

        for (int r = 0; r < qRows - 1; r++)
        {
            for (int c = 0; c < cols; c++)
                full[rows - 1 - r, c] = full[r, c];
        }

        return full;
    }

    void PlaceCell(int[,] map, int r, int c, int rows, int cols)
    {
        int id = map[r, c];
        if (id == 0) return;

        var cell = new Vector3Int(c, -r, 0);

        if (id == 5 || id == 6)
        {
            PlacePickup(id, cell);
            return;
        }

        Sprite sprite = SpriteFor(id);
        if (sprite == null) return;

        Tile tile = GetTile(id, sprite);
        _walls.SetTile(cell, tile);
        _walls.SetTileFlags(cell, TileFlags.None);

        float zRot = RotationFor(map, r, c, rows, cols, id);
        _walls.SetTransformMatrix(cell, Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, zRot)));
    }

    void PlacePickup(int id, Vector3Int cell)
    {
        Sprite sprite = SpriteFor(id);
        if (sprite == null) return;

        var go = new GameObject(id == 6 ? "Power_up" : "Coin");
        go.transform.SetParent(_pickups, false);
        go.transform.position = _walls.GetCellCenterWorld(cell);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 2;

        RuntimeAnimatorController controller = id == 6 ? Power_up_controller : Coins;
        if (controller != null)
        {
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
        }
    }

    void EnsureGrid()
    {
        Grid grid = GetComponent<Grid>();
        if (grid == null)
            grid = gameObject.AddComponent<Grid>();
        grid.cellSize = new Vector3(tileSize, tileSize, 1f);

        _walls = transform.Find("Walls")?.GetComponent<Tilemap>();
        if (_walls == null)
        {
            var wallsGo = new GameObject("Walls");
            wallsGo.transform.SetParent(transform, false);
            wallsGo.transform.localPosition = Vector3.zero;
            wallsGo.transform.localRotation = Quaternion.identity;
            wallsGo.transform.localScale = Vector3.one;
            _walls = wallsGo.AddComponent<Tilemap>();
            var renderer = wallsGo.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 3;
        }

        Transform pickups = transform.Find("Coins");
        if (pickups == null)
        {
            var go = new GameObject("Coins");
            go.transform.SetParent(transform, false);
            pickups = go.transform;
        }
        _pickups = pickups;
    }

    void ClearGenerated()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Tile_"))
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        if (_walls != null)
        {
            _walls.ClearAllTiles();
            _walls.orientation = Tilemap.Orientation.XY;
        }

        if (_pickups != null)
        {
            for (int i = _pickups.childCount - 1; i >= 0; i--)
            {
                GameObject child = _pickups.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        foreach (Tile tile in _tiles.Values)
        {
            if (tile == null) continue;
            if (Application.isPlaying) Destroy(tile);
            else DestroyImmediate(tile);
        }
        _tiles.Clear();
    }

    Tile GetTile(int id, Sprite sprite)
    {
        if (_tiles.TryGetValue(id, out Tile existing) && existing != null)
            return existing;

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = $"Tile_{id}";
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        tile.hideFlags = HideFlags.HideAndDontSave;
        _tiles[id] = tile;
        return tile;
    }

    Sprite SpriteFor(int id)
    {
        switch (id)
        {
            case 1: return Outside_corner;
            case 2: return Outside_wall;
            case 3: return Inside_corner;
            case 4: return Inside_wall;
            case 5: return Coin;
            case 6: return Power_up;
            case 7: return T_junction;
            case 8: return Enemy_exit;
            default: return null;
        }
    }

    static bool IsWall(int id)
    {
        return id == 1 || id == 2 || id == 3 || id == 4 || id == 7 || id == 8;
    }

    static int Cell(int[,] map, int r, int c, int rows, int cols)
    {
        if (r < 0 || c < 0 || r >= rows || c >= cols) return 0;
        return map[r, c];
    }

    static float RotationFor(int[,] map, int r, int c, int rows, int cols, int id)
    {
        bool l = IsWall(Cell(map, r, c - 1, rows, cols));
        bool rg = IsWall(Cell(map, r, c + 1, rows, cols));
        bool u = IsWall(Cell(map, r - 1, c, rows, cols));
        bool d = IsWall(Cell(map, r + 1, c, rows, cols));
        bool wl = !l;
        bool wr = !rg;
        bool wu = !u;
        bool wd = !d;

        // Outside_wall / Inside_wall / Enemy_exit is a horizontal bar through the tile centre.
        if (id == 2 || id == 4 || id == 8)
        {
            int vertical = (u ? 1 : 0) + (d ? 1 : 0);
            int horizontal = (l ? 1 : 0) + (rg ? 1 : 0);
            return vertical > horizontal ? 90f : 0f;
        }

        // T_junction points down (bar left-right, stem down).
        if (id == 7)
        {
            int best = int.MinValue;
            float rot = 0f;
            ScoreT(0f, l, rg, d, wu, ref best, ref rot);
            ScoreT(90f, u, d, l, wr, ref best, ref rot);
            ScoreT(180f, l, rg, u, wd, ref best, ref rot);
            ScoreT(270f, u, d, rg, wl, ref best, ref rot);
            return rot;
        }

        // Inside_corner in a T_junction has walls on all 4 sides
        if (id == 3 && l && rg && u && d)
        {
            bool nw = !IsWall(Cell(map, r - 1, c - 1, rows, cols));
            bool ne = !IsWall(Cell(map, r - 1, c + 1, rows, cols));
            bool sw = !IsWall(Cell(map, r + 1, c - 1, rows, cols));
            bool se = !IsWall(Cell(map, r + 1, c + 1, rows, cols));
            if (nw) return 180f;
            if (sw) return 270f;
            if (se) return 0f;
            if (ne) return 90f;
        }

        // Outside_corner and Inside_corner is an L from the centre
        {
            int best = int.MinValue;
            float rot = 0f;
            ScoreCorner(0f, rg, d, wu, wl, ref best, ref rot);
            ScoreCorner(90f, u, rg, wl, wd, ref best, ref rot);
            ScoreCorner(180f, l, u, wd, wr, ref best, ref rot);
            ScoreCorner(270f, d, l, wr, wu, ref best, ref rot);
            return rot;
        }
    }

    static void ScoreT(float rot, bool barA, bool barB, bool stem, bool openOpposite, ref int best, ref float bestRot)
    {
        int s = 0;
        if (barA) s += 3;
        if (barB) s += 3;
        if (stem) s += 3;
        if (openOpposite) s += 2;
        if (s > best)
        {
            best = s;
            bestRot = rot;
        }
    }

    static void ScoreCorner(float rot, bool armA, bool armB, bool openA, bool openB, ref int best, ref float bestRot)
    {
        int s = 0;
        if (armA) s += 4; else s -= 3;
        if (armB) s += 4; else s -= 3;
        if (openA) s += 2; else s -= 1;
        if (openB) s += 2; else s -= 1;
        if (s > best)
        {
            best = s;
            bestRot = rot;
        }
    }

    void FitCamera(int rows, int cols)
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Vector3 center = new Vector3((cols - 1) * tileSize * 0.5f, -(rows - 1) * tileSize * 0.5f, -10f);
        cam.transform.position = center;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x04, 0x0C, 0x24, 0xFF);

        float width = cols * tileSize;
        float height = rows * tileSize;
        float aspect = cam.aspect > 0.1f ? cam.aspect : 16f / 9f;
        cam.orthographicSize = Mathf.Max(height * 0.5f, width / (2f * aspect)) + tileSize;
    }
}
