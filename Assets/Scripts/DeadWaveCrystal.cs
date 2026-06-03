using UnityEngine;

public class DeadWaveCrystal : MonoBehaviour
{
    [Header("Pengaturan EXP/Score")]
    public int scoreValue = 100;       // Jumlah skor yang didapat
    public float magnetRadius = 5f;    // Jarak aman kristal mulai menyedot player
    public float flySpeed = 8f;        // Kecepatan terbang kristal menuju player

    private Transform playerTransform;
    private bool isFlying = false;

    private void Start()
    {
        // Cari objek player secara otomatis lewat Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
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

            // Jika sudah sangat dekat dengan player, hancurkan dan tambah skor
            if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
            {
                CollectCrystal();
            }
        }
    }

    private void CollectCrystal()
    {
        // PERBAIKAN: Diarahkan ke ZombieSpawner, bukan DeadWaveGameManager
        if (ZombieSpawner.Instance != null)
        {
            // Tambah skor ke ZombieSpawner yang sudah kita upgrade kemarin
            ZombieSpawner.Instance.AddScore(scoreValue);
        }

        // Hancurkan objek kristal dari map arena
        Destroy(gameObject);
    }
}