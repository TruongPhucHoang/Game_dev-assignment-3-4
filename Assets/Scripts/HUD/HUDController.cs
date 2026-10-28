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
    [SerializeField] string highScoreId = "Level1";

    CanvasScaler scaler;
    int score;
    int lives = 3;
    Image[] lifeIcons;
    GameObject countdownOverlay;
    Text countdownText;
    Text continueTitle;
    Button saveAndLeaveButton;
    Button continueButton;
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

    public void PauseGameTimer()
    {
        timing = false;
    }

    public void ResumeTimer()
    {
        timing = true;
    }

    public float GameTimeSeconds => gameTime;

    public void ShowGameOver()
    {
        timing = false;
        HighScoreRecord.SaveIfBetter(highScoreId, score, gameTime);
        EnsureCountdown();
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].interactable = false;

        if (countdownOverlay != null)
        {
            countdownOverlay.SetActive(true);
            countdownOverlay.transform.SetAsLastSibling();
        }

        if (countdownText != null)
        {
            countdownText.fontSize = 140;
            countdownText.text = "Game Over";
            countdownText.gameObject.SetActive(true);
            countdownText.transform.SetAsLastSibling();
        }

        StartCoroutine(LeaveAfterGameOver());
    }

    public void ShowContinueChoice(Action onLeave, Action onContinue)
    {
        timing = false;
        EnsureCountdown();
        EnsureContinueButtons(onLeave, onContinue);

        if (exitButton != null)
            exitButton.interactable = false;

        if (countdownOverlay != null)
        {
            countdownOverlay.SetActive(true);
            countdownOverlay.transform.SetAsLastSibling();
        }

        if (continueTitle != null)
        {
            continueTitle.gameObject.SetActive(true);
            continueTitle.transform.SetAsLastSibling();
        }

        if (saveAndLeaveButton != null)
        {
            saveAndLeaveButton.gameObject.SetActive(true);
            saveAndLeaveButton.transform.SetAsLastSibling();
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.transform.SetAsLastSibling();
        }
    }

    public void HideContinueChoice()
    {
        if (continueTitle != null)
            continueTitle.gameObject.SetActive(false);
        if (saveAndLeaveButton != null)
            saveAndLeaveButton.gameObject.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (countdownOverlay != null)
            countdownOverlay.SetActive(false);
        if (exitButton != null)
            exitButton.interactable = true;
    }

    public void SaveAndLeave()
    {
        HighScoreRecord.SaveIfBetter(highScoreId, score, gameTime);
        SceneManager.LoadScene(startSceneName);
    }

    void EnsureContinueButtons(Action onLeave, Action onContinue)
    {
        if (continueTitle == null)
        {
            GameObject titleObject = new GameObject("ContinueTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObject.transform.SetParent(transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(900f, 140f);
            titleRect.anchoredPosition = new Vector2(0f, 150f);
            continueTitle = titleObject.GetComponent<Text>();
            continueTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            continueTitle.fontSize = 90;
            continueTitle.fontStyle = FontStyle.Bold;
            continueTitle.alignment = TextAnchor.MiddleCenter;
            continueTitle.color = Color.white;
            continueTitle.text = "CONTINUE?";
            continueTitle.raycastTarget = false;
        }

        saveAndLeaveButton = EnsureChoiceButton(saveAndLeaveButton, "SaveAndLeave", "Save score and leave", new Vector2(0f, -10f), onLeave);
        continueButton = EnsureChoiceButton(continueButton, "Continue", "Continue", new Vector2(0f, -120f), onContinue);
    }

    Button EnsureChoiceButton(Button existing, string name, string label, Vector2 position, Action onClick)
    {
        if (existing == null)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640f, 78f);
            rect.anchoredPosition = position;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = new Color(0.12f, 0.45f, 0.62f, 1f);

            existing = buttonObject.GetComponent<Button>();
            existing.targetGraphic = image;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
        }

        existing.onClick.RemoveAllListeners();
        existing.onClick.AddListener(() => onClick());
        return existing;
    }

    IEnumerator LeaveAfterGameOver()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(startSceneName);
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
        if (lives <= 0)
            timing = false;
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
