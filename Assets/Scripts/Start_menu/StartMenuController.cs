using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] Button level1Button;
    [SerializeField] Button level2Button;
    [SerializeField] string mainSceneName = "level_1";

    AudioSource clickAudio;
    AudioClip clickClip;
    AudioClip hoverClip;
    CanvasScaler scaler;

    void Awake()
    {
        EnsureEventSystem();
        scaler = GetComponent<CanvasScaler>();
        ApplyAspectFit();
        clickAudio = gameObject.GetComponent<AudioSource>();
        if (clickAudio == null)
            clickAudio = gameObject.AddComponent<AudioSource>();
        clickAudio.playOnAwake = false;
        clickAudio.spatialBlend = 0f;
        clickClip = MakeTone(660f, 0.08f);
        hoverClip = MakeTone(880f, 0.04f);
    }

    void Start()
    {
        WireButton(level1Button, LoadLevel1);
        WireButton(level2Button, null);
        ShowRecord(level1Button, "Level1");
        ShowRecord(level2Button, "Level2");
    }

    static void ShowRecord(Button button, string levelKey)
    {
        if (button == null)
            return;

        HighScoreRecord.Load(levelKey, out int score, out float time);
        Text[] labels = button.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string name = labels[i].gameObject.name;
            if (name.Contains("HighTime"))
                labels[i].text = HighScoreRecord.FormatTime(time);
            else if (name.Contains("HighScore"))
                labels[i].text = score.ToString("000000");
        }
    }

    void WireButton(Button button, UnityEngine.Events.UnityAction onClick)
    {
        if (button == null)
            return;

        if (onClick != null)
            button.onClick.AddListener(onClick);

        var feedback = button.GetComponent<MenuButtonFeedback>();
        if (feedback == null)
            feedback = button.gameObject.AddComponent<MenuButtonFeedback>();
        feedback.Bind(clickAudio, hoverClip, clickClip);
    }

    void LoadLevel1()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    void OnRectTransformDimensionsChange()
    {
        ApplyAspectFit();
    }

    void ApplyAspectFit()
    {
        if (scaler == null)
            scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("InputController");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static AudioClip MakeTone(float frequency, float duration)
    {
        const int rate = 44100;
        int samples = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
        var clip = AudioClip.Create("ui_tone", samples, 1, rate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float env = 1f - (t / duration);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * 0.22f;
        }
        clip.SetData(data, 0);
        return clip;
    }
}

public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    AudioSource audioSource;
    AudioClip hoverClip;
    AudioClip clickClip;
    Vector3 baseScale;

    public void Bind(AudioSource source, AudioClip hover, AudioClip click)
    {
        audioSource = source;
        hoverClip = hover;
        clickClip = click;
    }

    void Awake()
    {
        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 0.0001f)
            baseScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = baseScale * 1.08f;
        Play(hoverClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = baseScale * 0.94f;
        Play(clickClip);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = baseScale * 1.08f;
    }

    void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;
        audioSource.PlayOneShot(clip);
    }
}
