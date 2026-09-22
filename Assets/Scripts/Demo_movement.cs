using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class Demo_movement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] AudioClip Normal_moving;
    [SerializeField] Grid levelGrid;

    static readonly Vector2Int[] LoopCells =
    {
        new Vector2Int(2, 1),
        new Vector2Int(7, 1),
        new Vector2Int(7, 5),
        new Vector2Int(2, 5)
    };

    static readonly string[] MoveStates =
    {
        "Right_move",
        "Down_move",
        "Left_move",
        "Up_move"
    };

    Animator animator;
    AudioSource movingAudio;
    Vector3 from;
    Vector3 to;
    float segmentLength;
    float t;
    int corner;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movingAudio = GetComponent<AudioSource>();
        movingAudio.playOnAwake = false;
        movingAudio.loop = true;
        movingAudio.spatialBlend = 0f;
        if (Normal_moving != null)
            movingAudio.clip = Normal_moving;
    }

    void Start()
    {
        if (levelGrid == null)
            levelGrid = FindAnyObjectByType<Grid>();

        corner = 0;
        BeginSegment();
        PlayMovingAudio();
    }

    void Update()
    {
        if (segmentLength <= 0f)
            return;

        t += moveSpeed * Time.deltaTime / segmentLength;
        if (t >= 1f)
        {
            transform.position = to;
            corner = (corner + 1) % LoopCells.Length;
            BeginSegment();
            return;
        }

        transform.position = from + (to - from) * t;
        KeepMoveState();
    }

    void BeginSegment()
    {
        int next = (corner + 1) % LoopCells.Length;
        from = CellCenter(LoopCells[corner]);
        to = CellCenter(LoopCells[next]);
        segmentLength = Vector3.Distance(from, to);
        if (segmentLength < 0.0001f)
            segmentLength = 0.0001f;
        t = 0f;
        transform.position = from;
        animator.Play(MoveStates[corner], 0, 0f);
    }

    void KeepMoveState()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(MoveStates[corner]))
            animator.Play(MoveStates[corner], 0, 0f);
    }

    void PlayMovingAudio()
    {
        if (movingAudio.clip == null)
            return;
        if (!movingAudio.isPlaying)
            movingAudio.Play();
    }

    Vector3 CellCenter(Vector2Int cell)
    {
        var mapCell = new Vector3Int(cell.x, -cell.y, 0);
        if (levelGrid != null)
            return levelGrid.GetCellCenterWorld(mapCell);
        return new Vector3(cell.x + 0.5f, -cell.y + 0.5f, 0f);
    }
}
