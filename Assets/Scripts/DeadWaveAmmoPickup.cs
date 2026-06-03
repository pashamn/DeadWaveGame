using UnityEngine;

public class DeadWaveAmmoPickup : MonoBehaviour
{
    [Header("Pengaturan Amunisi")]
    public int ammoAmount = 30; // Jumlah peluru yang didapat saat kotak ini diambil

    [Header("Efek Visual (Opsional)")]
    public GameObject pickupEffectPrefab; // Efek partikel saat kotak diambil

    private void OnTriggerEnter(Collider other)
    {
        // Cek apakah objek yang menabrak kotak ini adalah Player
        if (other.CompareTag("Player"))
        {
            // Cari script manager senjata di tubuh player
            PlayerWeaponCameraManager weaponManager = other.GetComponent<PlayerWeaponCameraManager>();

            if (weaponManager != null)
            {
                // Eksekusi fungsi penambah peluru cadangan yang sudah kita buat di script player
                weaponManager.AddCarriableAmmo(ammoAmount);

                // Munculkan efek partikel pickup jika ada
                if (pickupEffectPrefab != null)
                {
                    GameObject fx = Instantiate(pickupEffectPrefab, transform.position, transform.rotation);
                    Destroy(fx, 1f);
                }

                // Hancurkan kotak peluru di tanah agar tidak bisa diambil berkali-kali
                Destroy(gameObject);
            }
        }
    }
}