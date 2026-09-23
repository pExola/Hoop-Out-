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
    [SerializeField] private Color attackColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color stunnedColor = new Color(0.6f, 0.8f, 1f);

    [Header("IA de defesa (marcação + investida)")]
    [SerializeField] private float lateralSpeed = 3.0f;
    [SerializeField] private float verticalSpeed = 3.4f;
    [SerializeField] private float rideCharGap = 0.9f;
    [SerializeField] private float aimDelay = 0.12f;
    [SerializeField] private float aimBias = 0.35f;
    [SerializeField] private float lungeRange = 1.4f;
    [SerializeField] private float lungeSpeed = 4.6f;
    [SerializeField] private float lungeCommitTime = 0.5f;
    [SerializeField] private float lungeCooldown = 2.6f;
    [SerializeField] private float lungeWindupTime = 0.32f;
    [SerializeField] private float stealDistance = 0.55f;
    [SerializeField] private float stealBehindGrace = 0.25f;

    [Header("Ataque (posse do boss)")]
    [SerializeField] private Color fakeColor = new Color(1f, 0.9f, 0.1f);
    [SerializeField] private float minIdleWait = 1.0f;
    [SerializeField] private float maxIdleWait = 1.5f;
    [SerializeField] private float windupDuration = 0.52f;
    [SerializeField] private float stunDuration = 2.6f;
    [SerializeField] private float fakeChance = 0.35f;

    [Header("Posição visual do personagem no sprite")]
    [SerializeField] private Vector3 visualOffset = new Vector3(0f, 0.53f, 0f);

    public BossState CurrentState { get; private set; } = BossState.Idle;

    public bool IsLunging => lunging;

    public Vector3 VisualPosition => transform.position + visualOffset;

    public DodgeDirection RequiredDodge { get; private set; } = DodgeDirection.None;
    public bool PlayerDodgedInWindow => playerDodgedInWindow;
    public DodgeDirection LastPlayerDodge => lastPlayerDodge;

    private Vector3 startPosition;
    private Vector3 cachedInitialScale;
    private PlayerController player;
    private bool hasStolen;

    private float aimTimer;
    private float currentAimX;
    private bool lunging;
    private bool lungeDiving;
    private float lungeWindupLeft;
    private float lungeTimer;
    private Vector3 lungeTargetPos;
    private float nextLungeTime;

    private Coroutine attackCoroutine;
    private bool playerDodgedInWindow;
    private DodgeDirection lastPlayerDodge;

    private float LeadY
    {
        get
        {
            float playerVisualOffsetY = player != null ? (player.VisualPosition.y - player.Position.y) : -2.81f;
            return rideCharGap - (visualOffset.y - playerVisualOffsetY);
        }
    }

    private float ContactYOffset => (player != null ? (player.VisualPosition.y - player.Position.y) : -2.81f) - visualOffset.y;

    private void Awake()
    {
        startPosition = transform.position;
        cachedInitialScale = transform.localScale;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (hitFlash == null) hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
    }

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (MatchManager.Instance == null) return;
        if (player == null) return;
        if (!MatchManager.Instance.IsPlaying) return;
        if (!MatchManager.Instance.HasBall) return;
        if (CurrentState == BossState.Scoring) return;

        Defend();
        TrySteal();
    }

    private void Defend()
    {
        if (lunging)
        {
            if (!lungeDiving)
            {
                lungeWindupLeft -= Time.deltaTime;
                if (lungeWindupLeft <= 0f)
                {
                    lungeDiving = true;
                    transform.localScale = cachedInitialScale;
                }
                else
                {
                    FacePlayer();
                }
                return;
            }

            lungeTimer -= Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, lungeTargetPos, lungeSpeed * Time.deltaTime);
            FacePlayer();
            if (lungeTimer <= 0f)
            {
                lunging = false;
                lungeDiving = false;
                ResetVisuals();
            }
            return;
        }

        aimTimer -= Time.deltaTime;
        if (aimTimer <= 0f)
        {
            aimTimer = aimDelay + Random.Range(0f, 0.06f);
            currentAimX = player.Position.x + Random.Range(-aimBias, aimBias);
        }

        float targetY = Mathf.Clamp(player.Position.y + LeadY, -2.6f, 2.5f);

        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, currentAimX, lateralSpeed * Time.deltaTime);
        pos.y = Mathf.MoveTowards(pos.y, targetY, verticalSpeed * Time.deltaTime);
        transform.position = pos;
        FacePlayer();
    }

    private void FacePlayer()
    {
        float facing = player.Position.x - transform.position.x;
        if (Mathf.Abs(facing) > 0.05f && spriteRenderer != null)
        {
            float sign = Mathf.Sign(facing);
            transform.localScale = new Vector3(Mathf.Abs(cachedInitialScale.x) * sign, cachedInitialScale.y, cachedInitialScale.z);
        }
    }

    private void TrySteal()
    {
        if (player.IsShooting) return;
        if (hasStolen) return;

        float dist = Vector2.Distance(VisualPosition, player.VisualPosition);
        bool bossAhead = player.VisualPosition.y <= VisualPosition.y + stealBehindGrace;
        bool inArea = player.InScoringArea;

        if (lunging)
        {
            if (dist <= stealDistance && bossAhead && player.Speed <= 1.5f)
            {
                DoSteal();
            }
            return;
        }

        if (inArea && dist <= stealDistance && bossAhead && player.Speed < 0.9f)
        {
            DoSteal();
            return;
        }

        if (inArea && dist <= lungeRange && Time.time >= nextLungeTime)
        {
            lunging = true;
            lungeDiving = false;
            lungeWindupLeft = lungeWindupTime;
            lungeTimer = lungeCommitTime;
            lungeTargetPos = new Vector3(player.Position.x, player.Position.y + ContactYOffset, 0f);
            nextLungeTime = Time.time + lungeCooldown + Random.Range(0f, 0.6f);
            SetPressVisual();
        }
    }

    private void SetPressVisual()
    {
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.45f, 0.35f);
        transform.localScale = new Vector3(cachedInitialScale.x, cachedInitialScale.y * 0.92f, cachedInitialScale.z);
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlaySqueak();
    }

    private void DoSteal()
    {
        hasStolen = true;
        lunging = false;
        ResetVisuals();
        MatchManager.Instance.OnBossSteal();
    }

    public void OnStealPossession(string msg)
    {
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlaySqueak();
        CurrentState = BossState.Scoring;
        transform.DOKill();

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.SpawnImpactSparks(transform.position, Color.yellow, 6);
            VFXManager.Instance.ShowFloatingText(msg, Color.yellow, transform.position + new Vector3(0, 1.4f, 0), 1.25f);
        }
        MatchManager.Instance.ShowFeedback(msg, Color.yellow);

        Vector3 towardPlayer = (player != null ? player.Position : Vector3.zero) - transform.position;
        towardPlayer.z = 0;
        transform.DOPunchPosition(towardPlayer.normalized * 0.35f, 0.25f, 10, 0.5f);
    }

    public void PlayCounterScore()
    {
        StopAllCoroutines();
        StartCoroutine(CounterScoreRoutine());
    }

    private IEnumerator CounterScoreRoutine()
    {
        CurrentState = BossState.Scoring;
        transform.DOKill();
        Vector3 basePos = transform.position;

        if (HoopAudio.Instance != null) HoopAudio.Instance.PlayCrowdOoh();

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(cachedInitialScale.x * 0.9f, cachedInitialScale.y * 1.25f, cachedInitialScale.z), 0.15f));
        seq.Join(transform.DOMoveY(basePos.y + 0.5f, 0.2f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMoveY(basePos.y, 0.18f).SetEase(Ease.InQuad));
        seq.Append(transform.DOScale(new Vector3(cachedInitialScale.x * 1.12f, cachedInitialScale.y * 0.88f, cachedInitialScale.z), 0.08f));
        seq.Append(transform.DOScale(cachedInitialScale, 0.08f));

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ShowFloatingText("PONTO DO TIJOLO!", Color.red, transform.position + new Vector3(0, 1.2f, 0), 1.2f);
        }

        yield return new WaitForSeconds(0.8f);

        ResetVisuals();
        CurrentState = BossState.Idle;
    }

    public void StartBossAttack()
    {
        StopBossAttack();
        ResetDodgeWindow();
        attackCoroutine = StartCoroutine(BossAttackLoop());
    }

    public void StopBossAttack()
    {
        ResetDodgeWindow();
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        transform.DOKill();
        CurrentState = BossState.Idle;
        ResetVisuals();
    }

    public void RegisterPlayerDodge(DodgeDirection direction)
    {
        playerDodgedInWindow = true;
        lastPlayerDodge = direction;
    }

    public void ResetDodgeWindow()
    {
        playerDodgedInWindow = false;
        lastPlayerDodge = DodgeDirection.None;
    }

    private bool StillAttacking()
    {
        return MatchManager.Instance != null && MatchManager.Instance.IsPlaying && MatchManager.Instance.BossPossession;
    }

    private IEnumerator BossAttackLoop()
    {
        while (StillAttacking())
        {
            CurrentState = BossState.Idle;
            ResetDodgeWindow();
            ResetVisuals();

            if (HoopAudio.Instance != null) HoopAudio.Instance.PlayDribble();

            Vector3 idleBase = transform.position;
            transform.DOKill();
            transform.DOMoveY(idleBase.y + 0.12f, 0.32f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            float idleTime = Random.Range(minIdleWait, maxIdleWait);
            yield return new WaitForSeconds(idleTime);

            if (!StillAttacking()) yield break;

            bool isFake = Random.value < fakeChance;

            if (isFake)
            {
                CurrentState = BossState.WindupFake;
                RequiredDodge = DodgeDirection.None;
                SetVisualState(fakeSprite, fakeColor);
                transform.DOShakePosition(windupDuration, new Vector3(0.08f, 0.08f, 0), 15);

                yield return new WaitForSeconds(windupDuration);

                if (!StillAttacking()) yield break;

                if (PlayerDodgedInWindow)
                {
                    yield return StartCoroutine(BossAttackScoreRoutine("CAIU NA FALSA! +2"));
                    if (!StillAttacking()) yield break;
                }
                else
                {
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.ShowFloatingText("FAKE READ!", Color.cyan, transform.position + new Vector3(0, 1.4f, 0), 1.2f);
                    }
                    MatchManager.Instance.ShowFeedback("FAKE READ!", Color.cyan);
                    yield return new WaitForSeconds(0.4f);
                    if (!StillAttacking()) yield break;
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
                transform.DOKill();
                transform.DOMoveX(transform.position.x + nudgeX, windupDuration * 0.5f).SetEase(Ease.OutQuad);

                yield return new WaitForSeconds(windupDuration);

                if (!StillAttacking()) yield break;

                if (PlayerDodgedInWindow && LastPlayerDodge == RequiredDodge)
                {
                    yield return StartCoroutine(BossStunnedRoutine());
                    if (!StillAttacking()) yield break;
                }
                else
                {
                    yield return StartCoroutine(BossAttackScoreRoutine("O TIJOLO BOTOU PRA DENTRO! +2"));
                    if (!StillAttacking()) yield break;
                }
            }
        }
    }

    private IEnumerator BossAttackScoreRoutine(string reason)
    {
        CurrentState = BossState.Scoring;
        transform.DOKill();
        Vector3 basePos = transform.position;

        if (HoopAudio.Instance != null) HoopAudio.Instance.PlayCrowdOoh();

        Vector3 hoopPos = new Vector3(0f, 2.4f, 0f);
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.SpawnShockwave(hoopPos, new Color(1f, 0.25f, 0.25f), 3f);
            VFXManager.Instance.SpawnImpactSparks(hoopPos, Color.red, 30);
            VFXManager.Instance.ShowFloatingText("+2 O TIJOLO", Color.red, basePos + new Vector3(0, 1.4f, 0), 1.2f);
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(cachedInitialScale.x * 0.9f, cachedInitialScale.y * 1.25f, cachedInitialScale.z), 0.15f));
        seq.Join(transform.DOMoveY(basePos.y + 0.6f, 0.2f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMoveY(basePos.y, 0.18f).SetEase(Ease.InQuad));
        seq.Append(transform.DOScale(cachedInitialScale, 0.1f));

        MatchManager.Instance.AddBossScore(2, reason);

        yield return new WaitForSeconds(0.55f);

        MatchManager.Instance.EndBossPossession();
    }

    private IEnumerator BossStunnedRoutine()
    {
        CurrentState = BossState.Stunned;
        RequiredDodge = DodgeDirection.None;
        SetVisualState(stunnedSprite, stunnedColor);
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlayCrowdOoh();

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ShowFloatingText("ANKLE BREAKER!", new Color(1f, 0.3f, 0.85f), transform.position + new Vector3(0, 1.3f, 0), 1.25f);
            VFXManager.Instance.SpawnImpactSparks(transform.position, Color.yellow, 8);
        }

        MatchManager.Instance.BeginBossStun(stunDuration);

        transform.DOKill();
        transform.localScale = cachedInitialScale;
        transform.DOShakeRotation(stunDuration, new Vector3(0, 0, 14f), 10);

        yield return new WaitForSeconds(stunDuration);

        if (StillAttacking())
        {
            MatchManager.Instance.BossRecovered();
            CurrentState = BossState.Idle;
            ResetVisuals();
        }
    }

    private void SetVisualState(Sprite sprite, Color color)
    {
        if (spriteRenderer != null)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
        }
    }

    public void OnHitByPlayerAttack(AttackType attackType)
    {
        if (hitFlash != null) hitFlash.Flash(0.09f);
        transform.DOKill();
        if (attackType == AttackType.HoldDunk)
        {
            transform.DOPunchScale(new Vector3(0.4f, -0.35f, 0), 0.35f, 12, 1f);
        }
        else
        {
            transform.DOPunchScale(new Vector3(0.2f, -0.18f, 0), 0.35f, 12, 1f);
        }
    }

    public void ResetToStart()
    {
        StopAllCoroutines();
        hasStolen = false;
        lunging = false;
        lungeDiving = false;
        nextLungeTime = 0f;
        attackCoroutine = null;
        transform.DOKill();
        CurrentState = BossState.Idle;
        transform.position = startPosition;
        transform.localScale = cachedInitialScale;
        transform.localRotation = Quaternion.identity;
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (spriteRenderer != null)
        {
            if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            spriteRenderer.color = normalColor;
        }
        transform.localRotation = Quaternion.identity;
    }
}