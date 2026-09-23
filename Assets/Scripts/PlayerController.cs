using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public enum AttackType
{
    TapShot,
    HoldDunk
}

public enum PlayerMoveDir
{
    None,
    Left,
    Right,
    Up,
    Down
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite dodgeLeftSprite;
    [SerializeField] private Sprite dodgeRightSprite;
    [SerializeField] private Sprite chargingSprite;
    [SerializeField] private HitFlash hitFlash;
    [SerializeField] private GameObject chargeAura;

    [Header("Drible (movimento)")]
    [SerializeField] private float swipeThresholdPixels = 30f;
    [SerializeField] private float lateralSpeed = 5.5f;
    [SerializeField] private float advanceSpeed = 4.4f;
    [SerializeField] private float retreatSpeed = 3.2f;
    [SerializeField] private float momentumDamping = 5f;
    [SerializeField] private float dribbleBounceRate = 20f;

    [Header("Arremesso")]
    [SerializeField] private float holdDurationThreshold = 0.26f;
    [SerializeField] private float shootLineY = 3.7f;
    [SerializeField] private float dunkLineY = 4.55f;

    [Header("Limites da quadra")]
    [SerializeField] private float minX = -1.9f;
    [SerializeField] private float maxX = 1.9f;
    [SerializeField] private float minY = 0f;
    [SerializeField] private float maxY = 4.9f;

    [Header("Posição visual do personagem no sprite")]
    [SerializeField] private Vector3 visualOffset = new Vector3(0f, -2.81f, 0f);

    public Vector3 Position => transform.position;
    public Vector3 VisualPosition => transform.position + visualOffset;
    public bool InScoringArea => transform.position.y >= shootLineY;
    public bool InDunkZone => transform.position.y >= dunkLineY;
    public bool IsShooting => isShooting;
    public int SwipeCount { get; private set; }
    public float Speed => velocity.magnitude;
    public Vector2 Velocity => velocity;

    private Vector2 velocity;
    private Vector3 startPosition;
    private Vector3 initialScale;
    private bool isShooting;
    private bool isPointerDown;
    private bool swipeHandled;
    private Vector2 pointerStartPos;
    private float pointerStartTime;
    private float lastDribbleT;
    private float lastMoveSign;

    private void Awake()
    {
        startPosition = transform.position;
        initialScale = transform.localScale;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (hitFlash == null) hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
        SetupChargeAura();
    }

