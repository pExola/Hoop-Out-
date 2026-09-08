using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class HoopCamera : MonoBehaviour
{
    public static HoopCamera Instance { get; private set; }

    private const float TargetWidth = 4.5f;
    private const float TargetHeight = 8f;

    [SerializeField] private Transform backgroundTransform;

    private Camera cam;
    private int lastWidth;
    private int lastHeight;
    private Tween shakeTween;
    private Vector3 basePosition = new Vector3(0f, 0f, -10f);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        cam = GetComponent<Camera>();
        Adjust();
    }

    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            Adjust();
        }
    }

    public void Adjust()
    {
        if (cam == null) cam = GetComponent<Camera>();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        if (lastHeight <= 0) return;

        float screenAspect = (float)lastWidth / lastHeight;
        float targetAspect = TargetWidth / TargetHeight;

        if (screenAspect < targetAspect)
        {
            cam.orthographicSize = (TargetWidth / screenAspect) * 0.5f;
        }
        else
        {
            cam.orthographicSize = TargetHeight * 0.5f;
        }

        transform.position = basePosition;

        if (backgroundTransform != null)
        {
            backgroundTransform.position = Vector3.zero;
            backgroundTransform.localScale = Vector3.one;
        }
    }

    public void Shake(float duration, float strength, int vibrato = 20)
    {
        if (shakeTween != null && shakeTween.IsActive()) shakeTween.Kill();
        transform.position = basePosition;
        shakeTween = transform.DOShakePosition(duration, strength, vibrato)
            .OnComplete(() => transform.position = basePosition);
    }
}
