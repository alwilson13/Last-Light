using UnityEngine;

/// Smoothly follows the player and looks ahead toward the mouse.
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Camera Position")]
    [SerializeField]
    private Vector3 followOffset =
        new Vector3(0f, 14f, -8f);

    [Tooltip("Fixed downward camera angle.")]
    [Range(10f, 89f)]
    [SerializeField] private float cameraAngle = 60f;

    [Header("Follow Smoothing")]
    [Tooltip("Higher values make the camera follow more slowly.")]
    [Min(0.01f)]
    [SerializeField] private float smoothTime = 0.3f;

    [Header("Mouse Look-Ahead")]
    [Tooltip("How much of the distance toward the cursor to use.")]
    [Range(0f, 1f)]
    [SerializeField] private float lookAheadStrength = 0.25f;

    [Tooltip("Maximum distance the camera can shift toward the cursor.")]
    [Min(0f)]
    [SerializeField] private float maxLookAheadDistance = 3f;

    private Camera attachedCamera;
    private Vector3 followVelocity;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null || attachedCamera == null)
        {
            Debug.LogWarning(
                "CameraController needs a player and must be " +
                "attached to the gameplay camera.",
                this
            );

            enabled = false;
            return;
        }

        // Keep the cursor available for aiming.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        transform.rotation =
            Quaternion.Euler(cameraAngle, 0f, 0f);

        // Start at the player rather than sliding in from elsewhere.
        transform.position = player.position + followOffset;
    }

    private void LateUpdate()
    {
        if (player == null || Time.deltaTime <= 0f)
            return;

        transform.rotation =
            Quaternion.Euler(cameraAngle, 0f, 0f);

        Vector3 lookAhead = GetMouseLookAhead();

        Vector3 targetPosition =
            player.position + followOffset + lookAhead;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            Mathf.Max(0.01f, smoothTime),
            Mathf.Infinity,
            Time.deltaTime
        );
    }

    private Vector3 GetMouseLookAhead()
    {
        // Use the same aiming plane as PlayerAiming.
        Plane aimingPlane = new Plane(
            Vector3.up,
            player.position
        );

        Ray mouseRay = attachedCamera.ScreenPointToRay(
            Input.mousePosition
        );

        if (!aimingPlane.Raycast(mouseRay, out float distance))
            return Vector3.zero;

        Vector3 mouseWorldPosition = mouseRay.GetPoint(distance);

        Vector3 offset =
            mouseWorldPosition - player.position;

        offset.y = 0f;
        offset *= lookAheadStrength;

        // Keep the player within a reasonable distance of view center.
        return Vector3.ClampMagnitude(
            offset,
            Mathf.Max(0f, maxLookAheadDistance)
        );
    }
}