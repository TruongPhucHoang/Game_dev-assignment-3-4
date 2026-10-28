using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    [Header("Map CSV")]
    [SerializeField] TextAsset mapCsv;

    [Header("Sprites (Stage_object legend)")]
    [SerializeField] Sprite Outside_corner;   // 1
    [SerializeField] Sprite Outside_wall;     // 2
    [SerializeField] Sprite Inside_corner;    // 3
    [SerializeField] Sprite Inside_wall;      // 4
    [SerializeField] Sprite Coin;             // 5
    [SerializeField] Sprite Power_up;         // 6
    [SerializeField] Sprite T_junction;       // 7
    [SerializeField] Sprite Enemy_exit;       // 8

    [Header("Animators")]
    [SerializeField] RuntimeAnimatorController Coins_animator;
    [SerializeField] RuntimeAnimatorController Power_up_animator;

    [Header("Layout")]
    [SerializeField] float tileSize = 1f;
    [SerializeField] Camera targetCamera;
    [SerializeField] bool addSideTunnels = true;
    [SerializeField] Transform extraView;

    [Header("Tiles")]
    [SerializeField] Tile Tile_Outside_corner;
    [SerializeField] Tile Tile_Outside_wall;
    [SerializeField] Tile Tile_Inside_corner;
    [SerializeField] Tile Tile_Inside_wall;
    [SerializeField] Tile Tile_T_junction;
    [SerializeField] Tile Tile_Enemy_exit;

    int[,] levelMap;
    int[,] fullMap;
    int mapRows;
    int mapCols;
    Grid grid;
    Tilemap _walls;
    Transform _pickups;
    readonly Dictionary<int, Tile> _tiles = new Dictionary<int, Tile>();
    bool _generated;
    int pelletsLeft;

    public bool HasMap => fullMap != null;
    public float TileSize => tileSize;

    void Awake()
    {
        GenerateLevel();
    }

    protected void AllowRebuild()
    {
        _generated = false;
    }

    [ContextMenu("Rebuild Level")]
    public void GenerateLevel()
    {
        if (_generated) return;
        _generated = true;

        levelMap = ReadFirstArray(mapCsv);
        if (levelMap == null)
            return;

        EnsureGrid();
        ClearGenerated();

        int[,] full = BuildFullMap(out int rows, out int cols);
        fullMap = full;
        mapRows = rows;
        mapCols = cols;
        pelletsLeft = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
                PlaceCell(full, r, c, rows, cols);
        }

        ApplyWallRotations(full, rows, cols);
        BuildWallTriggers(full, rows, cols);
        FitCamera(rows, cols);
    }

    static int[,] ReadFirstArray(TextAsset csv)
    {
        if (csv == null || string.IsNullOrEmpty(csv.text))
            return null;

        var rows = new List<int[]>();
        string[] lines = csv.text.Split(new[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line.Replace(",", "")))
            {
                if (rows.Count > 0)
                    break;
                continue;
            }

            string[] cells = line.Split(',');
            if (cells.Length == 0 || !int.TryParse(cells[0].Trim(), out _))
            {
                if (rows.Count > 0)
                    break;
                continue;
            }

            var row = new List<int>();
            bool numeric = true;
            for (int c = 0; c < cells.Length; c++)
            {
                string cell = cells[c].Trim();
                if (cell.Length == 0)
                    continue;
                if (!int.TryParse(cell, out int id))
                {
                    numeric = false;
                    break;
                }
                row.Add(id);
            }

            if (!numeric)
            {
                if (rows.Count > 0)
                    break;
                continue;
            }

            if (row.Count > 0)
                rows.Add(row.ToArray());
        }

        if (rows.Count == 0)
            return null;

        int cols = rows[0].Length;
        var map = new int[rows.Count, cols];
        for (int r = 0; r < rows.Count; r++)
        {
            int n = Mathf.Min(cols, rows[r].Length);
            for (int c = 0; c < n; c++)
                map[r, c] = rows[r][c];
        }
        return map;
    }

    protected virtual int[,] BuildFullMap(out int rows, out int cols)
    {
        int qRows = levelMap.GetLength(0);
        int qCols = levelMap.GetLength(1);
        rows = qRows * 2 - 1;
        int mazeCols = qCols * 2;
        int tunnel = addSideTunnels ? 1 : 0;
        cols = mazeCols + tunnel * 2;

        var full = new int[rows, cols];
        for (int r = 0; r < qRows; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                int id = levelMap[r, c];
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
    }

    void ApplyWallRotations(int[,] map, int rows, int cols)
    {
        if (_walls == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int id = map[r, c];
                if (id == 0 || id == 5 || id == 6) continue;

                var cell = new Vector3Int(c, -r, 0);
                if (_walls.GetTile(cell) == null) continue;

                float zRot = RotationFor(map, r, c, rows, cols, id);
                _walls.SetTileFlags(cell, TileFlags.None);
                _walls.SetTransformMatrix(cell, Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, zRot)));
                _walls.SetTileFlags(cell, TileFlags.LockTransform);
            }
        }
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

        RuntimeAnimatorController controller = id == 6 ? Power_up_animator : Coins_animator;
        if (controller != null)
        {
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
        }

        if (id == 5 || id == 6)
        {
            var pickup = go.AddComponent<CircleCollider2D>();
            pickup.isTrigger = true;
            pickup.radius = tileSize * (id == 6 ? 0.4f : 0.3f);
            if (id == 6)
                go.AddComponent<PowerPill>();
            else
            {
                go.AddComponent<PelletPickup>();
                pelletsLeft++;
            }
        }
    }

    void EnsureGrid()
    {
        Grid grid = GetComponent<Grid>();
        if (grid == null)
            grid = gameObject.AddComponent<Grid>();
        grid.cellSize = new Vector3(tileSize, tileSize, 1f);
        this.grid = grid;

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

        Transform triggers = transform.Find("WallTriggers");
        if (triggers != null)
        {
            if (Application.isPlaying) Destroy(triggers.gameObject);
            else DestroyImmediate(triggers.gameObject);
        }
    }

    void BuildWallTriggers(int[,] map, int rows, int cols)
    {
        var root = new GameObject("WallTriggers");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!IsWall(map[r, c]))
                    continue;

                var cell = new Vector3Int(c, -r, 0);
                var go = new GameObject("WallTrigger");
                go.transform.SetParent(root.transform, false);
                go.transform.position = CellCenter(cell);

                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(tileSize * 0.92f, tileSize * 0.92f);
                go.AddComponent<WallTrigger>();
            }
        }
    }

    Tile GetTile(int id, Sprite sprite)
    {
        Tile asset = AssetTile(id);
        if (asset != null)
        {
            asset.sprite = sprite;
            asset.colliderType = Tile.ColliderType.None;
            asset.flags = TileFlags.LockTransform;
            return asset;
        }

        if (!_tiles.TryGetValue(id, out Tile tile) || tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = $"Tile_{id}";
            tile.colliderType = Tile.ColliderType.None;
            tile.hideFlags = HideFlags.HideAndDontSave;
            tile.flags = TileFlags.LockTransform;
            _tiles[id] = tile;
        }

        tile.sprite = sprite;
        return tile;
    }

    Tile AssetTile(int id)
    {
        switch (id)
        {
            case 1: return Tile_Outside_corner;
            case 2: return Tile_Outside_wall;
            case 3: return Tile_Inside_corner;
            case 4: return Tile_Inside_wall;
            case 7: return Tile_T_junction;
            case 8: return Tile_Enemy_exit;
            default: return null;
        }
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

    public Vector3Int WorldToCell(Vector3 world)
    {
        if (grid == null)
            return Vector3Int.zero;
        return grid.WorldToCell(world);
    }

    public Vector3 CellCenter(Vector3Int cell)
    {
        if (grid == null)
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        return grid.GetCellCenterWorld(cell);
    }

    public int TileId(Vector3Int cell)
    {
        if (fullMap == null)
            return -1;
        int row = -cell.y;
        int col = cell.x;
        if (row < 0 || col < 0 || row >= mapRows || col >= mapCols)
            return -1;
        return fullMap[row, col];
    }

    public bool IsWalkableForPacStudent(Vector3Int cell)
    {
        if (mapCols > 1 && (cell.x == 0 || cell.x == mapCols - 1))
            return false;

        int id = TileId(cell);
        return id == 0 || id == 5 || id == 6;
    }

    public bool TryTunnelWarp(Vector3Int from, Vector3Int step, out Vector3Int destination)
    {
        destination = from;
        if (mapCols <= 2 || step.x == 0)
            return false;

        bool exitLeft = step.x < 0 && from.x == 1;
        bool exitRight = step.x > 0 && from.x == mapCols - 2;
        if (!exitLeft && !exitRight)
            return false;

        int id = TileId(from);
        if (id != 0 && id != 5 && id != 6)
            return false;

        int arriveX = exitLeft ? mapCols - 2 : 1;
        destination = new Vector3Int(arriveX, from.y, 0);
        int arriveId = TileId(destination);
        return arriveId == 0 || arriveId == 5 || arriveId == 6;
    }

    public bool IsPellet(Vector3Int cell)
    {
        int id = TileId(cell);
        return id == 5 || id == 6;
    }

    public bool ConsumePellet()
    {
        pelletsLeft = Mathf.Max(0, pelletsLeft - 1);
        return pelletsLeft == 0;
    }

    public void ClearPellet(Vector3Int cell)
    {
        if (fullMap == null)
            return;

        int row = -cell.y;
        int col = cell.x;
        if (row < 0 || col < 0 || row >= mapRows || col >= mapCols)
            return;
        if (fullMap[row, col] == 5 || fullMap[row, col] == 6)
            fullMap[row, col] = 0;
    }

    public bool IsWallCell(Vector3Int cell)
    {
        return IsWall(TileId(cell));
    }

    public bool TryGetLevelRect(out Vector2 center, out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;
        if (fullMap == null || mapRows <= 0 || mapCols <= 0)
            return false;

        float minX = -0.5f * tileSize;
        float maxX = (mapCols - 0.5f) * tileSize;
        float maxY = 0.5f * tileSize;
        float minY = -(mapRows - 0.5f) * tileSize;
        center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        size = new Vector2(maxX - minX, maxY - minY);
        return true;
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

        if (id == 2 || id == 4 || id == 8)
        {
            int vertical = (u ? 1 : 0) + (d ? 1 : 0);
            int horizontal = (l ? 1 : 0) + (rg ? 1 : 0);
            return vertical > horizontal ? 90f : 0f;
        }

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
        if (rows <= 0 || cols <= 0) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        float minX = -0.5f * tileSize;
        float maxX = (cols - 0.5f) * tileSize;
        float maxY = 0.5f * tileSize;
        float minY = -(rows - 0.5f) * tileSize;

        Transform extra = extraView;
        if (extra == null)
        {
            GameObject demo = GameObject.Find("Demo_animation");
            if (demo != null)
                extra = demo.transform;
        }
        EncapsulateGroup(extra, ref minX, ref maxX, ref minY, ref maxY);

        float pad = tileSize * 1.5f;
        minX -= pad;
        maxX += pad;
        minY -= pad;
        maxY += pad;

        float width = maxX - minX;
        float height = maxY - minY;
        cam.transform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, -10f);
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x04, 0x0C, 0x24, 0xFF);

        const float aspect = 16f / 9f;
        cam.orthographicSize = Mathf.Max(height * 0.5f, width / (2f * aspect));
    }

    static void EncapsulateGroup(Transform root, ref float minX, ref float maxX, ref float minY, ref float maxY)
    {
        if (root == null) return;

        const float maxFromRoot = 8f;
        Vector3 origin = root.position;

        Transform[] nodes = root.GetComponentsInChildren<Transform>();
        for (int i = 0; i < nodes.Length; i++)
        {
            Vector3 p = nodes[i].position;
            p.x = Mathf.Clamp(p.x, origin.x - maxFromRoot, origin.x + maxFromRoot);
            p.y = Mathf.Clamp(p.y, origin.y - maxFromRoot, origin.y + maxFromRoot);
            Vector3 s = nodes[i].lossyScale;
            float hx = Mathf.Min(Mathf.Abs(s.x), 6f) * 0.5f;
            float hy = Mathf.Min(Mathf.Abs(s.y), 6f) * 0.5f;
            minX = Mathf.Min(minX, p.x - hx);
            maxX = Mathf.Max(maxX, p.x + hx);
            minY = Mathf.Min(minY, p.y - hy);
            maxY = Mathf.Max(maxY, p.y + hy);
        }
    }
}

public class WallTrigger : MonoBehaviour
{
}

public class PelletPickup : MonoBehaviour
{
}

public class PowerPill : MonoBehaviour
{
}
