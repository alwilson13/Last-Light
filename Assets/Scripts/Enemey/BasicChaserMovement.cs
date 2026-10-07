using UnityEngine;

/// Moves a 3D enemy directly toward the player on the X/Z plane.
[RequireComponent(typeof(CharacterController))]
public class BasicChaserMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 2.5f;

    [SerializeField] private bool rotateTowardMovement = true;

    [Header("Target Settings")]
    [Tooltip("If empty, finds the object tagged Player.")]
    [SerializeField] private Transform playerTarget;

    private CharacterController characterController;
    private EnemyHealth enemyHealth;
    private PlayerHealth playerHealth;

    private bool isStunned;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        isStunned = false;
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
            playerHealth =
                playerTarget.GetComponentInParent<PlayerHealth>();
        }
        else
        {
            Debug.LogWarning(
                "BasicChaserMovement could not find the player.",
                this
            );
        }
    }

    private void Update()
    {
        if (isStunned || playerTarget == null)
            return;

        if (Time.deltaTime <= 0f)
            return;

        if (enemyHealth != null && enemyHealth.IsDead())
            return;

        if (playerHealth != null && playerHealth.IsDead())
            return;

        MoveTowardPlayer();
    }

    private void MoveTowardPlayer()
    {
        Vector3 direction =
            playerTarget.position - transform.position;

        // Ignore differences in height.
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance < 0.001f)
            return;

        direction /= distance;

        // Prevent movement from overshooting the target.
        float step = Mathf.Min(
            Mathf.Max(0f, moveSpeed) * Time.deltaTime,
            distance
        );

        characterController.Move(direction * step);

        if (rotateTowardMovement)
        {
            transform.rotation = Quaternion.LookRotation(
                direction,
                Vector3.up
            );
        }
    }

    /// Called by the enemy stun system.
    public void SetStunned(bool value)
    {
        isStunned = value;
    }
}