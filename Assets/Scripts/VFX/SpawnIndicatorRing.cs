using UnityEngine;

/// Creates and animates a ground ring before an enemy spawns.
[RequireComponent(typeof(LineRenderer))]
public class SpawnIndicatorRing : MonoBehaviour
{
    [Header("Ring Shape")]
    [Min(0.01f)]
    [SerializeField] private float radius = 0.6f;

    [Min(3)]
    [SerializeField] private int segments = 64;

    [Tooltip("Local height above the indicator's spawn position.")]
    [SerializeField] private float groundOffset = 0.03f;

    [Header("Ring Appearance")]
    [SerializeField] private Color ringColor = Color.red;

    [Header("Animation Settings")]
    [Min(0f)]
    [SerializeField] private float pulseSpeed = 8f;

    [Range(0f, 0.9f)]
    [SerializeField] private float pulseAmount = 0.15f;

    [Min(0.01f)]
    [SerializeField] private float finalScaleMultiplier = 1.35f;

    [Min(0.001f)]
    [SerializeField] private float startWidth = 0.08f;

    [Min(0.001f)]
    [SerializeField] private float endWidth = 0.02f;

    private LineRenderer lineRenderer;
    private float duration;
    private float elapsed;
    private bool isAnimating;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        segments = Mathf.Max(3, segments);

        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = segments;

        // Ribbon faces the object's local +Z.
        // Rotate it so the ribbon faces upward instead.
        lineRenderer.alignment = LineAlignment.TransformZ;
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        UpdateRing(1f);
        UpdateAppearance(0f);
    }

    public void StartIndicator(float indicatorDuration)
    {
        duration = Mathf.Max(0.01f, indicatorDuration);
        elapsed = 0f;
        isAnimating = true;

        UpdateRing(1f);
        UpdateAppearance(0f);
    }

    private void Update()
    {
        if (!isAnimating)
            return;

        elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsed / duration);

        float pulse =
            1f + Mathf.Sin(elapsed * pulseSpeed) * pulseAmount;

        float growth = Mathf.Lerp(
            1f,
            finalScaleMultiplier,
            progress
        );

        UpdateRing(pulse * growth);
        UpdateAppearance(progress);

        if (progress >= 1f)
            isAnimating = false;
    }

    private void UpdateRing(float multiplier)
    {
        float currentRadius = Mathf.Max(0.01f, radius) * multiplier;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;

            // Local XY becomes world XZ after the 90-degree rotation.
            // Local -Z becomes upward.
            lineRenderer.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    Mathf.Sin(angle) * currentRadius,
                    -groundOffset
                )
            );
        }
    }

    private void UpdateAppearance(float progress)
    {
        float width = Mathf.Lerp(startWidth, endWidth, progress);

        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        Color color = ringColor;
        color.a *= 1f - progress;

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}