using UnityEngine;

/// Animates a ground warning before an enemy spawns.
/// EnemySpawner controls spawning and indicator destruction.
public class SpawnIndicator : MonoBehaviour
{
    [Header("Visual Reference")]
    [Tooltip("Child containing the flat warning mesh.")]
    [SerializeField] private Transform visualTransform;

    [Header("Pulse Settings")]
    [Min(0f)]
    [SerializeField] private float pulseSpeed = 8f;

    [Range(0f, 0.9f)]
    [SerializeField] private float pulseAmount = 0.2f;

    [Min(0.01f)]
    [SerializeField] private float finalScaleMultiplier = 1.4f;

    private Vector3 startScale;
    private float animationDuration;
    private float elapsed;
    private bool isAnimating;

    private void Awake()
    {
        if (visualTransform == null)
            visualTransform = transform;

        startScale = visualTransform.localScale;
    }

    public void StartIndicator(float duration)
    {
        animationDuration = Mathf.Max(0.01f, duration);
        elapsed = 0f;
        isAnimating = true;

        visualTransform.localScale = startScale;
    }

    private void Update()
    {
        if (!isAnimating || visualTransform == null)
            return;

        elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(
            elapsed / animationDuration
        );

        float pulse =
            1f + Mathf.Sin(elapsed * pulseSpeed) * pulseAmount;

        float growth = Mathf.Lerp(
            1f,
            finalScaleMultiplier,
            progress
        );

        float scaleMultiplier = pulse * growth;

        // Expand across the floor without changing thickness.
        visualTransform.localScale = new Vector3(
            startScale.x * scaleMultiplier,
            startScale.y,
            startScale.z * scaleMultiplier
        );

        if (progress >= 1f)
            isAnimating = false;
    }
}