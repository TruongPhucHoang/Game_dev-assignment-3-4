using UnityEngine;

public class LevelRound : MonoBehaviour
{
    public const float FrightSeconds = 10f;
    public const float RecoverSeconds = 3f;

    public static LevelRound Instance { get; private set; }
    public static bool Paused { get; private set; }
    public static bool RoundStarted { get; private set; }
    public static float ScaredRemaining { get; private set; }

    HUDController hud;
    MusicController music;
    float scaredLeft;
    bool returnToNormalMusic;
    bool gameOver;

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
        RoundStarted = false;
        Paused = true;
        EnemyController.SetMovementLocked(true);
        hud = FindAnyObjectByType<HUDController>();
        music = FindAnyObjectByType<MusicController>();
        BindGhosts();
        EnemyController.SetAllCollisions(false);
    }

    void Start()
    {
        if (hud == null)
            hud = FindAnyObjectByType<HUDController>();
        if (music == null)
            music = FindAnyObjectByType<MusicController>();

        BindGhosts();
        EnemyController.SetAllCollisions(false);
        if (music != null)
            music.PlayIntro();
        if (hud != null)
            hud.BeginCountdown(FinishCountdown);
    }

    void FinishCountdown()
    {
        RoundStarted = true;
        Paused = false;
        EnemyController.SetMovementLocked(false);
        EnemyController.SetAllCollisions(true);
        if (music != null)
        {
            music.StopIntro();
            music.PlayNormal();
        }

        if (hud != null)
            hud.StartGameTimer();
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

    public void EndRound()
    {
        if (gameOver)
            return;

        gameOver = true;
        Paused = true;
        EnemyController.SetMovementLocked(true);
        EnemyController.SetAllCollisions(false);

        PacStudentController player = FindAnyObjectByType<PacStudentController>();
        if (player != null)
            player.FreezeForGameOver();

        if (hud == null)
            hud = FindAnyObjectByType<HUDController>();
        if (hud != null)
            hud.ShowGameOver();
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
