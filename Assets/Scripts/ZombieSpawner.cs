using UnityEngine;
using System.Collections;
using TMPro; // WAJIB: Untuk mengatur UI TextMeshPro

public class ZombieSpawner : MonoBehaviour
{
    public static ZombieSpawner Instance; // Singleton agar gampang dipanggil dari script zombie

    [Header("Zombie Prefab & Loot")]
    public GameObject zombiePrefab;
    public GameObject crystalPrefab;        // Masukkan prefab Kristal Score/EXP Anda di sini

    [Header("Wave Settings")]
    public int currentWave = 1;
    public int zombiesToSpawn = 10;
    public int maxZombiesAlive = 3;
    public float spawnRate = 2f;
    public float waveBreakDuration = 30f;  // Jeda waktu antar wave (30 detik)

    [Header("Spawn Area")]
    public float spawnRadius = 5f;

    [Header("UI Canvas Settings")]
    public TextMeshProUGUI waveUIText;      // UI teks status Wave (misal di tengah)
    public TextMeshProUGUI zombieLeftText;  // UI teks sisa zombie yang hidup
    public TextMeshProUGUI scoreUIText;     // UI teks skor dengan label EXP
    public GameObject upgradePanelObject;   // UI Panel pop-up pilihan upgrade skill

    private int zombiesSpawned;
    private int zombiesKilled;
    
    private int totalScore = 0;             // Menyimpan total skor/EXP player
    private bool isWaveActive = false;      // Status penanda apakah wave sedang berjalan atau dalam waktu istirahat

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (upgradePanelObject != null) upgradePanelObject.SetActive(false); // Sembunyikan panel upgrade di awal
        UpdateScoreUI();
        
        // PENGAMAN 1: Langsung isi UI teks sisa zombie dengan angka target awal saat game di-start
        if (zombieLeftText != null)
        {
            zombieLeftText.text = $"Zombies: {zombiesToSpawn}";
        }
        
