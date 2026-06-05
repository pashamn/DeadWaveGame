using UnityEngine;

public class DeadWaveAmmoPickup : MonoBehaviour
{
    public enum PickupType { AmmoOnly, MedkitOnly }

    [Header("Tipe Item")]
    public PickupType jenisItem = PickupType.AmmoOnly;

    [Header("Pengaturan Amunisi")]
    public int ammoAmount = 30; 

    [Header("Pengaturan Medkit")]
    public int healAmount = 25; 

    [Header("Efek Visual (Opsional)")]
    public GameObject pickupEffectPrefab; 
    public float rotationSpeed = 40f;     

    private void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWeaponCameraManager weaponManager = other.GetComponent<PlayerWeaponCameraManager>();
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            if (jenisItem == PickupType.AmmoOnly && weaponManager != null)
            {
                weaponManager.AddCarriableAmmo(ammoAmount);
                ExecutePickupEffect();
            }
            else if (jenisItem == PickupType.MedkitOnly && playerHealth != null && !playerHealth.IsDead)
            {
                if (playerHealth.currentHealth >= playerHealth.maxHealth) return;

                int newHealth = playerHealth.currentHealth + healAmount;
                playerHealth.SetHealthFromSpawner(newHealth);
                ExecutePickupEffect();
            }
        }
    }

    private void ExecutePickupEffect()
    {
        if (pickupEffectPrefab != null)
        {
            GameObject fx = Instantiate(pickupEffectPrefab, transform.position, transform.rotation);
            Destroy(fx, 1f);
        }
        Destroy(gameObject);
    }
}