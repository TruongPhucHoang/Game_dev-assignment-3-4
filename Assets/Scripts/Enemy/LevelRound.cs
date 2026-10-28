using UnityEngine;

public class LevelRound : MonoBehaviour
{
    public const float FrightSeconds = 10f;
    public const float RecoverSeconds = 3f;

    public static LevelRound Instance { get; private set; }
    public static bool Paused { get; private set; }
    public static float ScaredRemaining { get; private set; }

    HUDController hud;
    MusicController music;
    float scaredLeft;
    bool returnToNormalMusic;

    public static void Ensure()
    {
        if (Instance != null)
            return;

        var existing = FindAnyObjectByType<LevelRound>();
        if (existing != null)
            return;

        var go = new GameObject("LevelRound");
        go.AddComponent<LevelRound>();
    }

    void Awake()
    {
        Instance = this;
        hud = FindAnyObjectByType<HUDController>();
        music = FindAnyObjectByType<MusicController>();
        BindGhosts();
    }

    void Update()
    {
        if (Paused)
            return;

        if (scaredLeft > 0f)
        {
            scaredLeft -= Time.deltaTime;
            ScaredRemaining = Mathf.Max(0f, scaredLeft);
            if (scaredLeft <= 0f)
            {
                scaredLeft = 0f;
                ScaredRemaining = 0f;
                if (hud != null)
                    hud.HideScaredTimer();
                EnemyController.ForEach(ghost => ghost.ForceNormal());
            }
            else
            {
                if (hud != null)
                    hud.ShowScaredTimer(Mathf.CeilToInt(scaredLeft));
                float remaining = scaredLeft;
                EnemyController.ForEach(ghost => ghost.ApplyFright(remaining));
            }
        }

        FollowMusic();
    }

    public void BeginFright()
    {
        scaredLeft = FrightSeconds;
        ScaredRemaining = scaredLeft;
        if (hud != null)
            hud.ShowScaredTimer(Mathf.CeilToInt(scaredLeft));
        EnemyController.ForEach(ghost => ghost.ApplyFright(scaredLeft));
        returnToNormalMusic = true;
        FollowMusic();
    }

    public void BeginDeathPause()
    {
        Paused = true;
        EnemyController.SetMovementLocked(true);
    }

    public void ResetAfterDeath()
    {
        Paused = false;
        EnemyController.SetMovementLocked(false);
        scaredLeft = 0f;
        ScaredRemaining = 0f;
        if (hud != null)
            hud.HideScaredTimer();
        EnemyController.ForEach(ghost => ghost.ResetHome());
        returnToNormalMusic = false;
        if (music != null)
            music.PlayNormal();
    }

    void BindGhosts()
    {
        for (int i = 1; i <= 4; i++)
        {
            GameObject ghost = GameObject.Find("Ghost" + i);
            if (ghost != null && ghost.GetComponent<EnemyController>() == null)
                ghost.AddComponent<EnemyController>();
        }
    }

    void FollowMusic()
    {
        if (music == null)
            return;

        if (EnemyController.AnyDead())
        {
            returnToNormalMusic = true;
            music.PlayDead();
        }
        else if (scaredLeft > 0f)
        {
            returnToNormalMusic = true;
            music.PlayScared();
        }
        else if (returnToNormalMusic)
        {
            returnToNormalMusic = false;
            music.PlayNormal();
        }
    }
}
