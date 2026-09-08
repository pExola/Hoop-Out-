using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public enum DodgeDirection
{
    None,
    Left,
    Right
}

public enum AttackType
{
    TapShot,
    HoldDunk
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float dodgeDuration = 0.2f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite dodgeLeftSprite;
    [SerializeField] private Sprite dodgeRightSprite;
    [SerializeField] private Sprite chargingSprite;
    [SerializeField] private HitFlash hitFlash;
    [SerializeField] private GameObject chargeAura;

    [SerializeField] private float swipeThresholdPixels = 35f;
    [SerializeField] private float holdDurationThreshold = 0.26f;

    private Vector3 initialPosition;
    private Vector3 initialScale;
    private bool isBusy;
    private Vector2 pointerStartPos;
    private float pointerStartTime;
    private bool isPointerDown;
    private bool swipeHandled;

    private void Awake()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (hitFlash == null) hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();

        if (chargeAura == null)
        {
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
    }

    private void OnEnable()
    {
        isBusy = false;
        isPointerDown = false;
        swipeHandled = false;
    }

    private void Update()
    {
        if (MatchManager.Instance == null || !MatchManager.Instance.IsPlaying)
        {
            if (chargeAura != null && chargeAura.activeSelf) chargeAura.SetActive(false);
            return;
        }

        if (isBusy)
        {
            if (chargeAura != null && chargeAura.activeSelf) chargeAura.SetActive(false);
            return;
        }

        HandleKeyboard();
        HandlePointer();

        bool isCharging = (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) || (isPointerDown && !swipeHandled);
        if (chargeAura != null)
        {
            if (isCharging && (Time.time - pointerStartTime) > 0.1f)
            {
                chargeAura.SetActive(true);
                float pulse = 1f + Mathf.Sin(Time.time * 26f) * 0.12f;
                chargeAura.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
            else if (chargeAura.activeSelf)
            {
                chargeAura.SetActive(false);
            }
        }
    }

    private void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
        {
            PerformDodge(DodgeDirection.Left);
        }
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
        {
            PerformDodge(DodgeDirection.Right);
        }

        if (kb.spaceKey.wasPressedThisFrame)
        {
            pointerStartTime = Time.time;
            if (chargingSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = chargingSprite;
            }
        }
        if (kb.spaceKey.wasReleasedThisFrame)
        {
            if (spriteRenderer != null && normalSprite != null)
            {
                spriteRenderer.sprite = normalSprite;
            }

            float pressDuration = Time.time - pointerStartTime;
            if (pressDuration >= holdDurationThreshold)
            {
                TriggerAttack(AttackType.HoldDunk);
            }
            else
            {
                TriggerAttack(AttackType.TapShot);
            }
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
            if (chargingSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = chargingSprite;
            }
        }
        else if (pointer.press.isPressed && isPointerDown && !swipeHandled)
        {
            Vector2 currentPos = pointer.position.ReadValue();
            Vector2 delta = currentPos - pointerStartPos;

            if (Mathf.Abs(delta.x) > swipeThresholdPixels && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                swipeHandled = true;
                if (spriteRenderer != null && normalSprite != null)
                {
                    spriteRenderer.sprite = normalSprite;
                }
                PerformDodge(delta.x < 0 ? DodgeDirection.Left : DodgeDirection.Right);
            }
        }
        else if (pointer.press.wasReleasedThisFrame && isPointerDown)
        {
            isPointerDown = false;
            if (spriteRenderer != null && normalSprite != null)
            {
                spriteRenderer.sprite = normalSprite;
            }

            if (!swipeHandled)
            {
                float duration = Time.time - pointerStartTime;
                if (duration >= holdDurationThreshold)
                {
                    TriggerAttack(AttackType.HoldDunk);
                }
                else
                {
                    TriggerAttack(AttackType.TapShot);
                }
            }
        }
    }

    public void PerformDodge(DodgeDirection direction)
    {
        if (isBusy) return;
        StopAllCoroutines();
        StartCoroutine(DodgeRoutine(direction));
    }

    private System.Collections.IEnumerator DodgeRoutine(DodgeDirection direction)
    {
        isBusy = true;

        if (spriteRenderer != null)
        {
            if (direction == DodgeDirection.Left && dodgeLeftSprite != null)
                spriteRenderer.sprite = dodgeLeftSprite;
            else if (direction == DodgeDirection.Right && dodgeRightSprite != null)
                spriteRenderer.sprite = dodgeRightSprite;
        }

        if (HoopAudio.Instance != null)
        {
            HoopAudio.Instance.PlaySqueak();
            HoopAudio.Instance.PlayDodge();
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.SpawnDust(transform.position + new Vector3(0, -2.5f, 0), 5);
        }

        MatchManager.Instance.OnPlayerDodge(direction);

        transform.DOKill();
        transform.localScale = initialScale;

        transform.DOScale(new Vector3(initialScale.x * 1.1f, initialScale.y * 0.9f, initialScale.z), dodgeDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(dodgeDuration);

        isBusy = false;
        transform.localScale = initialScale;
        transform.localPosition = initialPosition;
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }

    private void TriggerAttack(AttackType type)
    {
        MatchManager.Instance.OnPlayerAttack(type);
    }

    public void TriggerJumpShotAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(JumpShotRoutine());
    }

    private System.Collections.IEnumerator JumpShotRoutine()
    {
        isBusy = true;
        transform.DOKill();
        transform.localScale = initialScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 0.9f, initialScale.y * 1.15f, initialScale.z), 0.12f));
        seq.Append(transform.DOLocalMoveY(initialPosition.y + 0.85f, 0.22f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOLocalMoveY(initialPosition.y, 0.18f).SetEase(Ease.InQuad));
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 1.1f, initialScale.y * 0.9f, initialScale.z), 0.08f));
        seq.Append(transform.DOScale(initialScale, 0.08f));

        yield return new WaitForSeconds(0.68f);

        transform.localScale = initialScale;
        transform.localPosition = initialPosition;
        isBusy = false;
    }

    public void TriggerSlamDunkAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(SlamDunkRoutine());
    }

    private System.Collections.IEnumerator SlamDunkRoutine()
    {
        isBusy = true;
        transform.DOKill();
        transform.localScale = initialScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 1.15f, initialScale.y * 0.85f, initialScale.z), 0.1f));
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 0.85f, initialScale.y * 1.25f, initialScale.z), 0.15f));
        seq.Join(transform.DOLocalMove(new Vector3(0, 1.6f, 0), 0.28f).SetEase(Ease.OutCubic));
        seq.Append(transform.DOLocalMove(initialPosition, 0.2f).SetEase(Ease.InExpo));
        seq.Append(transform.DOScale(new Vector3(initialScale.x * 1.25f, initialScale.y * 0.8f, initialScale.z), 0.1f));
        seq.Append(transform.DOScale(initialScale, 0.1f));

        yield return new WaitForSeconds(0.85f);

        transform.localScale = initialScale;
        transform.localPosition = initialPosition;
        isBusy = false;
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.SpawnDust(transform.position + new Vector3(0, -2.5f, 0), 8);
        }
    }

    public void TriggerHit()
    {
        if (hitFlash != null) hitFlash.Flash(0.08f);
        StopAllCoroutines();
        StartCoroutine(HitRoutine());
    }

    private System.Collections.IEnumerator HitRoutine()
    {
        transform.DOKill();
        transform.localScale = initialScale;
        transform.DOShakePosition(0.25f, new Vector3(0.2f, 0.2f, 0), 15);

        yield return new WaitForSeconds(0.26f);

        transform.localPosition = initialPosition;
        transform.localScale = initialScale;
    }
}
