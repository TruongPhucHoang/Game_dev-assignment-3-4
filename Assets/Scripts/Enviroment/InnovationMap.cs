using System.Collections.Generic;
using UnityEngine;

public class InnovationMap : LevelGenerator
{
    const float PowerPelletChance = 0.02f;

    public void Rebuild()
    {
        AllowRebuild();
        GenerateLevel();
    }

    protected override int[,] BuildFullMap(out int rows, out int cols)
    {
        int[,] map = base.BuildFullMap(out rows, out cols);
        Randomize(map, rows, cols);
        return map;
    }

    void Randomize(int[,] map, int rows, int cols)
    {
        var original = (int[,])map.Clone();
        bool[,] house = HouseInterior(original, rows, cols);
        bool[,] locked = LockedCells(original, house, rows, cols);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!locked[r, c])
                    map[r, c] = 4;
            }
        }

        CarveMazes(map, locked, rows, cols);
        OpenTunnelDoors(map, original, locked, house, rows, cols);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (locked[r, c])
                    map[r, c] = original[r, c];
            }
        }

        if (rows > 1 && cols > 2 && !locked[1, 2])
            map[1, 2] = 0;

        KeepFormerPowerSpots(map, original, house, rows, cols);
        OpenPassages(map, locked, rows, cols);
        JoinFloors(map, locked, rows, cols);
        OpenPassages(map, locked, rows, cols);
        StyleWalls(map, locked, rows, cols);
        PlacePellets(map, locked, rows, cols);
        CloseOuterWall(map, rows, cols);
        FillOpenTiles(map, house, rows, cols);
        RollPowerPellets(map, rows, cols);
    }

    static void KeepFormerPowerSpots(int[,] map, int[,] original, bool[,] house, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (original[r, c] != 6 || house[r, c] || IsTunnelApproach(r, c, rows, cols))
                    continue;
                if (r == 0 || r == rows - 1 || c == 0 || c == cols - 1)
                    continue;
                if ((c == 1 || c == cols - 2) && r != rows / 2)
                    continue;

                map[r, c] = 0;
            }
        }
    }

    static void RollPowerPellets(int[,] map, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (map[r, c] == 6)
                    map[r, c] = 5;
            }
        }

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (map[r, c] == 5 && Random.value < PowerPelletChance)
                    map[r, c] = 6;
            }
        }
    }

    static void FillOpenTiles(int[,] map, bool[,] house, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (house[r, c])
                {
                    map[r, c] = 0;
                    continue;
                }

                if (IsTunnelApproach(r, c, rows, cols))
                {
                    map[r, c] = 0;
                    continue;
                }

                if (c == 0 || c == cols - 1 || IsWallId(map[r, c]))
                    continue;

                if (map[r, c] != 6)
                    map[r, c] = 5;
            }
        }
    }

    static bool IsTunnelApproach(int row, int col, int rows, int cols)
    {
        if (row != rows / 2)
            return false;

        if (col >= 1 && col <= 5)
            return true;

        return col <= cols - 2 && col >= cols - 6;
    }

    static void CloseOuterWall(int[,] map, int rows, int cols)
    {
        int tunnel = rows / 2;
        for (int c = 1; c < cols - 1; c++)
        {
            bool corner = c == 1 || c == cols - 2;
            map[0, c] = corner ? 1 : 2;
            map[rows - 1, c] = corner ? 1 : 2;
        }

        for (int r = 1; r < rows - 1; r++)
        {
            if (r == tunnel)
                continue;

            map[r, 1] = 2;
            map[r, cols - 2] = 2;
        }
    }

    void OpenPassages(int[,] map, bool[,] locked, int rows, int cols)
    {
        for (int pass = 0; pass < rows * cols; pass++)
        {
            bool opened = false;
            for (int r = 1; r < rows - 1; r++)
            {
                for (int c = 1; c < cols - 1; c++)
                {
                    if (!IsFloor(map[r, c]) || FloorNeighbors(map, r, c, rows, cols) >= 2)
                        continue;
                    if (OpenSecondExit(map, locked, rows, cols, r, c))
                        opened = true;
                }
            }

            if (!opened)
                break;
        }
    }

    bool OpenSecondExit(int[,] map, bool[,] locked, int rows, int cols, int row, int col)
    {
        int through = -1;
        int throughCount = 0;
        Vector2Int cell = new Vector2Int(col, row);
        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int next = cell + Step(dir);
            if (next.y <= 0 || next.x <= 0 || next.y >= rows - 1 || next.x >= cols - 1)
                continue;
            if (locked[next.y, next.x] || IsFloor(map[next.y, next.x]) || !IsWallId(map[next.y, next.x]))
                continue;

            Vector2Int beyond = next + Step(dir);
            if (beyond.y < 0 || beyond.x < 0 || beyond.y >= rows || beyond.x >= cols)
                continue;
            if (!IsFloor(map[beyond.y, beyond.x]))
                continue;

            throughCount++;
            if (Random.Range(0, throughCount) == 0)
                through = dir;
        }

        if (through < 0)
            return false;

        int choice = through;

        Vector2Int opening = cell + Step(choice);
        map[opening.y, opening.x] = 0;
        return true;
    }

    static int FloorNeighbors(int[,] map, int row, int col, int rows, int cols)
    {
        int count = 0;
        Vector2Int cell = new Vector2Int(col, row);
        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int next = cell + Step(dir);
            if (next.y < 0 || next.x < 0 || next.y >= rows || next.x >= cols)
                continue;
            if (IsFloor(map[next.y, next.x]))
                count++;
        }

        return count;
    }

    static void StyleWalls(int[,] map, bool[,] locked, int rows, int cols)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int r = 1; r < rows - 1; r++)
            {
                for (int c = 1; c < cols - 1; c++)
                {
                    if (locked[r, c] || IsFloor(map[r, c]))
                        continue;

                    int walls = WallNeighbors(map, r, c, rows, cols);
                    if (walls == 0)
                    {
                        map[r, c] = 0;
                        changed = true;
                    }
                }
            }
        }

        for (int r = 1; r < rows - 1; r++)
        {
            for (int c = 1; c < cols - 1; c++)
            {
                if (locked[r, c] || IsFloor(map[r, c]))
                    continue;

                bool left = IsWallId(NeighborId(map, r, c - 1, rows, cols));
                bool right = IsWallId(NeighborId(map, r, c + 1, rows, cols));
                bool up = IsWallId(NeighborId(map, r - 1, c, rows, cols));
                bool down = IsWallId(NeighborId(map, r + 1, c, rows, cols));
                bool straight = (left && right) || (up && down);
                bool corner = !straight && ((left || right) && (up || down));
                if (corner)
                    map[r, c] = 3;
                else
                    map[r, c] = 4;
            }
        }
    }

    static int WallNeighbors(int[,] map, int row, int col, int rows, int cols)
    {
        int count = 0;
        if (IsWallId(NeighborId(map, row, col - 1, rows, cols))) count++;
        if (IsWallId(NeighborId(map, row, col + 1, rows, cols))) count++;
        if (IsWallId(NeighborId(map, row - 1, col, rows, cols))) count++;
        if (IsWallId(NeighborId(map, row + 1, col, rows, cols))) count++;
        return count;
    }

    static int NeighborId(int[,] map, int row, int col, int rows, int cols)
    {
        if (row < 0 || col < 0 || row >= rows || col >= cols)
            return 1;
        return map[row, col];
    }

    static bool[,] HouseInterior(int[,] map, int rows, int cols)
    {
        var house = new bool[rows, cols];
        int startRow = rows / 2;
        int startCol = cols / 2;
        if (!IsFloor(map[startRow, startCol]))
            return house;

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startCol, startRow));
        house[startRow, startCol] = true;
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                Vector2Int next = cell + Step(i);
                if (next.y < 0 || next.x < 0 || next.y >= rows || next.x >= cols)
                    continue;
                if (house[next.y, next.x] || !IsFloor(map[next.y, next.x]))
                    continue;

                house[next.y, next.x] = true;
                queue.Enqueue(next);
            }
        }

        return house;
    }

    static bool[,] LockedCells(int[,] map, bool[,] house, int rows, int cols)
    {
        var locked = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int id = map[r, c];
                if (r == 0 || c == 0 || r == rows - 1 || c == cols - 1 || r == rows / 2)
                    locked[r, c] = true;
                if (c == 1 || c == cols - 2)
                    locked[r, c] = true;
                if (house[r, c] || id == 8)
                    locked[r, c] = true;
                if (IsWallId(id) && BesideHouse(map, house, r, c, rows, cols))
                    locked[r, c] = true;
            }
        }

        return locked;
    }

    static bool BesideHouse(int[,] map, bool[,] house, int row, int col, int rows, int cols)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2Int next = new Vector2Int(col, row) + Step(i);
            if (next.y < 0 || next.x < 0 || next.y >= rows || next.x >= cols)
                continue;
            if (house[next.y, next.x] || map[next.y, next.x] == 8)
                return true;
        }

        return false;
    }

    void CarveMazes(int[,] map, bool[,] locked, int rows, int cols)
    {
        var visited = new bool[rows, cols];
        for (int r = 1; r < rows; r += 2)
        {
            for (int c = 1; c < cols; c += 2)
            {
                if (!locked[r, c] && !visited[r, c])
                    CarveFrom(map, locked, visited, rows, cols, r, c);
            }
        }
    }

    void CarveFrom(int[,] map, bool[,] locked, bool[,] visited, int rows, int cols, int row, int col)
    {
        var stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(col, row));
        while (stack.Count > 0)
        {
            Vector2Int cell = stack.Peek();
            visited[cell.y, cell.x] = true;
            map[cell.y, cell.x] = 0;

            int choice = -1;
            int optionCount = 0;
            for (int dir = 0; dir < 4; dir++)
            {
                Vector2Int next = cell + Step(dir) * 2;
                if (next.y < 0 || next.x < 0 || next.y >= rows || next.x >= cols)
                    continue;
                if (locked[next.y, next.x] || visited[next.y, next.x])
                    continue;

                Vector2Int between = cell + Step(dir);
                if (locked[between.y, between.x])
                    continue;

                optionCount++;
                if (Random.Range(0, optionCount) == 0)
                    choice = dir;
            }

            if (choice < 0)
            {
                stack.Pop();
                continue;
            }

            Vector2Int middle = cell + Step(choice);
            Vector2Int destination = cell + Step(choice) * 2;
            map[middle.y, middle.x] = 0;
            visited[destination.y, destination.x] = true;
            stack.Push(destination);
        }
    }

    static void OpenTunnelDoors(int[,] map, int[,] original, bool[,] locked, bool[,] house, int rows, int cols)
    {
        int tunnel = rows / 2;
        OpenDoor(map, original, locked, house, rows, cols, tunnel, -1, true);
        OpenDoor(map, original, locked, house, rows, cols, tunnel, -1, false);
        OpenDoor(map, original, locked, house, rows, cols, tunnel, 1, true);
        OpenDoor(map, original, locked, house, rows, cols, tunnel, 1, false);
    }

    static void OpenDoor(int[,] map, int[,] original, bool[,] locked, bool[,] house, int rows, int cols, int tunnel, int side, bool leftHalf)
    {
        int neighbor = tunnel + side;
        if (neighbor < 0 || neighbor >= rows)
            return;

        for (int c = 1; c < cols - 1; c++)
        {
            bool onLeft = c < cols / 2;
            if (onLeft != leftHalf)
                continue;
            if (house[tunnel, c] || !IsFloor(original[tunnel, c]))
                continue;
            if (locked[neighbor, c])
                continue;

            map[neighbor, c] = 0;
            return;
        }
    }

    void JoinFloors(int[,] map, bool[,] locked, int rows, int cols)
    {
        int rootRow = 1;
        int rootCol = 2;
        if (rootRow >= rows || rootCol >= cols || !IsFloor(map[rootRow, rootCol]))
        {
            rootRow = rows / 2;
            rootCol = 1;
        }

        if (!IsFloor(map[rootRow, rootCol]))
            return;

        var reached = Reachable(map, rows, cols, rootRow, rootCol);
        bool grew = true;
        while (grew)
        {
            grew = false;
            for (int r = 0; r < rows && !grew; r++)
            {
                for (int c = 0; c < cols && !grew; c++)
                {
                    if (!IsFloor(map[r, c]) || reached[r, c])
                        continue;
                    if (CarvePath(map, locked, reached, rows, cols, r, c))
                    {
                        reached = Reachable(map, rows, cols, rootRow, rootCol);
                        grew = true;
                    }
                }
            }
        }
    }

    bool CarvePath(int[,] map, bool[,] locked, bool[,] reached, int rows, int cols, int startRow, int startCol)
    {
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var seen = new bool[rows, cols];
        var start = new Vector2Int(startCol, startRow);
        queue.Enqueue(start);
        seen[startRow, startCol] = true;

        Vector2Int found = new Vector2Int(-1, -1);
        while (queue.Count > 0 && found.x < 0)
        {
            Vector2Int cell = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                Vector2Int next = cell + Step(i);
                if (next.y < 0 || next.x < 0 || next.y >= rows || next.x >= cols)
                    continue;
                if (seen[next.y, next.x] || !CanCross(map, locked, next.y, next.x))
                    continue;

                seen[next.y, next.x] = true;
                parent[next] = cell;
                if (reached[next.y, next.x])
                {
                    found = next;
                    break;
                }

                queue.Enqueue(next);
            }
        }

        if (found.x < 0)
            return false;

        Vector2Int step = found;
        while (parent.ContainsKey(step))
        {
            if (!locked[step.y, step.x])
                map[step.y, step.x] = 0;
            step = parent[step];
        }

        if (!locked[startRow, startCol])
            map[startRow, startCol] = 0;
        return true;
    }

    static bool[,] Reachable(int[,] map, int rows, int cols, int startRow, int startCol)
    {
        var reached = new bool[rows, cols];
        if (!IsFloor(map[startRow, startCol]))
            return reached;

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startCol, startRow));
        reached[startRow, startCol] = true;
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                Vector2Int next = cell + Step(i);
                if (next.y < 0 || next.x < 0 || next.y >= rows || next.x >= cols)
                    continue;
                if (reached[next.y, next.x] || !IsFloor(map[next.y, next.x]))
                    continue;

                reached[next.y, next.x] = true;
                queue.Enqueue(next);
            }
        }

        return reached;
    }

    static void PlacePellets(int[,] map, bool[,] locked, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (locked[r, c] || !IsFloor(map[r, c]))
                    continue;

                map[r, c] = 5;
            }
        }
    }

    static bool CanCross(int[,] map, bool[,] locked, int row, int col)
    {
        if (!locked[row, col])
            return true;
        return IsFloor(map[row, col]);
    }

    static bool IsFloor(int id)
    {
        return id == 0 || id == 5 || id == 6;
    }

    static bool IsWallId(int id)
    {
        return id == 1 || id == 2 || id == 3 || id == 4 || id == 7 || id == 8;
    }

    static Vector2Int Step(int direction)
    {
        switch (direction)
        {
            case 0: return Vector2Int.right;
            case 1: return Vector2Int.left;
            case 2: return Vector2Int.up;
            default: return Vector2Int.down;
        }
    }
}
