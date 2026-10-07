using UnityEngine;

/// Handles mouse aiming for Last Light.
/// Rotates the player on the X/Z plane.
public class PlayerAiming : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("The camera used to aim toward the mouse cursor.")]
    [SerializeField] private Camera mainCamera;

    [Header("Aiming Settings")]
    [Tooltip("Adjust if the model does not face local +Z.")]
    [SerializeField] private float rotationOffset = 0f;

    private void Awake()
    {
        // Use the camera tagged MainCamera if none is assigned.
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        AimAtMouse();
    }

    private void AimAtMouse()
    {
        if (mainCamera == null)
            return;

        // Cast from the camera through the mouse cursor.
        Ray mouseRay = mainCamera.ScreenPointToRay(
            Input.mousePosition
        );

        // Aim across a horizontal plane at the player's height.
        Plane aimingPlane = new Plane(
            Vector3.up,
            transform.position
        );

        if (!aimingPlane.Raycast(mouseRay, out float distance))
            return;

        Vector3 mouseWorldPosition = mouseRay.GetPoint(distance);

        Vector3 aimDirection =
            mouseWorldPosition - transform.position;

        // Keep rotation horizontal.
        aimDirection.y = 0f;

        // Avoid rotating when the cursor is directly over the player.
        if (aimDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(
            aimDirection,
            Vector3.up
        );

        transform.rotation =
            targetRotation *
            Quaternion.Euler(0f, rotationOffset, 0f);
    }
}