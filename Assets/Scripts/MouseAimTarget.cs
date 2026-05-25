using UnityEngine;

public class MouseAimTarget : MonoBehaviour
{
    public LayerMask groundLayer;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
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
            transform.position = hit.point;
        }
    }
}