        StartCoroutine(GameStartCountdown());
    }

    // Jeda countdown 5 detik saat pertama kali game di-start agar player bisa bersiap
    private IEnumerator GameStartCountdown()
    {
        isWaveActive = false;
        for (int i = 5; i > 0; i--)
        {
            if (waveUIText != null) waveUIText.text = $"Mulai dalam: {i}";
            yield return new WaitForSeconds(1f);
        }
        
        StartNewWave();
    }

    void StartNewWave()
    {
        zombiesSpawned = 0;
        zombiesKilled = 0;
        isWaveActive = true;

        // PENGAMAN 2: Memaksa angka minimal jika terjadi kesalahan pembacaan variabel di frame pertama
        if (currentWave == 1 && zombiesToSpawn <= 0)
        {
            zombiesToSpawn = 10; 
        }

        // Tampilkan LOG & UI saat wave dimulai
        Debug.Log($"DeadWave Log: WAVE {currentWave} DIMULAI! Target: {zombiesToSpawn} Zombie.");
        if (waveUIText != null) waveUIText.text = $"WAVE {currentWave}";
        
        UpdateZombieLeftUI();
        Invoke(nameof(ClearWaveText), 3f);

        // Jalankan coroutine pembuat zombie
        StartCoroutine(SpawnZombieRoutine());
    }

    // Coroutine khusus untuk mengatur jeda kelahiran zombie secara berkala
    private IEnumerator SpawnZombieRoutine()
    {
        while (isWaveActive && zombiesSpawned < zombiesToSpawn)
        {
            // Ambil data jumlah zombie yang aktif saat ini di map
            GameObject[] activeZombies = GameObject.FindGameObjectsWithTag("Zombie");
            UpdateZombieLeftUI();

            // Hanya spawn jika zombie di map belum menyentuh batas maksimal
            if (activeZombies.Length < maxZombiesAlive)
            {
                SpawnZombie();
                zombiesSpawned++;
                UpdateZombieLeftUI();
            }

            // Beri jeda antar spawn sesuai spawnRate (2 detik)
            yield return new WaitForSeconds(spawnRate);
        }
    }

    void Update()
    {
        if (!isWaveActive) return;

        // Cek secara real-time apakah semua zombie sudah mati
        GameObject[] activeZombies = GameObject.FindGameObjectsWithTag("Zombie");
        
        // Logika hitungan kill
        zombiesKilled = zombiesToSpawn - activeZombies.Length;
        
        // Jaga agar teks UI sisa zombie selalu ter-update jika ada perubahan jumlah di map
        if (zombieLeftText != null)
        {
            int remainingInWave = Mathf.Max(0, zombiesToSpawn - zombiesSpawned + activeZombies.Length);
            zombieLeftText.text = $"Zombies: {remainingInWave}";
        }

        // PENGAMAN 3: Wave HANYA boleh selesai jika:
        // 1. Jumlah zombie yang lahir sudah mencapai target (zombiesSpawned >= zombiesToSpawn)
        // 2. DAN jumlah zombie yang tersisa hidup di map benar-benar sudah 0 (activeZombies.Length == 0)
        // 3. DAN target zombie-nya bukan angka 0 (zombiesToSpawn > 0)
        if (zombiesSpawned >= zombiesToSpawn && activeZombies.Length == 0 && zombiesToSpawn > 0)
        {
            isWaveActive = false; // Matikan status wave aktif
            StartCoroutine(WaveClearRoutine());
        }
    }

    void SpawnZombie()
    {
        Vector3 randomPosition = transform.position + new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            0,
            Random.Range(-spawnRadius, spawnRadius)
        );

        Instantiate(zombiePrefab, randomPosition, Quaternion.identity);
    }

    public void DropLootCrystal(Vector3 zombiePosition)
    {
        if (crystalPrefab != null)
        {
            Instantiate(crystalPrefab, zombiePosition + Vector3.up * 0.5f, Quaternion.identity);
        }
    }

    private IEnumerator WaveClearRoutine()
    {
        // Tampilkan LOG & UI saat wave selesai
        Debug.Log($"DeadWave Log: WAVE {currentWave} SELESAI!");
        if (waveUIText != null) waveUIText.text = "WAVE CLEAR!";
        yield return new WaitForSeconds(2f);
        if (waveUIText != null) waveUIText.text = "";

        // Tampilkan Panel Pilihan Upgrade & Freeze Game sementara agar player bisa memilih stat
        if (upgradePanelObject != null)
        {
            upgradePanelObject.SetActive(true);
            Time.timeScale = 0f; 
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true; 
        }
    }

    public void ChooseUpgradeDamage()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
            if (wManager != null)
            {
                wManager.fireDamage += 5;
                Debug.Log($"DeadWave Log: Damage Senjata Naik! Total: {wManager.fireDamage}");
            }
        }
        ResumeGameAfterUpgrade();
    }

    public void ChooseUpgradeFireRate()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
            if (wManager != null)
            {
                wManager.fireRate = Mathf.Max(0.08f, wManager.fireRate - 0.02f);
                Debug.Log($"DeadWave Log: Tembakan Makin Cepat! Jeda: {wManager.fireRate}");
            }
        }
        ResumeGameAfterUpgrade();
    }

    private void ResumeGameAfterUpgrade()
    {
        if (upgradePanelObject) upgradePanelObject.SetActive(false);

        Time.timeScale = 1f; // Jalankan kembali game
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;

        // Jalankan jeda istirahat 30 detik menuju wave berikutnya
        StartCoroutine(WaveBreakCountdownRoutine());
    }

    // Coroutine hitung mundur istirahat selama 30 detik untuk bersiap-siap
    private IEnumerator WaveBreakCountdownRoutine()
    {
        currentWave++;
        zombiesToSpawn += 5; // Tambah 5 target zombie untuk wave berikutnya

        for (int i = (int)waveBreakDuration; i > 0; i--)
        {
            if (waveUIText != null) waveUIText.text = $"Wave {currentWave} Muncul Dalam: {i}";
            yield return new WaitForSeconds(1f);
        }

        // Mulai wave baru setelah 30 detik selesai
        StartNewWave();
    }

    void NextWave() { } // Fungsi lama dinonaktifkan karena alurnya sudah digantikan oleh Coroutine

    private void ClearWaveText()
    {
        if (waveUIText != null) waveUIText.text = "";
    }

    public void AddScore(int amount)
    {
        totalScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreUIText != null) scoreUIText.text = $"EXP: {totalScore}";
    }

    private void UpdateZombieLeftUI()
    {
        if (zombieLeftText != null)
        {
            GameObject[] activeZombies = GameObject.FindGameObjectsWithTag("Zombie");
            int remainingInWave = Mathf.Max(0, zombiesToSpawn - zombiesSpawned + activeZombies.Length);
            zombieLeftText.text = $"Zombies: {remainingInWave}";
        }
    }
}