using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum Mood
    {
        Normal,
        Scared,
        Recovering,
        Dead
    }

    public static bool MovementLocked { get; private set; }

    const string NormalState = "Enemy1N_up";
    const string ScaredState = "Enemy1W_down";
    const string RecoveringState = "Recovering_enemy1_up";
    const string DeadState = "Enemy1_dead";

    static readonly List<EnemyController> all = new List<EnemyController>();

    public Mood State { get; private set; }
    public bool IsDead => State == Mood.Dead;
    public Vector3 Home => home;

    Animator animator;
    CircleCollider2D hit;
    Vector3 home;

    public static void SetMovementLocked(bool locked)
    {
        MovementLocked = locked;
    }

    public static void SetAllCollisions(bool enabled)
    {
        ForEach(ghost => ghost.SetCollisionsEnabled(enabled));
    }

    public void SetCollisionsEnabled(bool enabled)
    {
        if (hit != null)
            hit.enabled = enabled;
    }

    public static void ForEach(System.Action<EnemyController> action)
    {
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (all[i] == null)
            {
                all.RemoveAt(i);
                continue;
            }

            action(all[i]);
        }
    }

    public static bool AnyDead()
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && all[i].IsDead)
                return true;
        }

        return false;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
            animator.enabled = true;

        home = transform.position;
        float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        hit = GetComponent<CircleCollider2D>();
        if (hit == null)
            hit = gameObject.AddComponent<CircleCollider2D>();
        hit.isTrigger = true;
        hit.radius = 0.32f / scale;

        all.Add(this);
        PlayNormal();
    }

    void OnDestroy()
    {
        all.Remove(this);
    }

    public void ApplyFright(float remaining)
    {
        if (State == Mood.Dead)
            return;

        if (remaining <= LevelRound.RecoverSeconds)
        {
            if (State != Mood.Recovering)
                PlayRecovering();
        }
        else if (State != Mood.Scared)
        {
            PlayScared();
        }
    }

    public void ForceNormal()
    {
        if (State == Mood.Dead)
            return;

        PlayNormal();
    }

    public void Kill()
    {
        State = Mood.Dead;
        SetCollisionsEnabled(false);
        Play(DeadState);
        if (LevelRound.Instance != null)
            LevelRound.Instance.RefreshMusic();
    }

    public void ReviveAtHome()
    {
        transform.position = home;
        float scared = LevelRound.ScaredRemaining;
        if (scared <= 0f)
            PlayNormal();
        else if (scared <= LevelRound.RecoverSeconds)
            PlayRecovering();
        else
            PlayScared();

        SetCollisionsEnabled(true);
        if (LevelRound.Instance != null)
            LevelRound.Instance.RefreshMusic();
    }

    public void ResetHome()
    {
        transform.position = home;
        PlayNormal();
        SetCollisionsEnabled(true);
        GhostController mover = GetComponent<GhostController>();
        if (mover != null)
            mover.OnResetHome();
    }

    void PlayNormal()
    {
        State = Mood.Normal;
        Play(NormalState);
    }

    void PlayScared()
    {
        State = Mood.Scared;
        Play(ScaredState);
    }

    void PlayRecovering()
    {
        State = Mood.Recovering;
        Play(RecoveringState);
    }

    void Play(string stateName)
    {
        if (animator == null)
            return;

        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(stateName, 0, 0f);
    }
}
