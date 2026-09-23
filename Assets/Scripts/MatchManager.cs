using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private int targetScore = 15;
    [SerializeField] private float matchDuration = 120f;

    [SerializeField] private BossController boss;
    [SerializeField] private PlayerController player;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bossScoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Slider attackTimingSlider;
    [SerializeField] private GameObject hudPanel;

    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverTitleText;
    [SerializeField] private TMP_Text gameOverTitleShadowText;
    [SerializeField] private TMP_Text gameOverSubtitleText;
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button restartButton;

    [Header("Drible livre")]
    [SerializeField] private float openDistance = 1.15f;

    public bool IsPlaying { get; private set; }
    public bool HasBall { get; private set; }
    public bool BossPossession { get; private set; }
    public bool IsBossStunned { get; private set; }
    public float OpenDistance => openDistance;
    public int PlayerScore { get; private set; }
    public int BossScore { get; private set; }
    public int ComboStreak { get; private set; }

    private float timeRemaining;
    private Coroutine hitStopCoroutine;
    private Coroutine feedbackCoroutine;
    private Coroutine attackMeterCoroutine;

    private void Awake()
    {
        Application.runInBackground = true;
        Time.timeScale = 1f;
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(StartMatch);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);

        if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        HasBall = true;
        UpdateScoreUI();
        UpdateTimerUI(matchDuration);
        UpdateComboUI();
    }

    private void Update()
    {
        if (!IsPlaying) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerUI(0);
            EndMatch(PlayerScore > BossScore);
            return;
        }

        UpdateTimerUI(timeRemaining);
    }

    public void StartMatch()
    {
        StartMatchReal();
    }

    public void StartMatchReal()
    {
        PlayerScore = 0;
        BossScore = 0;
        ComboStreak = 0;
        timeRemaining = matchDuration;
        IsPlaying = true;
        HasBall = true;
        BossPossession = false;
        IsBossStunned = false;
        HideAttackMeter();
        ResetCourt();

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        if (HoopAudio.Instance != null)
        {
            HoopAudio.Instance.PlayWhistle();
            HoopAudio.Instance.PlayBGM();
        }

        UpdateScoreUI();
        UpdateComboUI();
        ShowFeedback("BORA!", Color.green);
    }

    public void OnPlayerDodge(DodgeDirection dir)
    {
        if (!IsPlaying) return;
        if (boss != null) boss.RegisterPlayerDodge(dir);
    }

    private void BeginBossPossession(string possessionMsg)
    {
        if (!IsPlaying) return;
        HasBall = false;
        BossPossession = true;
        IsBossStunned = false;
        HideAttackMeter();
        if (player != null) player.EnterGuardMode();
        ShowFeedback(possessionMsg, Color.yellow);
        if (boss != null) boss.StartBossAttack();
    }

    private IEnumerator TransitionToBossPossession(float delay, string msg)
    {
        yield return new WaitForSeconds(delay);
        if (!IsPlaying) yield break;
        BeginBossPossession(msg);
    }

    public void EndBossPossession()
    {
        if (!IsPlaying) return;
        BossPossession = false;
        IsBossStunned = false;
        HideAttackMeter();
        if (player != null) player.ExitGuardMode();
        ResetCourt();
        if (boss != null) boss.StopBossAttack();
    }

    public void BeginBossStun(float duration)
    {
        if (!IsPlaying) return;
        IsBossStunned = true;
        ShowFeedback("STUNNED! PONTUE AGORA!", Color.cyan);
        DoHitStop(0.06f);
        if (HoopCamera.Instance != null) HoopCamera.Instance.Shake(0.28f, 0.38f, 16);
        if (VFXManager.Instance != null) VFXManager.Instance.TriggerSpeedLines(0.4f);

        if (attackTimingSlider != null)
        {
            attackTimingSlider.gameObject.SetActive(true);
            if (attackMeterCoroutine != null) StopCoroutine(attackMeterCoroutine);
            attackMeterCoroutine = StartCoroutine(OscillateAttackMeter(duration));
        }
    }

    public void BossRecovered()
    {
        IsBossStunned = false;
        HideAttackMeter();
        ShowFeedback("O TIJOLO SE RECUPEROU!", Color.gray);
    }

    private IEnumerator OscillateAttackMeter(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && IsBossStunned)
        {
            elapsed += Time.deltaTime;
            if (attackTimingSlider != null)
            {
                attackTimingSlider.value = Mathf.Clamp01(1f - (elapsed / duration));
            }
            yield return null;
        }
        HideAttackMeter();
    }

    private void HideAttackMeter()
    {
        if (attackMeterCoroutine != null)
        {
            StopCoroutine(attackMeterCoroutine);
            attackMeterCoroutine = null;
        }
        if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);
    }

    public void OnPlayerAttack(AttackType type)
    {
        if (!IsPlaying) return;

        if (BossPossession)
        {
            if (!IsBossStunned)
            {
                ShowFeedback("DESVIE PARA STUNAR O TIJOLO!", Color.yellow);
                return;
            }
            IsBossStunned = false;
            HideAttackMeter();

            ComboStreak++;
            UpdateComboUI();
            float comboPitch = 1f + Mathf.Min(ComboStreak * 0.1f, 0.5f);
            ExecuteAttackHit(type, comboPitch);
            if (IsPlaying) StartCoroutine(ReturnToPlayerPossessionRoutine());
            return;
        }

        if (!HasBall) return;

        if (player == null) return;

        if (!player.InScoringArea)
        {
            FailShot("FORA DA ÁREA!", "CHEGUE PERTO DO ARO PARA PONTUAR!");
            return;
        }

        if (!PlayerIsOpen())
        {
            FailShot("BLOQUEADO!", "O TIJOLO CONTESTOU O CHUTE!");
            return;
        }

        if (type == AttackType.HoldDunk && !player.InDunkZone)
        {
            FailShot("LONGE DO ARO!", "A ENTERRADA PRECISA SER DE PERTO!");
            return;
        }

        ComboStreak++;
        UpdateComboUI();
        float comboPitch2 = 1f + Mathf.Min(ComboStreak * 0.1f, 0.5f);
        ExecuteAttackHit(type, comboPitch2);
        if (IsPlaying) StartCoroutine(ResetAfterScoreRoutine());
    }

    private IEnumerator ReturnToPlayerPossessionRoutine()
    {
        yield return new WaitForSeconds(0.9f);
        EndBossPossession();
    }

    private void FailShot(string feedbackMsg, string floatingMsg)
    {
        ShowFeedback(feedbackMsg, Color.red);
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlayCrowdOoh();
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ShowFloatingText(floatingMsg, Color.red, player.VisualPosition + new Vector3(0, 1.2f, 0), 1.2f);
        }

        HasBall = false;
        if (boss != null) boss.OnStealPossession("BOLA PERDIDA!");
        if (player != null) player.TriggerHit();
        StartCoroutine(TransitionToBossPossession(0.35f, "O TIJOLO TEM A BOLA!"));
    }

    public void OnBossSteal()
    {
        if (!IsPlaying || !HasBall) return;

        HasBall = false;
        if (boss != null) boss.OnStealPossession("ROUBOU A BOLA!");
        if (player != null) player.TriggerHit();
        StartCoroutine(TransitionToBossPossession(0.35f, "O TIJOLO TEM A BOLA!"));
    }

    private bool PlayerIsOpen()
    {
        if (player == null || boss == null) return true;
        return Vector2.Distance(player.VisualPosition, boss.VisualPosition) >= openDistance;
    }

    public void ResetCourt()
    {
        HasBall = true;
        if (player != null) player.ResetToStart();
        if (boss != null) boss.ResetToStart();
    }

    private IEnumerator ResetAfterScoreRoutine()
    {
        yield return new WaitForSeconds(0.9f);
        if (IsPlaying) ResetCourt();
    }

    private void ExecuteAttackHit(AttackType type, float comboPitch)
    {
        Vector3 hoopPos = new Vector3(0f, 2.4f, 0f);

        if (type == AttackType.HoldDunk)
        {
            DoHitStop(0.08f);
            if (HoopCamera.Instance != null) HoopCamera.Instance.Shake(0.48f, 0.65f, 24);
            if (HoopAudio.Instance != null) HoopAudio.Instance.PlaySlam(comboPitch);

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.TriggerSpeedLines(0.55f);
                VFXManager.Instance.SpawnShockwave(hoopPos, new Color(1f, 0.4f, 0f), 3.4f);
                VFXManager.Instance.SpawnImpactSparks(hoopPos, Color.yellow, 40);
                VFXManager.Instance.ShowFloatingText("POR CIMA DELE!", new Color(1f, 0.85f, 0.1f), hoopPos + new Vector3(0, 0.5f, 0), 1.35f);
            }

            if (player != null) player.TriggerSlamDunkAnimation();
            if (boss != null) boss.OnHitByPlayerAttack(AttackType.HoldDunk);

            AddPlayerScore(3, "ENTERRADA! +3");
        }
        else
        {
            DoHitStop(0.045f);
            if (HoopCamera.Instance != null) HoopCamera.Instance.Shake(0.22f, 0.28f, 15);
            if (HoopAudio.Instance != null) HoopAudio.Instance.PlaySwish(comboPitch);

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnImpactSparks(hoopPos, Color.cyan, 25);
                VFXManager.Instance.ShowFloatingText("DE LONGE! +2", Color.cyan, hoopPos + new Vector3(0, 0.4f, 0), 1.25f);
            }

            if (player != null) player.TriggerJumpShotAnimation();
            if (boss != null) boss.OnHitByPlayerAttack(AttackType.TapShot);

            AddPlayerScore(2, "CESTA LIMPA! +2");
        }
    }

    public void AddPlayerScore(int points, string reason)
    {
        PlayerScore += points;
        UpdateScoreUI();
        ShowFeedback(reason, Color.green);

        if (scoreText != null)
        {
            scoreText.transform.DOKill();
            scoreText.transform.DOPunchScale(Vector3.one * 0.35f, 0.22f);
        }

        if (PlayerScore >= targetScore)
        {
            EndMatch(true);
        }
    }

    public void AddBossScore(int points, string reason = null)
    {
        ComboStreak = 0;
        UpdateComboUI();
        BossScore += points;
        UpdateScoreUI();
        if (!string.IsNullOrEmpty(reason))
        {
            ShowFeedback(reason, Color.red);
        }

        DoHitStop(0.06f);
        if (HoopCamera.Instance != null) HoopCamera.Instance.Shake(0.35f, 0.45f, 18);
        if (player != null) player.TriggerHit();

        if (BossScore >= targetScore)
        {
            EndMatch(false);
        }
    }

    public void DoHitStop(float duration)
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            Time.timeScale = 1f;
        }
        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        hitStopCoroutine = null;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            if (bossScoreText != null)
            {
                scoreText.text = $"VOCÊ: {PlayerScore:D2}";
                bossScoreText.text = $"TIJOLO: {BossScore:D2}";
            }
            else
            {
                scoreText.text = $"VOCÊ {PlayerScore:D2}  x  {BossScore:D2} O TIJOLO";
            }
        }
    }

    private void UpdateTimerUI(float time)
    {
        if (timerText != null)
        {
            int mins = Mathf.FloorToInt(time / 60f);
            int secs = Mathf.FloorToInt(time % 60f);
            timerText.text = $"{mins:D2}:{secs:D2}";
        }
    }

    private void UpdateComboUI()
    {
        if (comboText == null) return;
        if (ComboStreak > 1)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"COMBO x{ComboStreak}!";
            comboText.transform.DOKill();
            comboText.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f);
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    public void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(FeedbackRoutine(message, color));
    }

    private IEnumerator FeedbackRoutine(string message, Color color)
    {
        feedbackText.DOKill();
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.alpha = 1f;
        feedbackText.transform.localScale = Vector3.one * 1.08f;
        feedbackText.transform.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(1.5f);

        DOTween.To(() => feedbackText.alpha, x => feedbackText.alpha = x, 0f, 0.35f)
            .OnComplete(() =>
            {
                if (feedbackText != null) feedbackText.text = "";
            });
    }

    private void EndMatch(bool playerWon)
    {
        IsPlaying = false;
        BossPossession = false;
        IsBossStunned = false;
        Time.timeScale = 1f;
        HideAttackMeter();

        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        if (feedbackText != null)
        {
            feedbackText.DOKill();
            feedbackText.text = "";
        }

        if (player != null) player.ResetToStart();
        if (boss != null) boss.ResetToStart();
        if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);

        if (HoopAudio.Instance != null)
        {
            HoopAudio.Instance.PlayWhistle();
            if (playerWon) HoopAudio.Instance.PlayCheer();
            HoopAudio.Instance.StopBGM();
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (gameOverTitleText != null)
        {
            string t = playerWon ? "VITÓRIA!" : "DERROTA!";
            gameOverTitleText.text = t;
            gameOverTitleText.color = playerWon ? new Color(1f, 0.88f, 0.15f) : new Color(1f, 0.28f, 0.38f);
            if (gameOverTitleShadowText != null)
            {
                gameOverTitleShadowText.text = t;
                gameOverTitleShadowText.color = new Color(0.03f, 0.01f, 0.07f, 1f);
            }
        }

        if (gameOverSubtitleText != null)
        {
            gameOverSubtitleText.text = playerWon ? "Você dominou o garrafão e mandou na quadra!" : "\"O Tijolo\" dominou o aro. Vai pra academia!";
            gameOverSubtitleText.color = playerWon ? new Color(0f, 0.94f, 1f) : new Color(0.85f, 0.85f, 0.9f);
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"VOCÊ {PlayerScore:D2}  x  {BossScore:D2} O TIJOLO\nMaior Combo: x{ComboStreak}";
        }
    }

    public void RestartGame()
    {
        DOTween.KillAll();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        DOTween.KillAll();
    }
}