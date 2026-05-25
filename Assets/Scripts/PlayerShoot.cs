using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Weapon")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Fire Rate")]
    public float fireRate = 0.2f;

    private float nextFireTime;

    void Update()
    {
        // Tahan klik kiri mouse
        if (Input.GetMouseButton(0) &&
            Time.time >= nextFireTime)
        {
            Shoot();

            nextFireTime =
                Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Camera cam = Camera.main;

        Ray ray =
            cam.ScreenPointToRay(
                Input.mousePosition
            );

        RaycastHit hit;

        Vector3 targetPoint;

        // Target aim mouse
        if (Physics.Raycast(ray, out hit, 100f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
        }

        // Rotasi FirePoint ke target
        firePoint.LookAt(targetPoint);

        // Spawn bullet
        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );
    }
}