using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class PacStudentController : MonoBehaviour
{
    [SerializeField] float tilesPerSecond = 5f;
    [SerializeField] AudioClip pelletMoveClip;
    [SerializeField] AudioClip emptyMoveClip;

    KeyCode lastInput = KeyCode.None;
    KeyCode currentInput = KeyCode.None;

    LevelGenerator level;
    Animator animator;
    AudioSource moveAudio;
    ParticleSystem dust;
    Transform dustTransform;
    Quaternion dustFacingRight = Quaternion.identity;
    CellTween tween = new CellTween();
    string facingState = "Right_move";

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.enabled = true;
        moveAudio = GetComponent<AudioSource>();
        moveAudio.playOnAwake = false;
        moveAudio.loop = true;
        moveAudio.spatialBlend = 0f;
        dust = GetComponentInChildren<ParticleSystem>(true);
        if (dust != null)
        {
            dustTransform = dust.transform;
            dustFacingRight = dustTransform.localRotation;
        }
        StopMovingPresentation();
    }

    void Start()
    {
        level = FindAnyObjectByType<LevelGenerator>();
        if (level != null)
            level.GenerateLevel();

        if (level != null && level.HasMap)
            transform.position = level.CellCenter(level.WorldToCell(transform.position));

        FaceRightIdle();
        StopMovingPresentation();
    }

    void Update()
    {
        ReadInput();

        if (tween.Active)
        {
            transform.position = tween.Step(Time.deltaTime);
            if (tween.Active)
                return;
        }

        if (TryStep(lastInput) || TryStep(currentInput))
            return;

        StopMovingPresentation();
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
        if (!level.IsWalkableForPacStudent(nextCell))
            return false;

        if (input == lastInput)
            currentInput = lastInput;

        Vector3 start = level.CellCenter(fromCell);
        Vector3 end = level.CellCenter(nextCell);
        float tileSpeed = Mathf.Max(0.01f, tilesPerSecond) * Mathf.Max(0.01f, level.TileSize);
        float distance = Vector3.Distance(start, end);
        tween.Begin(start, end, distance / tileSpeed);
        transform.position = start;
        BeginMovingPresentation(step, level.IsPellet(nextCell));
        return true;
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
