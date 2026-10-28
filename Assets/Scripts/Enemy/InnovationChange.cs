using UnityEngine;

public class InnovationChange : MonoBehaviour
{
    const float ContinueBoost = 1.05f;

    static InnovationChange current;
    float scale = 1f;

    public static float SpeedScale => current != null ? current.scale : 1f;

    public static void AttachIfInnovation()
    {
        if (current != null || LevelRound.Instance == null)
            return;
        if (FindAnyObjectByType<InnovationMap>() == null)
            return;

        LevelRound.Instance.gameObject.AddComponent<InnovationChange>();
    }

    void Awake()
    {
        current = this;
    }

    void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    public static void OnContinue()
    {
        if (current == null)
            return;

        current.scale *= ContinueBoost;
    }
}
