using UnityEngine;
using UnityEngine.InputSystem;

public class HoopBot : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private BossController boss;

    private bool isBotActive;
    private float nextDecisionTime;
    private float shootCooldown;
    private bool hasReactedToState;
    private BossState lastHandledState = BossState.Idle;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (boss == null) boss = Object.FindFirstObjectByType<BossController>();
    }

    private void Update()
    {
        CheckToggleInput();

        if (!isBotActive) return;
        if (MatchManager.Instance == null) return;
        if (!MatchManager.Instance.IsPlaying) return;

        if (boss == null) boss = Object.FindFirstObjectByType<BossController>();
        if (player == null) player = GetComponent<PlayerController>();
        if (boss == null || player == null) return;

        if (player.IsShooting) return;

        if (MatchManager.Instance.BossPossession)
        {
            HandleGuardMode();
            return;
        }

        if (!MatchManager.Instance.HasBall) return;

        Vector2 move = ComputeMove();
        player.BotSetVelocity(move);

        if (Vec2Equals(move, Vector2.zero) && Time.time >= shootCooldown)
        {
            shootCooldown = Time.time + 1.4f;
            AttackType type = (player.InDunkZone && Random.value < 0.6f)
                ? AttackType.HoldDunk
                : AttackType.TapShot;
            player.BotShoot(type);
        }
    }

    private void HandleGuardMode()
    {
        BossState st = boss.CurrentState;
        if (st != lastHandledState)
        {
            lastHandledState = st;
            hasReactedToState = false;
        }

        switch (st)
        {
            case BossState.WindupAttack:
                if (hasReactedToState) break;
                hasReactedToState = true;
                if (Random.value < 0.3f)
                {
                    DodgeDirection wrongDir = boss.RequiredDodge == DodgeDirection.Left
                        ? DodgeDirection.Right
                        : DodgeDirection.Left;
                    player.PerformDodge(wrongDir);
                }
                else
                {
                    player.PerformDodge(boss.RequiredDodge);
                }
                break;

            case BossState.WindupFake:
                if (hasReactedToState) break;
                hasReactedToState = true;
                if (Random.value < 0.35f)
                {
                    player.PerformDodge(Random.value < 0.5f ? DodgeDirection.Left : DodgeDirection.Right);
                }
                break;

            case BossState.Stunned:
                if (hasReactedToState) break;
                hasReactedToState = true;
                AttackType attack = Random.value < 0.55f ? AttackType.HoldDunk : AttackType.TapShot;
                player.BotShoot(attack);
                break;

            default:
                hasReactedToState = false;
                break;
        }
    }

    private static bool Vec2Equals(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }

    private Vector2 ComputeMove()
    {
        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + Random.Range(0.08f, 0.22f);
            ComputeDecision(out activeMoveX, out activeMoveY);
        }

        if (activeMoveY != 0f)
        {
            return new Vector2(activeMoveX, 1f);
        }
        return new Vector2(activeMoveX, 0f);
    }

    private float activeMoveX;
    private float activeMoveY;
    private float jukeDir = 1f;
    private float lastAwayScan = -99f;

    private void ComputeDecision(out float moveX, out float moveY)
    {
        Vector3 vp = player.VisualPosition;
        Vector3 vb = boss.VisualPosition;
        float hSep = vp.x - vb.x;
        float dist = Vector2.Distance(vp, vb);

        if (player.InScoringArea)
        {
            if (dist >= MatchManager.Instance.OpenDistance)
            {
                moveX = 0f;
                moveY = 0f;
                return;
            }

            if (Time.time - lastAwayScan > 0.3f)
            {
                lastAwayScan = Time.time;
                if (Mathf.Abs(hSep) > 0.1f) jukeDir = Mathf.Sign(hSep);
                else if (Random.value < 0.5f) jukeDir = 1f;
                else jukeDir = -1f;
            }

            float dirX = jukeDir;
            float px = player.Position.x;
            if ((dirX > 0f && px > 1.35f) || (dirX < 0f && px < -1.35f))
            {
                dirX = -dirX;
                jukeDir = dirX;
            }

            float wob = Mathf.Sin(Time.time * 2.8f) * 0.25f;
            moveX = Mathf.Clamp(dirX + wob, -1f, 1f);
            moveY = 0f;
            return;
        }

        bool bossAhead = vb.y > vp.y;
        if (dist < 2.2f && bossAhead)
        {
            moveX = hSep > 0.05f ? Mathf.Sign(hSep) : (Random.value < 0.5f ? 1f : -1f);
            if (Random.value < 0.08f) moveX = -moveX;
        }
        else
        {
            float px = player.Position.x;
            if (px < -0.3f) moveX = 1f;
            else if (px > 0.3f) moveX = -1f;
            else moveX = Mathf.Sin(Time.time * 1.7f) * 0.5f;
        }
        moveY = 1f;
    }

    public bool IsBotActive => isBotActive;

    public void SetBotActive(bool active)
    {
        isBotActive = active;
        if (isBotActive)
        {
            if (MatchManager.Instance != null && !MatchManager.Instance.IsPlaying)
            {
                MatchManager.Instance.StartMatch();
            }
        }
        else
        {
            activeMoveX = 0f;
            activeMoveY = 0f;
            hasReactedToState = false;
        }
    }

    private void CheckToggleInput()
    {
        if (Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame) return;
        SetBotActive(!isBotActive);
    }
}