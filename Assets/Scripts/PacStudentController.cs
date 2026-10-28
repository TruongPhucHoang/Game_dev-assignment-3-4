using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody2D))]
public class PacStudentController : MonoBehaviour
{
    [SerializeField] float tilesPerSecond = 5f;
    [SerializeField] AudioClip pelletMoveClip;
    [SerializeField] AudioClip emptyMoveClip;
    [SerializeField] AudioClip wallHitClip;

    KeyCode lastInput = KeyCode.None;
    KeyCode currentInput = KeyCode.None;

    LevelGenerator level;
    Animator animator;
    AudioSource moveAudio;
    AudioSource wallAudio;
    Rigidbody2D body;
    CircleCollider2D bodyCollider;
    ParticleSystem dust;
    ParticleSystem collisionDust;
    Transform dustTransform;
    Quaternion dustFacingRight = Quaternion.identity;
    CellTween tween = new CellTween();
    string facingState = "Right_move";
    Vector3Int lastStep = Vector3Int.right;
    Vector3Int bumpStep;
    Vector3 bumpOrigin;
    Vector3Int heldWallCell;
    bool holdingWall;
    bool bumping;
    bool bumpHandled;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.enabled = true;
        moveAudio = GetComponent<AudioSource>();
        moveAudio.playOnAwake = false;
        moveAudio.loop = true;
        moveAudio.spatialBlend = 0f;

        wallAudio = gameObject.AddComponent<AudioSource>();
        wallAudio.playOnAwake = false;
        wallAudio.loop = false;
        wallAudio.spatialBlend = 0f;

        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        bodyCollider = GetComponent<CircleCollider2D>();
        if (bodyCollider == null)
            bodyCollider = gameObject.AddComponent<CircleCollider2D>();
        bodyCollider.isTrigger = true;
        float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        bodyCollider.radius = 0.28f / scale;

        dust = transform.Find("Movement_dust")?.GetComponent<ParticleSystem>();
        if (dust == null)
            dust = GetComponentInChildren<ParticleSystem>(true);
        if (dust != null)
        {
            dustTransform = dust.transform;
            dustFacingRight = dustTransform.localRotation;
        }

        collisionDust = transform.Find("Collision_dust")?.GetComponent<ParticleSystem>();
        if (collisionDust != null)
        {
            var main = collisionDust.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            collisionDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        StopMovingPresentation();
    }

    void Start()
    {
        level = FindAnyObjectByType<LevelGenerator>();
        if (level != null)
            level.GenerateLevel();

        if (level != null && level.HasMap)
            MoveBody(level.CellCenter(level.WorldToCell(transform.position)));

        FaceRightIdle();
        StopMovingPresentation();
        if (collisionDust != null)
            collisionDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        ReadInput();

        if (tween.Active)
        {
            MoveBody(tween.Step(Time.deltaTime));
            if (tween.Active)
                return;

            if (bumping)
            {
                bumping = false;
                MoveBody(bumpOrigin);
                StopMovingPresentation();
                return;
            }
        }

        if (TryStep(lastInput) || TryStep(currentInput))
            return;

        if (TryBump(lastInput))
            return;

        StopMovingPresentation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!bumping || bumpHandled || other.GetComponent<WallTrigger>() == null)
            return;

        bumpHandled = true;
        Vector3 contact = other.ClosestPoint(transform.position);
        if ((contact - transform.position).sqrMagnitude < 0.0001f)
            contact = bumpOrigin + (Vector3)bumpStep * (level != null ? level.TileSize * 0.5f : 0.5f);

