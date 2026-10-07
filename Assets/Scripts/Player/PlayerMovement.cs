using UnityEngine;

/// Handles top-down movement independently of player aiming.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Used when no PlayerStats component is found.")]
    [Min(0f)]
    [SerializeField] private float fallbackMoveSpeed = 6f;

    private CharacterController characterController;
    private PlayerStats playerStats;

    private Vector3 moveDirection;

    // Reads the latest speed so stat changes apply immediately.
    public float CurrentMoveSpeed => Mathf.Max(
        0f,
        playerStats != null
            ? playerStats.MovementSpeed
            : fallbackMoveSpeed
    );

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerStats = GetComponentInParent<PlayerStats>();
    }

    private void Update()
    {
        ReadMovementInput();
        MovePlayer();
    }

    private void ReadMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // World-space movement: aiming does not change WASD directions.
        moveDirection = new Vector3(horizontal, 0f, vertical);

        // Prevent diagonal movement from being faster.
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
    }

    private void MovePlayer()
    {
        characterController.Move(
            moveDirection * CurrentMoveSpeed * Time.deltaTime
        );
    }

    /// Adds a permanent movement-speed increase for the current run.
    public void IncreaseMoveSpeed(float amount)
    {
        if (amount <= 0f)
            return;

        if (playerStats == null)
        {
            fallbackMoveSpeed += amount;
            return;
        }

        StatModifier speedModifier = new StatModifier
        {
            statType = StatType.MovementSpeed,
            modifierType = StatModifierType.Flat,
            value = amount
        };

        playerStats.AddModifier(this, speedModifier);
    }

    public Vector3 GetMoveInput()
    {
        return moveDirection;
    }

    public float GetMoveSpeed()
    {
        return CurrentMoveSpeed;
    }
}