using System.Collections;
using UnityEngine;
using DG.Tweening;

public enum BossState
{
    Idle,
    WindupFake,
    WindupAttack,
    Stunned,
    Scoring
}

public class BossController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite fakeSprite;
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private Sprite stunnedSprite;
    [SerializeField] private HitFlash hitFlash;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color fakeColor = new Color(1f, 0.9f, 0.1f);
    [SerializeField] private Color attackColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color stunnedColor = new Color(0.6f, 0.8f, 1f);

    [SerializeField] private float minIdleWait = 1.1f;
    [SerializeField] private float maxIdleWait = 1.7f;
    [SerializeField] private float windupDuration = 0.52f;
    [SerializeField] private float stunDuration = 2.4f;

    public BossState CurrentState { get; private set; } = BossState.Idle;
    public DodgeDirection RequiredDodge { get; private set; } = DodgeDirection.None;

    private Coroutine currentRoutine;
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private bool playerDodgedInWindow;
    private DodgeDirection lastPlayerDodge;

    private void Awake()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (hitFlash == null) hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
    }

    public void StartBossAI()
    {
        StopBossAI();
        currentRoutine = StartCoroutine(BossAILoop());
    }

    public void StopBossAI()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
        transform.DOKill();
        ResetVisuals();
    }

    private IEnumerator BossAILoop()
    {
        while (MatchManager.Instance != null && MatchManager.Instance.IsPlaying)
        {
            CurrentState = BossState.Idle;
            RequiredDodge = DodgeDirection.None;
            playerDodgedInWindow = false;
            lastPlayerDodge = DodgeDirection.None;
            ResetVisuals();

            if (HoopAudio.Instance != null) HoopAudio.Instance.PlayDribble();

            transform.DOLocalMoveY(initialPosition.y + 0.12f, 0.32f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            float idleTime = Random.Range(minIdleWait, maxIdleWait);
            yield return new WaitForSeconds(idleTime);

            if (!MatchManager.Instance.IsPlaying) yield break;

            bool isFake = Random.value < 0.35f;

            if (isFake)
            {
                CurrentState = BossState.WindupFake;
                RequiredDodge = DodgeDirection.None;
                SetVisualState(fakeSprite, fakeColor);
                transform.DOShakePosition(windupDuration, new Vector3(0.08f, 0.08f, 0), 15);

                yield return new WaitForSeconds(windupDuration);

                if (playerDodgedInWindow)
                {
                    yield return StartCoroutine(BossScoreRoutine());
                }
                else
                {
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.ShowFloatingText("FAKE READ!", Color.cyan, transform.position + new Vector3(0, 1.4f, 0), 1.2f);
                    }
                    MatchManager.Instance.ShowFeedback("FAKE READ!", Color.cyan);
                    yield return new WaitForSeconds(0.4f);
                }
            }
            else
            {
                CurrentState = BossState.WindupAttack;
                if (HoopAudio.Instance != null) HoopAudio.Instance.PlaySqueak();
                DodgeDirection attackDir = Random.value < 0.5f ? DodgeDirection.Left : DodgeDirection.Right;
                RequiredDodge = (attackDir == DodgeDirection.Left) ? DodgeDirection.Right : DodgeDirection.Left;

                SetVisualState(attackSprite, attackColor);
                float nudgeX = (attackDir == DodgeDirection.Left) ? -0.45f : 0.45f;
                transform.DOLocalMoveX(initialPosition.x + nudgeX, windupDuration * 0.5f).SetEase(Ease.OutQuad);

                yield return new WaitForSeconds(windupDuration);

                if (playerDodgedInWindow && lastPlayerDodge == RequiredDodge)
                {
                    yield return StartCoroutine(BossStunnedRoutine());
                }
                else
                {
                    yield return StartCoroutine(BossScoreRoutine());
                }
            }
        }
    }

    public bool PlayerDodgedInWindow => playerDodgedInWindow;
    public DodgeDirection LastPlayerDodge => lastPlayerDodge;

    public void ResetDodgeWindow()
    {
        playerDodgedInWindow = false;
        lastPlayerDodge = DodgeDirection.None;
    }

    public void TriggerTutorialFake(float duration = 1.6f)
    {
        StopBossAI();
        ResetDodgeWindow();
        CurrentState = BossState.WindupFake;
        RequiredDodge = DodgeDirection.None;
        SetVisualState(fakeSprite, fakeColor);
        transform.DOKill();
        transform.localPosition = initialPosition;
        transform.DOShakePosition(duration, new Vector3(0.08f, 0.08f, 0), 12);
    }

    public void TriggerTutorialAttack(DodgeDirection attackDir, float duration = 1.8f)
    {
        StopBossAI();
        ResetDodgeWindow();
        CurrentState = BossState.WindupAttack;
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlaySqueak();
        RequiredDodge = (attackDir == DodgeDirection.Left) ? DodgeDirection.Right : DodgeDirection.Left;
        SetVisualState(attackSprite, attackColor);
        float nudgeX = (attackDir == DodgeDirection.Left) ? -0.45f : 0.45f;
        transform.DOKill();
        transform.localPosition = initialPosition;
        transform.DOLocalMoveX(initialPosition.x + nudgeX, duration * 0.4f).SetEase(Ease.OutQuad);
    }

    public void TriggerTutorialStun(float duration = 4f)
    {
        StopBossAI();
        CurrentState = BossState.Stunned;
        SetVisualState(stunnedSprite, stunnedColor);
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlayCrowdOoh();
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ShowFloatingText("ANKLE BREAKER!", new Color(1f, 0.3f, 0.85f), transform.position + new Vector3(0, 1.3f, 0), 1.25f);
            VFXManager.Instance.SpawnImpactSparks(transform.position, Color.yellow, 8);
        }
        transform.DOKill();
        transform.localPosition = initialPosition;
        transform.localScale = initialScale;
        transform.DOShakeRotation(duration, new Vector3(0, 0, 12f), 8);
    }

    public void EndTutorialStun()
    {
        transform.DOKill();
        ResetVisuals();
        CurrentState = BossState.Idle;
    }

    public void RegisterPlayerDodge(DodgeDirection dir)
    {
        playerDodgedInWindow = true;
        lastPlayerDodge = dir;
    }

    private IEnumerator BossStunnedRoutine()
    {
        CurrentState = BossState.Stunned;
        MatchManager.Instance.OnBossStunned(stunDuration);
        SetVisualState(stunnedSprite, stunnedColor);
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlayCrowdOoh();

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ShowFloatingText("ANKLE BREAKER!", new Color(1f, 0.3f, 0.85f), transform.position + new Vector3(0, 1.3f, 0), 1.25f);
            VFXManager.Instance.SpawnImpactSparks(transform.position, Color.yellow, 8);
        }

        transform.DOKill();
        transform.localPosition = initialPosition;
        transform.localScale = initialScale;
        transform.DOShakeRotation(stunDuration, new Vector3(0, 0, 14f), 10);

        yield return new WaitForSeconds(stunDuration);

        if (CurrentState == BossState.Stunned)
        {
            MatchManager.Instance.OnBossRecovered();
            CurrentState = BossState.Idle;
            ResetVisuals();
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void OnHitByPlayerAttack(AttackType attackType)
    {
        if (hitFlash != null) hitFlash.Flash(0.09f);

        StopBossAI();
        transform.DOKill();
        ResetVisuals();

        CurrentState = BossState.Idle;
        currentRoutine = StartCoroutine(BossHitReactionRoutine(attackType));
    }

    private IEnumerator BossHitReactionRoutine(AttackType attackType)
    {
        Vector3 punchScale = (attackType == AttackType.HoldDunk) ? new Vector3(0.4f, -0.35f, 0) : new Vector3(0.2f, -0.18f, 0);
        transform.DOPunchScale(punchScale, 0.35f, 12, 1f);
        yield return new WaitForSeconds(0.36f);

        ResetVisuals();
        if (MatchManager.Instance != null && MatchManager.Instance.IsPlaying && !MatchManager.Instance.IsTutorialActive)
        {
            StartBossAI();
        }
    }

    private IEnumerator BossScoreRoutine(string reason = null)
    {
        CurrentState = BossState.Scoring;
        transform.DOKill();
        transform.localPosition = initialPosition;
        transform.localScale = initialScale;

        transform.DOLocalMoveY(initialPosition.y + 0.5f, 0.15f)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() =>
            {
                transform.localPosition = initialPosition;
                transform.localScale = initialScale;
            });

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ShowFloatingText("+2 THE BRICK", Color.red, transform.position + new Vector3(0, 1.2f, 0), 1.2f);
        }

        MatchManager.Instance.AddBossScore(2);
        yield return new WaitForSeconds(0.85f);
        ResetVisuals();
    }

    private void SetVisualState(Sprite sprite, Color color)
    {
        if (spriteRenderer != null)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
        }
    }

    private void ResetVisuals()
    {
        if (spriteRenderer != null)
        {
            if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            spriteRenderer.color = normalColor;
        }
        transform.localPosition = initialPosition;
        transform.localScale = initialScale;
        transform.localRotation = Quaternion.identity;
    }
}
