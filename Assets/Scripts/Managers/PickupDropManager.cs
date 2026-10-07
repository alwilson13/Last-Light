using UnityEngine;

/// Controls battery pickup drops from defeated enemies.
public class PickupDropManager : MonoBehaviour
{
    public static PickupDropManager Instance { get; private set; }

    [Header("Battery Drop Settings")]
    [SerializeField] private GameObject batteryPickupPrefab;

    [Tooltip("0.25 means a 25% chance per defeated enemy.")]
    [Range(0f, 1f)]
    [SerializeField] private float batteryDropChance = 0.25f;

    [Tooltip("Maximum drops per run. Zero means unlimited.")]
    [Min(0)]
    [SerializeField] private int maxBatteryDropsPerRun = 0;

    [Tooltip("Offset from the enemy's position.")]
    [SerializeField]
    private Vector3 pickupSpawnOffset =
        new Vector3(0f, 0.25f, 0f);

    private int batteryDropsCreated;

    public int BatteryDropsCreated => batteryDropsCreated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TryDropBatteryPickup(Vector3 dropPosition)
    {
        if (!isActiveAndEnabled || batteryPickupPrefab == null)
            return;

        if (maxBatteryDropsPerRun > 0 &&
            batteryDropsCreated >= maxBatteryDropsPerRun)
        {
            return;
        }

        if (Random.value >= batteryDropChance)
            return;

        Instantiate(
            batteryPickupPrefab,
            dropPosition + pickupSpawnOffset,
            Quaternion.identity
        );

        batteryDropsCreated++;
    }

    /// Call when starting a new run without reloading the scene.
    public void ResetDrops()
    {
        batteryDropsCreated = 0;
    }
}