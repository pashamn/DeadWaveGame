using UnityEngine;

public class DeadWaveCrystal : MonoBehaviour
{
    [Header("Pengaturan EXP/Score")]
    public int scoreValue = 100;       // Jumlah skor yang didapat
    public float magnetRadius = 5f;    // Jarak aman kristal mulai menyedot player
    public float flySpeed = 8f;        // Kecepatan terbang kristal menuju player

    [Header("Pengaturan Tambahan Peluru (RANDOM)")]
    [Tooltip("Jumlah peluru MINIMAL yang bisa didapatkan")]
    public int minAmmoToGrant = 10;    
    
    [Tooltip("Jumlah peluru MAKSIMAL yang bisa didapatkan")]
    public int maxAmmoToGrant = 30;    

    private Transform playerTransform;
    private PlayerWeaponCameraManager playerWeaponManager; // Referensi komponen senjata player
    private bool isFlying = false;

    private void Start()
    {
        // Cari objek player secara otomatis lewat Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerWeaponManager = player.GetComponent<PlayerWeaponCameraManager>();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Hitung jarak antara kristal dan player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Jika player masuk radius magnet, aktifkan mode terbang
        if (distanceToPlayer <= magnetRadius)
        {
            isFlying = true;
        }

        // Proses terbang menyedot ke tubuh player
        if (isFlying)
        {
            // Terbang ke arah dada player (ditambah sedikit posisi Y-nya)
            Vector3 targetPosition = playerTransform.position + Vector3.up * 1f;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, flySpeed * Time.deltaTime);

            // Jika sudah sangat dekat dengan player, hancurkan dan tambah skor + peluru
            if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
            {
                CollectCrystal();
            }
        }
    }

    private void CollectCrystal()
    {
        // 1. Tambah skor ke ZombieSpawner
        if (ZombieSpawner.Instance != null)
        {
            ZombieSpawner.Instance.AddScore(scoreValue);
        }

        // 2. Tambah amunisi cadangan secara ACAK ke tas Player
        if (playerWeaponManager != null)
        {
            // Trik Random: mengacak nilai dari minAmmo hingga maxAmmo
            // (maxAmmoToGrant + 1) digunakan karena Random.Range untuk integer tidak menyertakan angka batas atasnya
            int randomAmmoGained = Random.Range(minAmmoToGrant, maxAmmoToGrant + 1);

            playerWeaponManager.AddCarriableAmmo(randomAmmoGained);
            Debug.Log($"DeadWave Log: Kristal Tersedot! +{scoreValue} EXP & +{randomAmmoGained} Peluru (Hasil Gacha).");
        }

        // Hancurkan objek kristal dari map arena
        Destroy(gameObject);
    }
}