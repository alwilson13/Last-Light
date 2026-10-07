using System.Collections.Generic;
using UnityEngine;

/// Moves a 3D enemy between patrol points on the X/Z plane.
[RequireComponent(typeof(CharacterController))]
public class ArenaPatrolEnemy : MonoBehaviour
{
    public enum PatrolMode
    {
        Loop,
        PingPong,
        Random
    }

    [Header("Patrol Settings")]
    [Tooltip("If empty, finds objects tagged ArenaPatrolPoint.")]
    [SerializeField] private Transform[] patrolPoints;

    [SerializeField] private PatrolMode patrolMode = PatrolMode.Loop;

    [Min(0f)]
    [SerializeField] private float moveSpeed = 3f;

    [Min(0.01f)]
    [SerializeField] private float pointReachDistance = 0.25f;

    [SerializeField] private bool rotateTowardMovement = true;

    [Header("Arena Boundaries")]
    [SerializeField] private bool useArenaLimits = true;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;
    [SerializeField] private float minZ = -4.5f;
    [SerializeField] private float maxZ = 4.5f;

    private CharacterController characterController;
    private EnemyHealth enemyHealth;

    private int currentPointIndex;
    private int patrolDirection = 1;
    private bool isStunned;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        FindPatrolPointsIfNeeded();

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning(
                "ArenaPatrolEnemy has no patrol points.",
                this
            );

            enabled = false;
            return;
        }

        ClampStartingPosition();
    }

    private void Update()
    {
        if (isStunned || Time.deltaTime <= 0f)
            return;

        if (enemyHealth != null && enemyHealth.IsDead())
            return;

        MoveAlongPatrolPath();
    }

    private void FindPatrolPointsIfNeeded()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
            return;

        GameObject[] foundPoints =
            GameObject.FindGameObjectsWithTag("ArenaPatrolPoint");

        List<Transform> points = new List<Transform>();

        foreach (GameObject point in foundPoints)
        {
            points.Add(point.transform);
        }

        // Use names such as PatrolPoint_00, PatrolPoint_01, etc.
        points.Sort((a, b) =>
            string.CompareOrdinal(a.name, b.name));

        patrolPoints = points.ToArray();
    }

    private void MoveAlongPatrolPath()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        currentPointIndex = Mathf.Clamp(
            currentPointIndex,
            0,
            patrolPoints.Length - 1
        );

        Transform targetPoint = patrolPoints[currentPointIndex];

        if (targetPoint == null)
        {
            ChooseNextPoint();
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = targetPoint.position;

        // Patrol only horizontally.
        targetPosition.y = currentPosition.y;

        // Keep destinations reachable within the configured limits.
        targetPosition = ClampToArena(targetPosition);

        Vector3 directionToTarget =
            targetPosition - currentPosition;

        float distance = directionToTarget.magnitude;

        if (distance <= pointReachDistance)
        {
            ChooseNextPoint();
            return;
        }

        Vector3 moveDirection = directionToTarget.normalized;

        // Limit the step so fast movement cannot overshoot the point.
        float step = Mathf.Min(
            Mathf.Max(0f, moveSpeed) * Time.deltaTime,
            distance
        );

        Vector3 nextPosition = ClampToArena(
            currentPosition + moveDirection * step
        );

        characterController.Move(
            nextPosition - currentPosition
        );

        if (rotateTowardMovement)
        {
            transform.rotation = Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );
        }
    }

    private void ChooseNextPoint()
    {
        int count = patrolPoints.Length;

        if (count <= 1)
        {
            currentPointIndex = 0;
            return;
        }

        switch (patrolMode)
        {
            case PatrolMode.Loop:
                currentPointIndex =
                    (currentPointIndex + 1) % count;
                break;

            case PatrolMode.PingPong:
                if (currentPointIndex >= count - 1)
                    patrolDirection = -1;
                else if (currentPointIndex <= 0)
                    patrolDirection = 1;

                currentPointIndex += patrolDirection;
                break;

            case PatrolMode.Random:
                // Choose a different point.
                int offset = Random.Range(1, count);

                currentPointIndex =
                    (currentPointIndex + offset) % count;
                break;
        }
    }

    private Vector3 ClampToArena(Vector3 position)
    {
        if (!useArenaLimits)
            return position;

        position.x = Mathf.Clamp(
            position.x,
            Mathf.Min(minX, maxX),
            Mathf.Max(minX, maxX)
        );

        position.z = Mathf.Clamp(
            position.z,
            Mathf.Min(minZ, maxZ),
            Mathf.Max(minZ, maxZ)
        );

        return position;
    }

    private void ClampStartingPosition()
    {
        if (!useArenaLimits)
            return;

        // Temporarily disable the controller when repositioning.
        characterController.enabled = false;
        transform.position = ClampToArena(transform.position);
        characterController.enabled = true;
    }

    public void SetStunned(bool value)
    {
        isStunned = value;
    }
}