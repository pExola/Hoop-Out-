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

    [SerializeField] private GameObject tutorialInstructionPanel;
    [SerializeField] private TMP_Text tutorialStepTitle;
    [SerializeField] private TMP_Text tutorialStepBody;

    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverTitleText;
    [SerializeField] private TMP_Text gameOverTitleShadowText;
    [SerializeField] private TMP_Text gameOverSubtitleText;
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button restartButton;

    public bool IsPlaying { get; private set; }
    public bool IsTutorialActive { get; private set; }
    public int PlayerScore { get; private set; }
    public int BossScore { get; private set; }
    public int ComboStreak { get; private set; }

    private float timeRemaining;
    private bool isBossStunned;
    private bool tutorialAttackReceived;
    private Coroutine attackMeterCoroutine;
    private Coroutine hitStopCoroutine;
    private Coroutine tutorialRoutine;
    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        Application.runInBackground = true;
        Time.timeScale = 1f;
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
#if UNITY_EDITOR
        PlayerPrefs.DeleteKey("HoopOut_TutorialCompleted");
#endif
    }

    private void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(StartMatch);
        if (tutorialButton != null) tutorialButton.onClick.AddListener(StartGuidedTutorial);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);

        if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);
        if (tutorialInstructionPanel != null) tutorialInstructionPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

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
        if (PlayerPrefs.GetInt("HoopOut_TutorialCompleted", 0) == 0)
        {
            StartGuidedTutorial();
        }
        else
        {
            StartMatchReal();
        }
    }

    public void StartGuidedTutorial()
    {
        if (tutorialRoutine != null) StopCoroutine(tutorialRoutine);
        tutorialRoutine = StartCoroutine(GuidedTutorialRoutine());
    }

    public void StartMatchReal()
    {
        IsTutorialActive = false;
        if (tutorialInstructionPanel != null) tutorialInstructionPanel.SetActive(false);

        PlayerScore = 0;
        BossScore = 0;
        ComboStreak = 0;
        timeRemaining = matchDuration;
        IsPlaying = true;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        if (HoopAudio.Instance != null)
        {
            HoopAudio.Instance.PlayWhistle();
            HoopAudio.Instance.PlayBGM();
        }

        UpdateScoreUI();
        UpdateComboUI();
        ShowFeedback("LET'S GO!", Color.green);

        if (boss != null) boss.StartBossAI();
    }

    private IEnumerator GuidedTutorialRoutine()
    {
        IsTutorialActive = true;
        IsPlaying = true;
        PlayerScore = 0;
        BossScore = 0;
        ComboStreak = 0;
        timeRemaining = matchDuration;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (tutorialInstructionPanel != null) tutorialInstructionPanel.SetActive(true);

        UpdateScoreUI();
        UpdateTimerUI(matchDuration);
        UpdateComboUI();

        if (boss != null) boss.StopBossAI();

        ShowTutorialInstruction("STEP 1/3: THE FAKE", "The Brick will glow <color=#FFE600>YELLOW</color>!\n<color=#FF3366>DO NOT DODGE!</color> Hold your ground!");
        yield return new WaitForSeconds(2.2f);

        bool fakePassed = false;
        while (!fakePassed)
        {
            if (!IsPlaying) yield break;
            if (boss != null) boss.TriggerTutorialFake(1.8f);
            yield return new WaitForSeconds(1.8f);

            if (boss != null && boss.PlayerDodgedInWindow)
            {
                ShowTutorialInstruction("FELL FOR THE FAKE!", "<color=#FF3344>Hold your ground</color> when yellow!\nTry again...");
                ShowFeedback("FELL FOR THE FAKE!", Color.red);
                yield return new WaitForSeconds(1.8f);
            }
            else
            {
                fakePassed = true;
                if (boss != null) boss.EndTutorialStun();
                ShowFeedback("GREAT READ!", Color.cyan);
                ShowTutorialInstruction("GREAT READ!", "<color=#00F0FF>Locked in!</color> You ignored the fake.");
                yield return new WaitForSeconds(1.6f);
            }
        }

        ShowTutorialInstruction("STEP 2/3: THE ATTACK", "The Brick will glow <color=#FF3344>RED</color>!\n<color=#00F0FF>SWIPE QUICK</color> to dodge!\n(Swipe or [D])");
        yield return new WaitForSeconds(2.2f);

        bool dodgePassed = false;
        while (!dodgePassed)
        {
            if (!IsPlaying) yield break;
            if (boss != null) boss.TriggerTutorialAttack(DodgeDirection.Left, 2.5f);

            float timer = 2.5f;
            while (timer > 0f)
            {
                if (!IsPlaying) yield break;
                timer -= Time.deltaTime;
                if (boss != null && boss.PlayerDodgedInWindow && boss.LastPlayerDodge == DodgeDirection.Right)
                {
                    dodgePassed = true;
                    break;
                }
                yield return null;
            }

            if (dodgePassed)
            {
                if (boss != null) boss.TriggerTutorialStun(6f);
                isBossStunned = true;
                tutorialAttackReceived = false;
                ShowFeedback("ANKLE BREAKER!", Color.green);
                ShowTutorialInstruction("STEP 3/3: TAKE THE SHOT", "Boss is dazed! Score now:\n<color=#00F0FF>[TAP]</color> Jump Shot (+2 pts)\n<color=#FFE600>[HOLD]</color> SLAM DUNK (+3 pts)!");

                if (attackTimingSlider != null)
                {
                    attackTimingSlider.gameObject.SetActive(true);
                    if (attackMeterCoroutine != null) StopCoroutine(attackMeterCoroutine);
                    attackMeterCoroutine = StartCoroutine(OscillateAttackMeter(6f));
                }

                float attackTimeout = 6f;
                while (!tutorialAttackReceived && attackTimeout > 0f && isBossStunned)
                {
                    if (!IsPlaying) yield break;
                    attackTimeout -= Time.deltaTime;
                    yield return null;
                }

                if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);
                if (!tutorialAttackReceived && boss != null) boss.EndTutorialStun();
            }
            else
            {
                if (boss != null) boss.EndTutorialStun();
                ShowTutorialInstruction("HIT TAKEN!", "Dodge away from the attack!\nTry again...");
                yield return new WaitForSeconds(1.8f);
            }
        }

        ShowTutorialInstruction("TUTORIAL COMPLETE!", "<color=#FFE600>YOU'RE READY FOR THE ASPHALT!</color>\nShow 'em who runs the court!");
        ShowFeedback("GAME ON!", Color.green);
        yield return new WaitForSeconds(2.4f);

        PlayerPrefs.SetInt("HoopOut_TutorialCompleted", 1);
        PlayerPrefs.Save();

        StartMatchReal();
    }

    public void ShowTutorialInstruction(string title, string body)
    {
        if (tutorialInstructionPanel != null) tutorialInstructionPanel.SetActive(true);
        if (tutorialStepTitle != null) tutorialStepTitle.text = title;
        if (tutorialStepBody != null) tutorialStepBody.text = body;
        if (tutorialInstructionPanel != null)
        {
            tutorialInstructionPanel.transform.DOKill();
            tutorialInstructionPanel.transform.localScale = Vector3.one * 1.05f;
            tutorialInstructionPanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    public void OnPlayerDodge(DodgeDirection dir)
    {
        if (!IsPlaying) return;
        if (boss != null) boss.RegisterPlayerDodge(dir);
    }

    public void OnBossStunned(float duration)
    {
        isBossStunned = true;
        ShowFeedback("STUNNED! SCORE NOW!", Color.cyan);
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

    public void OnBossRecovered()
    {
        isBossStunned = false;
        if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);
        ShowFeedback("BOSS RECOVERED!", Color.gray);
    }

    private IEnumerator OscillateAttackMeter(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && isBossStunned)
        {
            elapsed += Time.deltaTime;
            if (attackTimingSlider != null)
            {
                attackTimingSlider.value = Mathf.Clamp01(1f - (elapsed / duration));
            }
            yield return null;
        }
        if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);
    }

    public void OnPlayerAttack(AttackType type)
    {
        if (!IsPlaying) return;

        if (!isBossStunned)
        {
            ShowFeedback(IsTutorialActive ? "WAIT FOR THE STUN!" : "DODGE FIRST!", Color.yellow);
            return;
        }

        isBossStunned = false;
        if (attackMeterCoroutine != null) StopCoroutine(attackMeterCoroutine);
        if (attackTimingSlider != null) attackTimingSlider.gameObject.SetActive(false);

        if (IsTutorialActive)
        {
            tutorialAttackReceived = true;
            ExecuteAttackHit(type, 1.15f);
        }
        else
        {
            ComboStreak++;
            UpdateComboUI();
            float comboPitch = 1f + Mathf.Min(ComboStreak * 0.1f, 0.5f);
            ExecuteAttackHit(type, comboPitch);
        }
    }

    private void ExecuteAttackHit(AttackType type, float comboPitch)
    {
        Vector3 hoopPos = new Vector3(0f, 2.3f, 0f);

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
                VFXManager.Instance.ShowFloatingText("POSTERIZED!", new Color(1f, 0.85f, 0.1f), hoopPos + new Vector3(0, 0.5f, 0), 1.35f);
            }

            if (player != null) player.TriggerSlamDunkAnimation();
            if (boss != null) boss.OnHitByPlayerAttack(AttackType.HoldDunk);

            AddPlayerScore(3, "SLAM DUNK! +3");
        }
        else
        {
            DoHitStop(0.045f);
            if (HoopCamera.Instance != null) HoopCamera.Instance.Shake(0.22f, 0.28f, 15);
            if (HoopAudio.Instance != null) HoopAudio.Instance.PlaySwish(comboPitch);

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnImpactSparks(hoopPos, Color.cyan, 25);
                VFXManager.Instance.ShowFloatingText("FROM DOWNTOWN! +2", Color.cyan, hoopPos + new Vector3(0, 0.4f, 0), 1.25f);
            }

            if (player != null) player.TriggerJumpShotAnimation();
            if (boss != null) boss.OnHitByPlayerAttack(AttackType.TapShot);

            AddPlayerScore(2, "SWISH! +2");
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
                scoreText.text = $"PLAYER: {PlayerScore:D2}";
                bossScoreText.text = $"BOSS: {BossScore:D2}";
            }
            else
            {
                scoreText.text = $"YOU {PlayerScore:D2}  x  {BossScore:D2} THE BRICK";
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
        Time.timeScale = 1f;

        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        if (feedbackText != null)
        {
            feedbackText.DOKill();
            feedbackText.text = "";
        }

        if (boss != null) boss.StopBossAI();
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
            string t = playerWon ? "VICTORY!" : "DEFEAT!";
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
            gameOverSubtitleText.text = playerWon ? "You owned the paint and ruled the court!" : "\"The Brick\" dominated the rim. Hit the gym!";
            gameOverSubtitleText.color = playerWon ? new Color(0f, 0.94f, 1f) : new Color(0.85f, 0.85f, 0.9f);
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"YOU {PlayerScore:D2}  x  {BossScore:D2} THE BRICK\nMax Combo: x{ComboStreak}";
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
