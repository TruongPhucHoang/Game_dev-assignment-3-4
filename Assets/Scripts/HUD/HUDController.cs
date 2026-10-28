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
