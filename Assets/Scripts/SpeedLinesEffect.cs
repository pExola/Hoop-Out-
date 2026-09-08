using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SpeedLinesEffect : MonoBehaviour
{
    [SerializeField] private int lineCount = 28;
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.85f);

    private CanvasGroup canvasGroup;
    private RectTransform[] lines;
    private Tween fadeTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        BuildLines();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void BuildLines()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        lines = new RectTransform[lineCount];
        float angleStep = 360f / lineCount;

        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject($"Line_{i}", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(transform, false);

            Image img = lineObj.GetComponent<Image>();
            img.color = (i % 3 == 0) ? new Color(0f, 0.95f, 1f, 0.95f) : lineColor;
            img.raycastTarget = false;

            RectTransform rt = lineObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);

            float angle = i * angleStep;
            rt.localRotation = Quaternion.Euler(0, 0, angle);

            float length = Random.Range(200f, 480f);
            float width = Random.Range(3f, 7f);
            rt.sizeDelta = new Vector2(width, length);
            rt.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad) * 110f, 130f - Mathf.Cos(angle * Mathf.Deg2Rad) * 110f);

            lines[i] = rt;
        }
    }

    public void Trigger(float duration = 0.5f)
    {
        if (fadeTween != null && fadeTween.IsActive()) fadeTween.Kill();

        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        if (lines != null)
        {
            float angleStep = 360f / lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != null)
                {
                    float angle = i * angleStep;
                    float innerDist = Random.Range(90f, 130f);
                    float length = Random.Range(220f, 500f);
                    float width = Random.Range(3.5f, 8f);
                    lines[i].sizeDelta = new Vector2(width, length);
                    lines[i].anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad) * innerDist, 130f - Mathf.Cos(angle * Mathf.Deg2Rad) * innerDist);
                }
            }
        }

        fadeTween = canvasGroup.DOFade(0f, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
