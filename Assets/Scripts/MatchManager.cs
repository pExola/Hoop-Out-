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

    [Header("Drible livre")]
    [SerializeField] private float openDistance = 1.15f;

    public bool IsPlaying { get; private set; }
    public bool IsTutorialActive { get; private set; }
    public bool HasBall { get; private set; }
    public float OpenDistance => openDistance;
    public int PlayerScore { get; private set; }
    public int BossScore { get; private set; }
    public int ComboStreak { get; private set; }

    private float timeRemaining;
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
        HasBall = true;
        ResetCourt();

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
        ShowFeedback("BORA!", Color.green);
    }

    private IEnumerator GuidedTutorialRoutine()
    {
        IsTutorialActive = true;
        IsPlaying = true;
        PlayerScore = 0;
        BossScore = 0;
        ComboStreak = 0;
        timeRemaining = matchDuration;
        HasBall = true;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (tutorialInstructionPanel != null) tutorialInstructionPanel.SetActive(true);

        UpdateScoreUI();
        UpdateTimerUI(matchDuration);
        UpdateComboUI();
        ResetCourt();

        ShowTutorialInstruction("PASSO 1/3: O DRIBLE", "Deslize para <color=#00F0FF>MOVIMENTAR</color> o jogador!\n← ou → (lado), ↑ (avança), ↓ (recua).\n[Teclado: WASD / Setas]");
        float t = 0f;
        int swipeBaseline = 0;
        if (player != null) swipeBaseline = player.SwipeCount;
        while (player == null || player.SwipeCount < swipeBaseline + 4)
        {
            if (!IsPlaying) yield break;
            t += Time.deltaTime;
            if (t > 20f) break;
            yield return null;
        }
        ShowFeedback("DRIBLE AFINADO!", Color.cyan);

        ShowTutorialInstruction("PASSO 2/3: FURE A DEFESA", "Avance pegando o aro e <color=#FFE600>DESVIE</color> do Tijolo.\nCrie espaço na área de arremesso!");
        t = 0f;
        while (!(player != null && player.InScoringArea && PlayerIsOpen()))
        {
            if (!IsPlaying) yield break;
            t += Time.deltaTime;
            if (t > 12f)
            {
                ShowTutorialInstruction("PASSO 2/3: FURE A DEFESA", "Mude de direção bruscamente para <color=#FFE600>FURAR</color> a marcação!\nChegue perto do aro com o Tijolo longe.");
                t = 0f;
            }
            yield return null;
        }
        ShowFeedback("FUROU A DEFESA!", Color.cyan);

        ShowTutorialInstruction("PASSO 3/3: PONTUE", "Na área, mãos longe do Tijolo:\n<color=#00F0FF>[TOQUE]</color> Arremesso (+2 pts)\n<color=#FFE600>[SEGURE]</color> ENTERRADA (+3 pts)!");
        t = 0f;
        while (PlayerScore <= 0)
        {
            if (!IsPlaying) yield break;
            t += Time.deltaTime;
            if (t > 15f)
            {
                ShowTutorialInstruction("PASSO 3/3: PONTUE", "Crie espaço com o drible e segure no chute!\n<color=#FFE600>[SEGURE]</color> = enterrada, <color=#00F0FF>[TOQUE]</color> = arremesso.");
                t = 0f;
            }
            yield return null;
        }

        ShowTutorialInstruction("TUTORIAL CONCLUÍDO!", "<color=#FFE600>PRONTO PARA O ASFALTO!</color>\nMostra quem manda na quadra!");
        ShowFeedback("O JOGO COMEÇOU!", Color.green);
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

    public void OnPlayerAttack(AttackType type)
    {
        if (!IsPlaying || !HasBall) return;

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
        float comboPitch = 1f + Mathf.Min(ComboStreak * 0.1f, 0.5f);
        if (IsTutorialActive) comboPitch = 1.15f;
        ExecuteAttackHit(type, comboPitch);
        if (IsPlaying) StartCoroutine(ResetAfterScoreRoutine());
    }

    private void FailShot(string feedbackMsg, string floatingMsg)
    {
        ShowFeedback(feedbackMsg, Color.red);
        if (HoopAudio.Instance != null) HoopAudio.Instance.PlayCrowdOoh();
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.ShowFloatingText(floatingMsg, Color.red, player.VisualPosition + new Vector3(0, 1.2f, 0), 1.2f);
        }

        if (IsTutorialActive)
        {
            StartCoroutine(TutorialFailRoutine());
            return;
        }

        HasBall = false;
        if (boss != null) boss.OnStealPossession("BOLA PERDIDA!");
        if (player != null) player.TriggerHit();
        StartCoroutine(BossCounterRoutine("O TIJOLO PUNIU O ERRO! +2"));
    }

    private IEnumerator TutorialFailRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (!IsPlaying) yield break;
        ResetCourt();
    }

    public void OnBossSteal()
    {
        if (!IsPlaying || !HasBall) return;
        if (IsTutorialActive)
        {
            ResetCourt();
            return;
        }

        HasBall = false;
        if (boss != null) boss.OnStealPossession("ROUBOU A BOLA!");
        if (player != null) player.TriggerHit();
        StartCoroutine(BossCounterRoutine("O TIJOLO CONTRATACA E PONTUA! +2"));
    }

    private IEnumerator BossCounterRoutine(string reason)
    {
        yield return new WaitForSeconds(0.45f);
        if (!IsPlaying) yield break;

        Vector3 hoopPos = new Vector3(0f, 2.4f, 0f);
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.SpawnShockwave(hoopPos, new Color(1f, 0.25f, 0.25f), 3f);
            VFXManager.Instance.SpawnImpactSparks(hoopPos, Color.red, 30);
        }
        if (boss != null) boss.PlayCounterScore();

        AddBossScore(2, reason);

        yield return new WaitForSeconds(1.15f);
        if (IsPlaying) ResetCourt();
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
        Time.timeScale = 1f;

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