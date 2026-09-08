using UnityEngine;
using UnityEngine.InputSystem;

public class HoopBot : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private BossController boss;

    private bool isBotActive;
    private bool hasReactedToCurrentState;
    private BossState lastSeenBossState = BossState.Idle;
    private float stateEnterTime;
    private float targetReactionDelay;
    private bool willMakeMistake;

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

        if (!MatchManager.Instance.IsPlaying)
        {
            return;
        }

        if (boss == null) boss = Object.FindFirstObjectByType<BossController>();
        if (player == null) player = GetComponent<PlayerController>();
        if (boss == null || player == null) return;

        BossState currentState = boss.CurrentState;
        if (currentState != lastSeenBossState)
        {
            lastSeenBossState = currentState;
            hasReactedToCurrentState = false;
            stateEnterTime = Time.time;
            targetReactionDelay = Random.Range(0.14f, 0.28f);
            willMakeMistake = Random.value < 0.18f;
        }

        if (hasReactedToCurrentState) return;
        if (Time.time - stateEnterTime < targetReactionDelay) return;

        ProcessBotDecision(currentState);
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
    }

    private void CheckToggleInput()
    {
        if (Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame) return;
        SetBotActive(!isBotActive);
    }

    private void ProcessBotDecision(BossState state)
    {
        switch (state)
        {
            case BossState.WindupAttack:
                hasReactedToCurrentState = true;
                if (willMakeMistake)
                {
                    if (Random.value < 0.5f)
                    {
                        DodgeDirection wrongDir = (boss.RequiredDodge == DodgeDirection.Left) ? DodgeDirection.Right : DodgeDirection.Left;
                        player.PerformDodge(wrongDir);
                    }
                }
                else
                {
                    player.PerformDodge(boss.RequiredDodge);
                }
                break;

            case BossState.WindupFake:
                hasReactedToCurrentState = true;
                if (willMakeMistake)
                {
                    player.PerformDodge(Random.value < 0.5f ? DodgeDirection.Left : DodgeDirection.Right);
                }
                break;

            case BossState.Stunned:
                hasReactedToCurrentState = true;
                AttackType attack = (Random.value < 0.75f) ? AttackType.HoldDunk : AttackType.TapShot;
                MatchManager.Instance.OnPlayerAttack(attack);
                break;
        }
    }
}
