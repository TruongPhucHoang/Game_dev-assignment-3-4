using System.Collections.Generic;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    const float NormalSpeedScale = 0.9f;

    static readonly Vector3Int[] Directions =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    EnemyController agent;
    Animator animator;
    LevelGenerator level;
    PacStudentController player;
    CellTween tween = new CellTween();
    readonly List<Vector3Int> house = new List<Vector3Int>();
    readonly List<Vector3Int> gates = new List<Vector3Int>();
    readonly List<Vector3Int> open = new List<Vector3Int>();
    readonly List<Vector3Int> back = new List<Vector3Int>();
    readonly List<Vector3Int> filtered = new List<Vector3Int>();
    Vector3Int lastStep = Vector3Int.right;
    int number = 1;
    bool hasStep;
    bool leftHouse;
    bool ready;

    public int Number => number;

    void Awake()
    {
        agent = GetComponent<EnemyController>();
        animator = GetComponent<Animator>();
        number = NumberFromName(gameObject.name);

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Start()
    {
        ready = TryPrepare();
    }

    public void OnResetHome()
    {
        tween.Cancel();
        hasStep = false;
        leftHouse = false;
        if (level != null && level.HasMap)
            transform.position = level.CellCenter(level.WorldToCell(transform.position));
    }

    void Update()
    {
        if (!ready)
            ready = TryPrepare();
        if (!ready)
            return;

        if (!LevelRound.RoundStarted || LevelRound.Paused || EnemyController.MovementLocked)
            return;

        if (agent != null && agent.IsDead)
            return;

        if (tween.Active)
        {
            transform.position = tween.Step(Time.deltaTime);
            if (tween.Active)
                return;
        }

        Vector3Int cell = level.WorldToCell(transform.position);
        if (!leftHouse && !house.Contains(cell) && !IsGate(cell))
            leftHouse = true;

        Vector3Int step = leftHouse ? ChooseRoam(cell) : ChooseExit(cell);
        if (step == Vector3Int.zero)
            return;

        BeginStep(cell, step);
    }

    bool TryPrepare()
    {
        if (level == null)
            level = FindFirstObjectByType<LevelGenerator>();
        if (player == null)
            player = FindFirstObjectByType<PacStudentController>();
        if (level == null || !level.HasMap)
            return false;

        transform.position = level.CellCenter(level.WorldToCell(transform.position));
        BuildHouse();
        return house.Count > 0 && gates.Count > 0;
    }

    void BuildHouse()
    {
        house.Clear();
        gates.Clear();

        for (int x = 0; x < 40; x++)
        {
            for (int y = 0; y > -40; y--)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (level.TileId(cell) == 8)
                    gates.Add(cell);
            }
        }

        Vector3Int start = level.WorldToCell(transform.position);
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(start);
        house.Add(start);
        while (queue.Count > 0)
        {
            Vector3Int cell = queue.Dequeue();
            for (int i = 0; i < Directions.Length; i++)
            {
                Vector3Int next = cell + Directions[i];
                if (house.Contains(next) || !level.IsWalkableForPacStudent(next))
                    continue;

                house.Add(next);
                queue.Enqueue(next);
            }
        }
    }

    void BeginStep(Vector3Int from, Vector3Int step)
    {
        Vector3Int to = from + step;
        Vector3 start = level.CellCenter(from);
        Vector3 end = level.CellCenter(to);
        float tileSpeed = NormalTilesPerSecond() * Mathf.Max(0.01f, level.TileSize);
        float distance = Vector3.Distance(start, end);
        tween.Begin(start, end, distance / tileSpeed);
        lastStep = step;
        hasStep = true;
        transform.position = start;
        PlayFacing(step);
    }

    float NormalTilesPerSecond()
    {
        float pac = player != null ? player.TilesPerSecond : 5f;
        return Mathf.Max(0.01f, pac) * NormalSpeedScale;
    }

    Vector3Int ChooseExit(Vector3Int cell)
    {
        if (IsAssignedGate(cell))
        {
            open.Clear();
            for (int i = 0; i < Directions.Length; i++)
            {
                Vector3Int step = Directions[i];
                Vector3Int next = cell + step;
                if (level.IsWalkableForPacStudent(next) && !house.Contains(next))
                    open.Add(step);
            }

            if (open.Count > 0)
                return open[Random.Range(0, open.Count)];
        }

        Vector3Int goal = NearestAssignedGate(cell);
        open.Clear();
        back.Clear();
        int best = int.MaxValue;
        for (int i = 0; i < Directions.Length; i++)
        {
            Vector3Int step = Directions[i];
            Vector3Int next = cell + step;
            bool inside = house.Contains(next) && level.IsWalkableForPacStudent(next);
            if (!inside && !IsAssignedGate(next))
                continue;

            int score = Mathf.Abs(next.x - goal.x) + Mathf.Abs(next.y - goal.y);
            if (score > best)
                continue;

            if (score < best)
            {
                best = score;
                open.Clear();
                back.Clear();
            }

            if (hasStep && step == -lastStep)
                back.Add(step);
            else
                open.Add(step);
        }

        if (open.Count > 0)
            return open[Random.Range(0, open.Count)];
        if (back.Count > 0)
            return back[0];
        return Vector3Int.zero;
    }

    Vector3Int ChooseRoam(Vector3Int cell)
    {
        open.Clear();
        back.Clear();
        for (int i = 0; i < Directions.Length; i++)
        {
            Vector3Int step = Directions[i];
            if (!CanWalk(cell, step))
                continue;

            if (hasStep && step == -lastStep)
                back.Add(step);
            else
                open.Add(step);
        }

        List<Vector3Int> pool = open.Count > 0 ? open : back;
        if (pool.Count == 0)
            return Vector3Int.zero;

        if (number == 4)
            return ChooseClockwise(cell, hasStep ? lastStep : Vector3Int.right, pool);

        if (number == 1 || number == 2)
        {
            float here = DistanceToPlayer(cell);
            filtered.Clear();
            for (int i = 0; i < pool.Count; i++)
            {
                float there = DistanceToPlayer(cell + pool[i]);
                bool flee = number == 1 && there + 0.001f >= here;
                bool chase = number == 2 && there <= here + 0.001f;
                if (flee || chase)
                    filtered.Add(pool[i]);
            }

            if (filtered.Count > 0)
                pool = filtered;
        }

        return pool[Random.Range(0, pool.Count)];
    }

    Vector3Int ChooseClockwise(Vector3Int cell, Vector3Int facing, List<Vector3Int> pool)
    {
        Vector3Int left = new Vector3Int(-facing.y, facing.x, 0);
        Vector3Int right = new Vector3Int(facing.y, -facing.x, 0);
        Vector3Int reverse = new Vector3Int(-facing.x, -facing.y, 0);
        Vector3Int[] order = { left, facing, right, reverse };

        if (TryAlongWall(cell, order, pool, true, out Vector3Int alongOutside))
            return alongOutside;
        if (TryAlongWall(cell, order, pool, false, out Vector3Int alongAny))
            return alongAny;

        for (int i = 0; i < order.Length; i++)
        {
            if (pool.Contains(order[i]))
                return order[i];
        }

        return pool[0];
    }

    bool TryAlongWall(Vector3Int cell, Vector3Int[] order, List<Vector3Int> pool, bool outsideOnly, out Vector3Int step)
    {
        for (int i = 0; i < order.Length; i++)
        {
            if (!pool.Contains(order[i]))
                continue;

            Vector3Int left = new Vector3Int(-order[i].y, order[i].x, 0);
            Vector3Int beside = cell + order[i] + left;
            if (outsideOnly)
            {
                if (!IsOutsideWall(beside))
                    continue;
            }
            else if (level.IsWalkableForPacStudent(beside))
            {
                continue;
            }

            step = order[i];
            return true;
        }

        step = Vector3Int.zero;
        return false;
    }

    bool CanWalk(Vector3Int from, Vector3Int step)
    {
        if (level.TryTunnelWarp(from, step, out _))
            return false;

        return level.IsWalkableForPacStudent(from + step);
    }

    bool IsOutsideWall(Vector3Int cell)
    {
        int id = level.TileId(cell);
        return id == 1 || id == 2 || id == 7;
    }

    bool IsGate(Vector3Int cell)
    {
        return level.TileId(cell) == 8;
    }

    bool IsAssignedGate(Vector3Int cell)
    {
        if (!IsGate(cell))
            return false;

        bool top = number == 1 || number == 3;
        int gateY = TopGateY();
        return top ? cell.y == gateY : cell.y != gateY;
    }

    Vector3Int NearestAssignedGate(Vector3Int cell)
    {
        Vector3Int best = gates[0];
        int bestScore = int.MaxValue;
        for (int i = 0; i < gates.Count; i++)
        {
            if (!IsAssignedGate(gates[i]))
                continue;

            int score = Mathf.Abs(gates[i].x - cell.x) + Mathf.Abs(gates[i].y - cell.y);
            if (score < bestScore)
            {
                bestScore = score;
                best = gates[i];
            }
        }

        return best;
    }

    int TopGateY()
    {
        int top = int.MinValue;
        for (int i = 0; i < gates.Count; i++)
        {
            if (gates[i].y > top)
                top = gates[i].y;
        }

        return top;
    }

    float DistanceToPlayer(Vector3Int cell)
    {
        if (player == null)
            return 0f;

        return Vector3.Distance(level.CellCenter(cell), player.transform.position);
    }

    void PlayFacing(Vector3Int step)
    {
        if (animator == null || agent == null || agent.State != EnemyController.Mood.Normal)
            return;

        string direction = "right";
        if (step.x < 0)
            direction = "left";
        else if (step.y > 0)
            direction = "up";
        else if (step.y < 0)
            direction = "down";

        animator.Play("Enemy1N_" + direction, 0, 0f);
    }

    static int NumberFromName(string ghostName)
    {
        for (int i = ghostName.Length - 1; i >= 0; i--)
        {
            if (ghostName[i] >= '1' && ghostName[i] <= '4')
                return ghostName[i] - '0';
        }

        return 1;
    }

    class CellTween
    {
        public bool Active;
        Vector3 from;
        Vector3 to;
        float duration;
        float elapsed;

        public void Begin(Vector3 start, Vector3 end, float seconds)
        {
            from = start;
            to = end;
            duration = Mathf.Max(0.0001f, seconds);
            elapsed = 0f;
            Active = true;
        }

        public void Cancel()
        {
            Active = false;
        }

        public Vector3 Step(float deltaTime)
        {
            elapsed += deltaTime;
            float t = elapsed / duration;
            if (t >= 1f)
            {
                Active = false;
                return to;
            }

            return from + (to - from) * t;
        }
    }
}
