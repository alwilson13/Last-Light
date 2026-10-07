using UnityEngine;

/// Recharges the player's flashlight when collected.
public class BatteryPickup : MonoBehaviour
{
    [Header("Battery Pickup Settings")]
    [Min(1f)]
    [SerializeField] private float rechargeAmount = 25f;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Allows collection if the battery was full on arrival,
        // then drains while the player remains nearby.
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (collected || Time.timeScale <= 0f)
            return;

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead())
            return;

        FlashlightBattery battery =
            playerHealth.GetComponent<FlashlightBattery>();

        if (battery == null)
            return;

        // Reserve the pickup before Recharge raises its events.
        collected = true;

        float restored = battery.Recharge(rechargeAmount);

        if (restored <= 0f)
        {
            // Leave the pickup available when the battery is full.
            collected = false;
            return;
        }

        Destroy(gameObject);
    }
}