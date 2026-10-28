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

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("InputController");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }
}
