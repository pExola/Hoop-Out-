using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshPro tmp;

    private void Awake()
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
    }

    public void Show(string message, Color color, Vector3 worldPos, float scale = 1f)
    {
        worldPos.x = Mathf.Clamp(worldPos.x, -0.5f, 0.5f);
        transform.position = worldPos;
        transform.localScale = Vector3.one * (0.35f * scale);
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-3.5f, 3.5f));

        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = message;
            tmp.color = color;
            tmp.alpha = 1f;
            tmp.fontStyle = FontStyles.Bold;
        }

        transform.DOMoveY(worldPos.y + 1.1f, 0.85f).SetEase(Ease.OutCubic);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(Vector3.one * (1.1f * scale), 0.16f).SetEase(Ease.OutBack));
        seq.Append(transform.DOScale(Vector3.one * (0.95f * scale), 0.5f));

        if (tmp != null)
        {
            DOTween.To(() => tmp.alpha, x => tmp.alpha = x, 0f, 0.35f)
                .SetDelay(0.5f)
                .OnComplete(() =>
                {
                    transform.DOKill();
                    Destroy(gameObject);
                });
        }
        else
        {
            Destroy(gameObject, 0.85f);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
