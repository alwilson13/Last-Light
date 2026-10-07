using UnityEngine;

/// Tracks the player, lunges in a locked direction,
/// then rests before repeating.
[RequireComponent(typeof(CharacterController))]
public class ChargerMovement : MonoBehaviour
{
    private enum MovementState
    {
        Tracking,
        Charging,
        Cooldown
    }

    [Header("Charge Settings")]
    [Min(0f)]
    [SerializeField] private float trackingSpeed = 1.5f;

    [Min(0f)]
    [SerializeField] private float chargeSpeed = 8f;

    [Min(0.01f)]
    [SerializeField] private float trackingTime = 1.2f;

    [Min(0.01f)]
    [SerializeField] private float chargeTime = 0.45f;

    [Min(0.01f)]
    [SerializeField] private float cooldownTime = 1f;

    [Header("Target Settings")]
    [SerializeField] private Transform playerTarget;

    private CharacterController characterController;
    private EnemyHealth enemyHealth;
    private PlayerHealth targetHealth;

    private MovementState currentState;
    private Vector3 chargeDirection;
    private float stateTimer;
    private bool isStunned;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        isStunned = false;
        BeginTracking();
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }

        if (playerTarget != null)
        {
            targetHealth =
                playerTarget.GetComponentInParent<PlayerHealth>();
        }
        else
        {
            Debug.LogWarning(
                "ChargerMovement could not find the player.",
                this
            );
        }
    }

    private void Update()
    {
        if (isStunned || playerTarget == null)
            return;

        if (enemyHealth != null && enemyHealth.IsDead())
            return;

        if (targetHealth != null && targetHealth.IsDead())
            return;

        // Stops behavior updates when time is paused.
        if (Time.deltaTime <= 0f)
            return;

        switch (currentState)
        {
            case MovementState.Tracking:
                TrackPlayer();
                break;

            case MovementState.Charging:
                Charge();
                break;

            case MovementState.Cooldown:
                Cooldown();
                break;
        }
    }

    private Vector3 GetDirectionToPlayer()
    {
        Vector3 direction =
            playerTarget.position - transform.position;

        direction.y = 0f;

        return direction.normalized;
    }

    private void TrackPlayer()
    {
        Vector3 direction = GetDirectionToPlayer();

        Move(direction, trackingSpeed);
        RotateTowardDirection(direction);

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            // Lock once: the charge does not follow mouse/player movement.
            chargeDirection = GetDirectionToPlayer();

            RotateTowardDirection(chargeDirection);

            currentState = MovementState.Charging;
            stateTimer = Mathf.Max(0.01f, chargeTime);
        }
    }

    private void Charge()
    {
        Move(chargeDirection, chargeSpeed);

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            currentState = MovementState.Cooldown;
            stateTimer = Mathf.Max(0.01f, cooldownTime);
        }
    }

    private void Cooldown()
    {
        // No Move call means the enemy remains stationary.
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            BeginTracking();
        }
    }

    private void BeginTracking()
    {
        currentState = MovementState.Tracking;
        stateTimer = Mathf.Max(0.01f, trackingTime);
        chargeDirection = Vector3.zero;
    }

    private void Move(Vector3 direction, float speed)
    {
        characterController.Move(
            direction * speed * Time.deltaTime
        );
    }

    private void RotateTowardDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(
            direction,
            Vector3.up
        );
    }

    /// Called by the enemy stun system.
    public void SetStunned(bool value)
    {
        if (isStunned == value)
            return;

        isStunned = value;

        // Cancel the current charge and restart tracking after recovery.
        BeginTracking();
    }
}