    private void SetupChargeAura()
    {
        if (chargeAura != null) return;
        chargeAura = new GameObject("ChargeAura");
        chargeAura.transform.SetParent(transform, false);
        chargeAura.transform.localPosition = new Vector3(0, -3.25f, 0);
        LineRenderer lr = chargeAura.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 32;
        lr.startWidth = 0.12f;
        lr.endWidth = 0.12f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.9f, 0f, 0.95f);
        lr.endColor = new Color(0f, 0.95f, 1f, 0.95f);
        lr.sortingOrder = 6;
        for (int i = 0; i < 32; i++)
        {
            float rad = (i / 32f) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * 1.3f, Mathf.Sin(rad) * 0.65f, 0));
        }
        chargeAura.SetActive(false);
    }

    private void Update()
    {
        if (MatchManager.Instance == null) return;
        if (!MatchManager.Instance.IsPlaying)
        {
            HideCharge();
            return;
        }
        if (isShooting) return;

        HandleKeyboard();
        HandlePointer();
        IntegrateMovement();
        DribbleFeedback();
    }

    private void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) ApplyMove(PlayerMoveDir.Left);
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) ApplyMove(PlayerMoveDir.Right);

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) ApplyMove(PlayerMoveDir.Up);
        else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) ApplyMove(PlayerMoveDir.Down);

        if (kb.spaceKey.wasPressedThisFrame)
        {
            pointerStartTime = Time.time;
            ShowCharge();
        }
        if (kb.spaceKey.wasReleasedThisFrame)
        {
            float duration = Time.time - pointerStartTime;
            ReleasePress(duration);
        }
    }

    private void HandlePointer()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            pointerStartPos = pointer.position.ReadValue();
            pointerStartTime = Time.time;
            isPointerDown = true;
            swipeHandled = false;
            ShowCharge();
        }
        else if (pointer.press.isPressed && isPointerDown && !swipeHandled)
        {
            Vector2 currentPos = pointer.position.ReadValue();
            Vector2 delta = currentPos - pointerStartPos;
            if (delta.magnitude > swipeThresholdPixels)
            {
                swipeHandled = true;
                ApplyMove(Mathf.Abs(delta.y) > Mathf.Abs(delta.x)
                    ? (delta.y > 0 ? PlayerMoveDir.Up : PlayerMoveDir.Down)
                    : (delta.x > 0 ? PlayerMoveDir.Right : PlayerMoveDir.Left));
            }
        }
        else if (pointer.press.wasReleasedThisFrame && isPointerDown)
        {
            float duration = Time.time - pointerStartTime;
            ReleasePress(duration);
        }
    }

    private void ReleasePress(float duration)
    {
        isPointerDown = false;
        HideCharge();
        if (spriteRenderer != null && normalSprite != null && !isShooting) spriteRenderer.sprite = normalSprite;
        if (swipeHandled) return;
        AttackType type = duration >= holdDurationThreshold ? AttackType.HoldDunk : AttackType.TapShot;
        TryShoot(type);
    }

    private void ApplyMove(PlayerMoveDir dir)
    {
        switch (dir)
        {
            case PlayerMoveDir.Left:
                velocity.x = -lateralSpeed;
                lastMoveSign = -1f;
                SwipeCount++;
                break;
            case PlayerMoveDir.Right:
                velocity.x = lateralSpeed;
                lastMoveSign = 1f;
                SwipeCount++;
                break;
            case PlayerMoveDir.Up:
                velocity.y = advanceSpeed;
                SwipeCount++;
                break;
            case PlayerMoveDir.Down:
                velocity.y = -retreatSpeed;
                SwipeCount++;
                break;
        }

        if (spriteRenderer != null && !isShooting)
        {
            if (lastMoveSign < 0f && dodgeLeftSprite != null) spriteRenderer.sprite = dodgeLeftSprite;
            else if (lastMoveSign > 0f && dodgeRightSprite != null) spriteRenderer.sprite = dodgeRightSprite;
            else if (normalSprite != null) spriteRenderer.sprite = normalSprite;
        }
    }

    private void IntegrateMovement()
    {
        float dt = Time.deltaTime;
        transform.position += (Vector3)(velocity * dt);

        if (Mathf.Abs(velocity.x) > 0.01f)
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, momentumDamping * dt);
        if (Mathf.Abs(velocity.y) > 0.01f)
            velocity.y = Mathf.MoveTowards(velocity.y, 0f, momentumDamping * dt);

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }

    private void DribbleFeedback()
    {
        float speed = velocity.magnitude;
        if (speed > 0.15f)
        {
            float b = Mathf.Abs(Mathf.Sin(Time.time * dribbleBounceRate)) * 0.07f;
            transform.localScale = new Vector3(initialScale.x * (1f - b), initialScale.y * (1f + b), initialScale.z);

            if (Time.time - lastDribbleT > 0.3f)
            {
                lastDribbleT = Time.time;
                if (HoopAudio.Instance != null) HoopAudio.Instance.PlayDribble();
            }
        }
        else if (!isShooting)
        {
            transform.localScale = initialScale;
        }
    }

    public void BotSetVelocity(Vector2 dir)
    {
        if (isShooting) return;
        velocity.x = dir.x * lateralSpeed;
        velocity.y = dir.y * advanceSpeed;
        if (dir.y > 0f) SwipeCount++;
        if (Mathf.Abs(dir.x) > 0.01f) lastMoveSign = Mathf.Sign(dir.x);
    }

    public void BotShoot(AttackType type)
    {
        TryShoot(type);
    }

    public void TryShoot(AttackType type)
    {
        if (MatchManager.Instance == null) return;
        if (!MatchManager.Instance.IsPlaying || isShooting) return;
        if (!MatchManager.Instance.HasBall) return;
        MatchManager.Instance.OnPlayerAttack(type);
    }

    private void ShowCharge()
    {
        if (chargeAura == null) return;
        chargeAura.SetActive(true);
        if (spriteRenderer != null && chargingSprite != null) spriteRenderer.sprite = chargingSprite;
    }

    private void HideCharge()
    {
        if (chargeAura != null) chargeAura.SetActive(false);
    }

    public void TriggerJumpShotAnimation()
    {
        StartCoroutine(JumpShotRoutine());
    }

    private System.Collections.IEnumerator JumpShotRoutine()
    {
        isShooting = true;
        transform.DOKill();
        transform.localScale = initialScale;
        Vector3 basePos = transform.position;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 0.9f, initialScale.y * 1.15f, initialScale.z), 0.12f));
        seq.Append(transform.DOMoveY(basePos.y + 0.6f, 0.2f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMoveY(basePos.y, 0.18f).SetEase(Ease.InQuad));
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 1.1f, initialScale.y * 0.9f, initialScale.z), 0.08f));
        seq.Append(transform.DOScale(initialScale, 0.08f));

        yield return new WaitForSeconds(0.66f);

        transform.localScale = initialScale;
        isShooting = false;
    }

    public void TriggerSlamDunkAnimation()
    {
        StartCoroutine(SlamDunkRoutine());
    }

    private System.Collections.IEnumerator SlamDunkRoutine()
    {
        isShooting = true;
        transform.DOKill();
        transform.localScale = initialScale;
        Vector3 basePos = transform.position;

        float hopY = Mathf.Min(basePos.y + 1.3f, maxY);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 1.15f, initialScale.y * 0.85f, initialScale.z), 0.1f));
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 0.85f, initialScale.y * 1.25f, initialScale.z), 0.15f));
        seq.Join(transform.DOMoveY(hopY, 0.28f).SetEase(Ease.OutCubic));
        seq.Append(transform.DOMoveY(basePos.y, 0.2f).SetEase(Ease.InExpo));
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 1.25f, initialScale.y * 0.8f, initialScale.z), 0.1f));
        seq.Append(transform.DOScale(initialScale, 0.1f));

        yield return new WaitForSeconds(0.85f);

        transform.localScale = initialScale;
        isShooting = false;
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.SpawnDust(transform.position + new Vector3(0, -2.5f, 0), 8);
        }
    }

    public void TriggerHit()
    {
        if (hitFlash != null) hitFlash.Flash(0.08f);
        StartCoroutine(HitRoutine());
    }

    private System.Collections.IEnumerator HitRoutine()
    {
        transform.DOKill();
        transform.DOShakePosition(0.22f, new Vector3(0.2f, 0.2f, 0), 14);
        yield return new WaitForSeconds(0.24f);
        transform.localScale = initialScale;
    }

    public void ResetToStart()
    {
        StopAllCoroutines();
        isShooting = false;
        isPointerDown = false;
        swipeHandled = false;
        velocity = Vector2.zero;
        transform.DOKill();
        transform.position = startPosition;
        transform.localScale = initialScale;
        HideCharge();
        if (spriteRenderer != null && normalSprite != null) spriteRenderer.sprite = normalSprite;
    }
}