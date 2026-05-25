using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    [Header("Aim")]
    public LayerMask groundLayer;

    public float rotationSpeed = 10f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Aim();
    }

    void Aim()
    {
        Ray ray =
            mainCamera.ScreenPointToRay(
                Input.mousePosition
            );

        RaycastHit hit;

        if (Physics.Raycast(
            ray,
            out hit,
            100f,
            groundLayer))
        {
            Vector3 direction =
                hit.point - transform.position;

            direction.y = 0f;

            // Hindari rotation error
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                // Rotate smooth
                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
            }
        }
    }
}