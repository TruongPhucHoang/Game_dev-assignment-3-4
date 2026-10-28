using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class MenuBorderConveyor : MonoBehaviour
{
    [SerializeField] Sprite decorationSprite;
    [SerializeField] RuntimeAnimatorController animatorController;
    [SerializeField] int decorationCount = 24;
    [SerializeField] float decorationSize = 44f;
    [SerializeField] float conveyorSpeed = 90f;
    [SerializeField] float screenPadding = 18f;
    [SerializeField] float outerLineThickness = 5f;
    [SerializeField] float trackThickness = 62f;
    [SerializeField] float innerLineThickness = 5f;
    [SerializeField] Color outerLineColor = Color.white;
    [SerializeField] Color trackColor = new Color(0.55f, 0.84f, 1f, 1f);
    [SerializeField] Color innerLineColor = Color.white;

    RectTransform root;
    RectTransform[] decorations;
    Sprite whiteSprite;
    float distance;
    float lastWidth = -1f;
    float lastHeight = -1f;
    int builtCount = -1;
    readonly Vector2[] corners = new Vector2[4];
    readonly float[] edgeLengths = new float[4];
    float perimeter;

    void OnEnable()
    {
        EnsureVisuals();
        if (Application.isPlaying)
            PlayDecorationAnims();
    }

    void Start()
    {
        EnsureVisuals();
        if (Application.isPlaying)
            PlayDecorationAnims();
    }

    void Update()
    {
        if (root == null || decorations == null || AnyDecorationMissing())
            EnsureVisuals();
        RefreshPath();
        if (perimeter <= 0.0001f || decorations == null)
            return;

        if (Application.isPlaying)
        {
            distance += conveyorSpeed * Time.deltaTime;
            if (distance >= perimeter)
                distance -= perimeter;
        }
        else
        {
            distance = 0f;
        }

        PlaceAlongTrack();
    }

    void OnRectTransformDimensionsChange()
    {
        if (root == null)
            return;
        ApplyPadding();
        lastWidth = -1f;
        RefreshPath();
        PlaceAlongTrack();
    }

    void EnsureVisuals()
    {
        if (root == null)
            root = GetComponent<RectTransform>();
        ApplyPadding();
        if (whiteSprite == null)
        {
            whiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f),
                4f);
        }

        if (root.Find("OuterTop") == null)
            BuildFrame();

        int count = Mathf.Max(4, decorationCount);
        if (decorations == null || decorations.Length != count || builtCount != count || AnyDecorationMissing())
            BuildDecorations();
    }

    bool AnyDecorationMissing()
    {
        if (decorations == null)
            return true;
        for (int i = 0; i < decorations.Length; i++)
        {
            if (decorations[i] == null)
                return true;
        }
        return false;
    }

    void ApplyPadding()
    {
        Vector2 min = new Vector2(screenPadding, screenPadding);
        Vector2 max = new Vector2(-screenPadding, -screenPadding);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        if (root.anchorMin == Vector2.zero &&
            root.anchorMax == Vector2.one &&
            root.offsetMin == min &&
            root.offsetMax == max &&
            root.pivot == pivot)
            return;

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = min;
        root.offsetMax = max;
        root.pivot = pivot;
    }

    void BuildFrame()
    {
        MakeBar("OuterTop", outerLineColor);
        MakeBar("OuterRight", outerLineColor);
        MakeBar("OuterBottom", outerLineColor);
        MakeBar("OuterLeft", outerLineColor);
        MakeBar("TrackTop", trackColor);
        MakeBar("TrackRight", trackColor);
        MakeBar("TrackBottom", trackColor);
        MakeBar("TrackLeft", trackColor);
        MakeBar("InnerTop", innerLineColor);
        MakeBar("InnerRight", innerLineColor);
        MakeBar("InnerBottom", innerLineColor);
        MakeBar("InnerLeft", innerLineColor);
        lastWidth = -1f;
        LayoutFrame();
    }

    void LayoutFrame()
    {
        float o = outerLineThickness;
        float t = trackThickness;
        float inn = innerLineThickness;
        float ot = o + t;

        SetBar("OuterTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, o));
        SetBar("OuterBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, o));
        SetBar("OuterLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(o, 0f));
        SetBar("OuterRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(o, 0f));

        SetBar("TrackTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -o), new Vector2(-2f * o, t));
        SetBar("TrackBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, o), new Vector2(-2f * o, t));
        SetBar("TrackLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(o, 0f), new Vector2(t, -2f * o));
        SetBar("TrackRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-o, 0f), new Vector2(t, -2f * o));

        SetBar("InnerTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -ot), new Vector2(-2f * ot, inn));
        SetBar("InnerBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ot), new Vector2(-2f * ot, inn));
        SetBar("InnerLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(ot, 0f), new Vector2(inn, -2f * ot));
        SetBar("InnerRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-ot, 0f), new Vector2(inn, -2f * ot));
    }

    void BuildDecorations()
    {
        ClearDecorations();
        int count = Mathf.Max(4, decorationCount);
        decorations = new RectTransform[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Decoration_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            MarkGenerated(go);
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(decorationSize, decorationSize);

            var image = go.GetComponent<Image>();
            image.sprite = decorationSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (Application.isPlaying && animatorController != null)
            {
                var animator = go.AddComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = animatorController;
            }

            decorations[i] = rt;
        }

        builtCount = count;
        lastWidth = -1f;
        RefreshPath();
        PlaceAlongTrack();
    }

    void ClearDecorations()
    {
        if (root == null)
            return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == null || !child.name.StartsWith("Decoration_"))
                continue;
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        decorations = null;
        builtCount = -1;
    }

    void PlayDecorationAnims()
    {
        if (!Application.isPlaying || decorations == null)
            return;
        for (int i = 0; i < decorations.Length; i++)
        {
            if (decorations[i] == null)
                continue;
            var animator = decorations[i].GetComponent<Animator>();
            if (animator == null)
                continue;
            animator.Play("Menu_boarder", 0, (i / (float)decorations.Length) % 1f);
        }
    }

    void RefreshPath()
    {
        if (root == null)
            return;
        Rect r = root.rect;
        if (r.width < 1f || r.height < 1f)
            return;
        if (Mathf.Approximately(r.width, lastWidth) && Mathf.Approximately(r.height, lastHeight))
            return;
        LayoutFrame();

        lastWidth = r.width;
        lastHeight = r.height;
        float inset = outerLineThickness + trackThickness * 0.5f;
        float x0 = r.xMin + inset;
        float x1 = r.xMax - inset;
        float y0 = r.yMin + inset;
        float y1 = r.yMax - inset;
        corners[0] = new Vector2(x0, y0);
        corners[1] = new Vector2(x1, y0);
        corners[2] = new Vector2(x1, y1);
        corners[3] = new Vector2(x0, y1);

        perimeter = 0f;
        for (int i = 0; i < 4; i++)
        {
            edgeLengths[i] = Vector2.Distance(corners[i], corners[(i + 1) % 4]);
            perimeter += edgeLengths[i];
        }
    }

    void PlaceAlongTrack()
    {
        if (perimeter <= 0.0001f || decorations == null)
            return;
        float spacing = perimeter / decorations.Length;
        for (int i = 0; i < decorations.Length; i++)
        {
            if (decorations[i] == null)
                continue;
            float along = distance + spacing * i;
            if (along >= perimeter)
                along -= perimeter;
            decorations[i].anchoredPosition = PointOnTrack(along);
        }
    }

    Vector2 PointOnTrack(float along)
    {
        for (int i = 0; i < 4; i++)
        {
            float len = edgeLengths[i];
            if (along > len)
            {
                along -= len;
                continue;
            }

            if (len < 0.0001f)
                return corners[i];

            float t = along / len;
            Vector2 from = corners[i];
            Vector2 to = corners[(i + 1) % 4];
            return from + (to - from) * t;
        }

        return corners[0];
    }

    void MakeBar(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        MarkGenerated(go);
        go.transform.SetParent(root, false);
        var image = go.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.color = color;
        image.raycastTarget = false;
    }

    void SetBar(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var child = root.Find(name) as RectTransform;
        if (child == null)
            return;
        child.anchorMin = anchorMin;
        child.anchorMax = anchorMax;
        child.pivot = pivot;
        child.anchoredPosition = anchoredPos;
        child.sizeDelta = sizeDelta;
    }

    static void MarkGenerated(GameObject go)
    {
        go.hideFlags = HideFlags.DontSave;
    }
}
