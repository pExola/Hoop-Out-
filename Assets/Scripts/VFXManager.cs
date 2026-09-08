using UnityEngine;
using DG.Tweening;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] private TMPro.TMP_FontAsset fontAsset;
    [SerializeField] private SpeedLinesEffect speedLines;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (speedLines == null) speedLines = Object.FindFirstObjectByType<SpeedLinesEffect>(FindObjectsInactive.Include);
    }

    public void TriggerSpeedLines(float duration = 0.5f)
    {
        if (speedLines == null) speedLines = Object.FindFirstObjectByType<SpeedLinesEffect>(FindObjectsInactive.Include);
        if (speedLines != null) speedLines.Trigger(duration);
    }

    public void SpawnDust(Vector3 position, int count = 6)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dust.transform.position = position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.1f), 0);
            dust.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);

            var col = dust.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = dust.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Sprites/Default"));
                mr.material.color = new Color(0.85f, 0.85f, 0.95f, 0.7f);
            }

            Vector3 drift = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 0.5f), 0);
            dust.transform.DOMove(dust.transform.position + drift, 0.35f).SetEase(Ease.OutQuad);
            dust.transform.DOScale(0f, 0.35f).SetEase(Ease.InQuad).OnComplete(() => Destroy(dust));
        }
    }

    public void SpawnImpactSparks(Vector3 position, Color sparkColor, int count = 12)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * Random.Range(0.12f, 0.25f);
            spark.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            var col = spark.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = spark.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Sprites/Default"));
                mr.material.color = sparkColor;
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(0.8f, 2.0f);
            Vector3 targetPos = position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0);

            spark.transform.DOMove(targetPos, 0.28f).SetEase(Ease.OutExpo);
            spark.transform.DOScale(0f, 0.28f).SetEase(Ease.InQuad).OnComplete(() => Destroy(spark));
        }
    }

    public void SpawnShockwave(Vector3 position, Color ringColor, float maxRadius = 2.8f)
    {
        GameObject ringObj = new GameObject("ShockwaveRing");
        ringObj.transform.position = position;
        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 36;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = ringColor;
        lr.endColor = new Color(ringColor.r, ringColor.g, ringColor.b, 0.1f);

        for (int i = 0; i < 36; i++)
        {
            float rad = (i / 36f) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * 0.2f, Mathf.Sin(rad) * 0.2f, 0));
        }

        ringObj.transform.DOScale(Vector3.one * maxRadius, 0.38f).SetEase(Ease.OutQuad);
        DOTween.To(() => lr.startColor.a, a =>
        {
            Color c = ringColor;
            c.a = a;
            lr.startColor = c;
        }, 0f, 0.38f).OnComplete(() => Destroy(ringObj));
    }

    public void ShowFloatingText(string message, Color color, Vector3 worldPos, float scale = 1f)
    {
        GameObject textObj = new GameObject("FloatingTextInstance");
        var tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.fontSize = 1.6f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.sortingOrder = 50;
        if (tmp.fontMaterial != null)
        {
            tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmp.fontMaterial.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.22f);
            tmp.fontMaterial.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, new Color(0.02f, 0.01f, 0.05f, 1f));
        }

        var ft = textObj.AddComponent<FloatingText>();
        ft.Show(message, color, worldPos, scale);
    }
}
