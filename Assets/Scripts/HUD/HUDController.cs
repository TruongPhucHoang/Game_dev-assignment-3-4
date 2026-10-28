using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] Button exitButton;
    [SerializeField] Text scoreText;
    [SerializeField] Text timerText;
    [SerializeField] Text scaredTimerText;
    [SerializeField] Text levelNameText;
    [SerializeField] string startSceneName = "StartScene";
    [SerializeField] string levelName = "Level 1";

    CanvasScaler scaler;
    int score;
    int lives = 3;
    Image[] lifeIcons;
    GameObject countdownOverlay;
    Text countdownText;
    float gameTime;
    bool timing;
    static Sprite overlaySprite;

    void Awake()
    {
        EnsureEventSystem();
        scaler = GetComponent<CanvasScaler>();
        ApplyAspectFit();

        if (scoreText != null)
            scoreText.text = "000000";
        if (timerText != null)
            timerText.text = "00:00:00";
        if (levelNameText != null)
            levelNameText.text = levelName;
        if (scaredTimerText != null)
        {
            scaredTimerText.text = "0";
            scaredTimerText.gameObject.SetActive(false);
        }

        EnsureCountdown();

        Transform livesRoot = transform.Find("Lives");
        if (livesRoot != null)
        {
            lifeIcons = new[]
            {
                livesRoot.Find("Life1")?.GetComponent<Image>(),
                livesRoot.Find("Life2")?.GetComponent<Image>(),
                livesRoot.Find("Life3")?.GetComponent<Image>()
            };
        }
    }

    public int Lives => lives;

    public void ShowScaredTimer(int seconds)
    {
        if (scaredTimerText == null)
            return;

        scaredTimerText.gameObject.SetActive(true);
        scaredTimerText.text = Mathf.Max(0, seconds).ToString();
    }

    public void HideScaredTimer()
    {
        if (scaredTimerText == null)
            return;

        scaredTimerText.gameObject.SetActive(false);
    }

    void Start()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToStart);
    }

    void Update()
    {
        if (!timing || timerText == null)
            return;

        gameTime += Time.deltaTime;
        int centiseconds = Mathf.FloorToInt(gameTime * 100f);
        int hundredths = centiseconds % 100;
        int totalSeconds = centiseconds / 100;
        int seconds = totalSeconds % 60;
        int minutes = totalSeconds / 60;
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00") + ":" + hundredths.ToString("00");
    }

    public void BeginCountdown(Action onFinished)
    {
        EnsureCountdown();
        StartCoroutine(PlayCountdown(onFinished));
    }

    public void StartGameTimer()
    {
        gameTime = 0f;
        timing = true;
        if (timerText != null)
            timerText.text = "00:00:00";
    }

    void OnRectTransformDimensionsChange()
    {
        ApplyAspectFit();
    }

    public void AddPoints(int points)
    {
        if (points <= 0)
            return;

        score += points;
        if (scoreText != null)
            scoreText.text = score.ToString("D6");
    }

    public bool LoseLife()
    {
        if (lives <= 0)
            return false;

        lives--;
        if (lifeIcons != null && lives >= 0 && lives < lifeIcons.Length && lifeIcons[lives] != null)
            lifeIcons[lives].enabled = false;
        return lives > 0;
    }

    void ExitToStart()
    {
        SceneManager.LoadScene(startSceneName);
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

    void EnsureCountdown()
    {
        if (countdownOverlay != null)
            return;

        countdownOverlay = new GameObject("CountdownOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        countdownOverlay.transform.SetParent(transform, false);
        RectTransform overlayRect = countdownOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image image = countdownOverlay.GetComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = new Color(0f, 0f, 0f, 0.62f);
        image.raycastTarget = true;

        GameObject textObject = new GameObject("CountdownText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        countdownText = textObject.GetComponent<Text>();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 200;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.color = Color.white;
        countdownText.raycastTarget = false;
        countdownText.horizontalOverflow = HorizontalWrapMode.Overflow;
        countdownText.verticalOverflow = VerticalWrapMode.Overflow;

        countdownOverlay.SetActive(false);
        textObject.SetActive(false);
    }

    IEnumerator PlayCountdown(Action onFinished)
    {
        string[] steps = { "3", "2", "1", "GO!" };
        for (int i = 0; i < steps.Length; i++)
        {
            ShowCountdown(steps[i]);
            yield return new WaitForSeconds(1f);
        }

        HideCountdown();
        if (onFinished != null)
            onFinished();
    }

    void ShowCountdown(string label)
    {
        if (countdownOverlay != null)
        {
            countdownOverlay.SetActive(true);
            countdownOverlay.transform.SetAsLastSibling();
        }

        if (countdownText != null)
        {
            countdownText.text = label;
            countdownText.gameObject.SetActive(true);
            countdownText.transform.SetAsLastSibling();
        }
    }

    void HideCountdown()
    {
        if (countdownOverlay != null)
            countdownOverlay.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    static Sprite WhiteSprite()
    {
        if (overlaySprite != null)
            return overlaySprite;

        Texture2D texture = Texture2D.whiteTexture;
        overlaySprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return overlaySprite;
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("InputController");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }
}
