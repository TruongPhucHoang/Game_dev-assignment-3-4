using UnityEngine;

public class CherryController : MonoBehaviour
{
    [SerializeField] Sprite cherrySprite;
    [SerializeField] float respawnDelay = 5f;
    [SerializeField] float tilesPerSecond = 3.5f;
    [SerializeField] float cherryScale = 1.75f;
    [SerializeField] float outsidePadding = 1f;

    LevelGenerator level;
    SpriteMask levelMask;
    GameObject cherry;
    float wait;
    bool moving;
    Vector3 from;
    Vector3 to;
    float duration;
    float elapsed;
    static Sprite maskSprite;

    void Start()
    {
        level = GetComponent<LevelGenerator>();
        if (level == null)
            level = FindAnyObjectByType<LevelGenerator>();
        if (level != null)
            level.GenerateLevel();

        EnsureMask();
        wait = respawnDelay;
    }

    void Update()
    {
        if (moving)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0.0001f ? 1f : elapsed / duration;
            if (t >= 1f)
            {
                DestroyCherry();
                return;
            }

            cherry.transform.position = from + (to - from) * t;
            return;
        }

        wait -= Time.deltaTime;
        if (wait <= 0f)
            Spawn();
    }

    void Spawn()
    {
        if (cherrySprite == null || level == null || !level.TryGetLevelRect(out Vector2 center, out Vector2 size))
        {
            wait = 0.25f;
            return;
        }

        EnsureMask();

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float levelEdge = EdgeDistance(dir, size.x * 0.5f, size.y * 0.5f);
        float cameraExit = CameraExit(center, dir);

        Vector3 origin = new Vector3(center.x, center.y, 0f);
        from = origin - (Vector3)(dir * (levelEdge + outsidePadding));
        to = origin + (Vector3)(dir * cameraExit);
        float distance = Vector3.Distance(from, to);
        float speed = Mathf.Max(0.01f, tilesPerSecond) * Mathf.Max(0.01f, level.TileSize);
        duration = distance / speed;
        elapsed = 0f;

        cherry = new GameObject("Cherry");
        cherry.transform.SetParent(transform, false);
        cherry.transform.position = from;
        cherry.transform.localScale = Vector3.one * cherryScale;

        var renderer = cherry.AddComponent<SpriteRenderer>();
        renderer.sprite = cherrySprite;
        renderer.sortingOrder = 100;
        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        moving = true;
    }

    void DestroyCherry()
    {
        if (cherry != null)
            Destroy(cherry);
        cherry = null;
        moving = false;
        wait = respawnDelay;
    }

    void EnsureMask()
    {
        if (levelMask != null || level == null || !level.TryGetLevelRect(out Vector2 center, out Vector2 size))
            return;

        if (maskSprite == null)
        {
            maskSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f),
                4f);
        }

        var maskObject = new GameObject("CherryMask");
        maskObject.transform.SetParent(transform, false);
        maskObject.transform.position = new Vector3(center.x, center.y, 0f);
        maskObject.transform.localScale = new Vector3(size.x, size.y, 1f);

        levelMask = maskObject.AddComponent<SpriteMask>();
        levelMask.sprite = maskSprite;
        levelMask.isCustomRangeActive = true;
        levelMask.frontSortingOrder = 200;
        levelMask.backSortingOrder = 50;
    }

    float CameraExit(Vector2 origin, Vector2 dir)
    {
        Camera cam = Camera.main;
        float levelEdge = 0f;
        if (level != null && level.TryGetLevelRect(out _, out Vector2 size))
            levelEdge = EdgeDistance(dir, size.x * 0.5f, size.y * 0.5f) + outsidePadding;

        if (cam == null || !cam.orthographic)
            return levelEdge + outsidePadding * 4f;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 camPos = cam.transform.position;
        float minX = camPos.x - halfW;
        float maxX = camPos.x + halfW;
        float minY = camPos.y - halfH;
        float maxY = camPos.y + halfH;
        float exit = RayExit(origin, dir, minX, maxX, minY, maxY);
        if (float.IsInfinity(exit))
            exit = levelEdge;
        return exit + outsidePadding;
    }

    static float EdgeDistance(Vector2 dir, float halfWidth, float halfHeight)
    {
        float tx = Mathf.Abs(dir.x) < 0.0001f ? float.PositiveInfinity : halfWidth / Mathf.Abs(dir.x);
        float ty = Mathf.Abs(dir.y) < 0.0001f ? float.PositiveInfinity : halfHeight / Mathf.Abs(dir.y);
        return Mathf.Min(tx, ty);
    }

    static float RayExit(Vector2 origin, Vector2 dir, float minX, float maxX, float minY, float maxY)
    {
        float t = float.PositiveInfinity;
        if (dir.x > 0.0001f)
            t = Mathf.Min(t, (maxX - origin.x) / dir.x);
        else if (dir.x < -0.0001f)
            t = Mathf.Min(t, (minX - origin.x) / dir.x);

        if (dir.y > 0.0001f)
            t = Mathf.Min(t, (maxY - origin.y) / dir.y);
        else if (dir.y < -0.0001f)
            t = Mathf.Min(t, (minY - origin.y) / dir.y);

        return t;
    }
}