        tween.Cancel();
        bumping = false;
        MoveBody(bumpOrigin);
        StopMovingPresentation();
        PlayWallHit(contact);
    }

    void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.wKey.wasPressedThisFrame)
            lastInput = KeyCode.W;
        else if (keyboard.aKey.wasPressedThisFrame)
            lastInput = KeyCode.A;
        else if (keyboard.sKey.wasPressedThisFrame)
            lastInput = KeyCode.S;
        else if (keyboard.dKey.wasPressedThisFrame)
            lastInput = KeyCode.D;
    }

    bool TryStep(KeyCode input)
    {
        if (level == null || !level.HasMap || !Direction(input, out Vector3Int step))
            return false;

        Vector3Int fromCell = level.WorldToCell(transform.position);
        Vector3Int nextCell = fromCell + step;
        if (level.TryTunnelWarp(fromCell, step, out Vector3Int arrived))
        {
            fromCell = arrived;
            nextCell = arrived + step;
            if (!level.IsWalkableForPacStudent(nextCell))
                nextCell = arrived;
        }
        else if (!level.IsWalkableForPacStudent(nextCell))
        {
            return false;
        }

        if (input == lastInput)
            currentInput = lastInput;

        Vector3 start = level.CellCenter(fromCell);
        Vector3 end = level.CellCenter(nextCell);
        float tileSpeed = Mathf.Max(0.01f, tilesPerSecond) * Mathf.Max(0.01f, level.TileSize);
        float distance = Vector3.Distance(start, end);
        tween.Begin(start, end, distance / tileSpeed);
        lastStep = step;
        bumping = false;
        holdingWall = false;
        MoveBody(start);
        BeginMovingPresentation(step, level.IsPellet(nextCell));
        return true;
    }

    bool TryBump(KeyCode input)
    {
        if (level == null || !level.HasMap || !Direction(input, out Vector3Int step))
            return false;

        Vector3Int fromCell = level.WorldToCell(transform.position);
        Vector3Int nextCell = fromCell + step;
        if (!level.IsWallCell(nextCell))
            return false;

        if (holdingWall && nextCell == heldWallCell)
            return true;

        heldWallCell = nextCell;
        holdingWall = true;
        bumpOrigin = level.CellCenter(fromCell);
        bumpStep = step;
        bumpHandled = false;
        bumping = true;
        lastStep = step;

        float reach = level.TileSize * 0.42f;
        float tileSpeed = Mathf.Max(0.01f, tilesPerSecond) * Mathf.Max(0.01f, level.TileSize);
        tween.Begin(bumpOrigin, bumpOrigin + (Vector3)step * reach, reach / tileSpeed);
        MoveBody(bumpOrigin);
        BeginMovingPresentation(step, false);
        return true;
    }

    void PlayWallHit(Vector3 point)
    {
        if (wallHitClip != null && wallAudio != null)
            wallAudio.PlayOneShot(wallHitClip);

        if (collisionDust == null)
            return;

        collisionDust.transform.position = point;
        collisionDust.Emit(14);
    }

    void MoveBody(Vector3 position)
    {
        transform.position = position;
        if (body != null)
            body.position = position;
    }

    void BeginMovingPresentation(Vector3Int step, bool eatingPellet)
    {
        facingState = StateFor(step);
        animator.speed = 1f;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(facingState))
            animator.Play(facingState, 0, 0f);

        AudioClip clip = eatingPellet ? pelletMoveClip : emptyMoveClip;
        if (clip != null)
        {
            if (moveAudio.clip != clip)
            {
                moveAudio.clip = clip;
                moveAudio.Play();
            }
            else if (!moveAudio.isPlaying)
            {
                moveAudio.Play();
            }
        }

        AimDust(step);
        if (dust != null && !dust.isPlaying)
            dust.Play();
    }

    void StopMovingPresentation()
    {
        animator.speed = 0f;
        if (moveAudio.isPlaying)
            moveAudio.Stop();
        if (dust != null && dust.isPlaying)
            dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void FaceRightIdle()
    {
        facingState = "Right_move";
        animator.Play(facingState, 0, 0f);
        animator.speed = 0f;
        AimDust(new Vector3Int(1, 0, 0));
    }

    void AimDust(Vector3Int step)
    {
        if (dustTransform == null)
            return;

        float z = 0f;
        if (step.x < 0)
            z = 180f;
        else if (step.y > 0)
            z = 90f;
        else if (step.y < 0)
            z = -90f;

        dustTransform.localRotation = dustFacingRight * Quaternion.Euler(0f, 0f, z);
    }

    static bool Direction(KeyCode input, out Vector3Int step)
    {
        switch (input)
        {
            case KeyCode.W:
                step = new Vector3Int(0, 1, 0);
                return true;
            case KeyCode.A:
                step = new Vector3Int(-1, 0, 0);
                return true;
            case KeyCode.S:
                step = new Vector3Int(0, -1, 0);
                return true;
            case KeyCode.D:
                step = new Vector3Int(1, 0, 0);
                return true;
            default:
                step = Vector3Int.zero;
                return false;
        }
    }

    static string StateFor(Vector3Int step)
    {
        if (step.x > 0) return "Right_move";
        if (step.x < 0) return "Left_move";
        if (step.y > 0) return "Up_move";
        return "Down_move";
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
