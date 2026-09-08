using UnityEngine;
using TMPro;
using DG.Tweening;

public class NeonPulse : MonoBehaviour
{
    [SerializeField] private RectTransform shadowTransform;
    [SerializeField] private float pulseScale = 1.045f;
    [SerializeField] private float pulseDuration = 0.85f;
    [SerializeField] private Color glowColorA = new Color(0.03f, 0.01f, 0.07f, 1f);
    [SerializeField] private Color glowColorB = new Color(0.05f, 0.02f, 0.10f, 1f);

    private Vector3 initialScale;
    private Vector2 initialShadowPos;
    private TextMeshProUGUI shadowTmp;
    private Tween scaleTween;
    private Tween shadowColorTween;

    private void Awake()
    {
        initialScale = transform.localScale;
        if (shadowTransform != null)
        {
            initialShadowPos = shadowTransform.anchoredPosition;
            shadowTmp = shadowTransform.GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        StartPulse();
    }

    private void OnDisable()
    {
        StopPulse();
    }

    private void StartPulse()
    {
        StopPulse();

        transform.localScale = initialScale;
        scaleTween = transform.DOScale(initialScale * pulseScale, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        if (shadowTransform != null)
        {
            shadowTransform.anchoredPosition = initialShadowPos;

            if (shadowTmp != null)
            {
                shadowTmp.color = glowColorA;
                shadowColorTween = shadowTmp.DOColor(glowColorB, pulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
    }

    private void StopPulse()
    {
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
        if (shadowColorTween != null && shadowColorTween.IsActive()) shadowColorTween.Kill();

        transform.localScale = initialScale;
        if (shadowTransform != null)
        {
            shadowTransform.anchoredPosition = initialShadowPos;
            if (shadowTmp != null) shadowTmp.color = glowColorA;
        }
    }
}
