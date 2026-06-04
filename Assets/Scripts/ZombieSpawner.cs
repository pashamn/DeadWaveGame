using UnityEngine;
using System.Collections;
using System.Collections.Generic; // WAJIB: Untuk menggunakan List dalam pengacakan
using TMPro;

public class ZombieSpawner : MonoBehaviour
{
    public static ZombieSpawner Instance;

    [Header("Zombie Prefab & Loot")]
    public GameObject zombiePrefab;
    public GameObject crystalPrefab;        

    [Header("Wave Settings")]
    public int currentWave = 1;
    public int zombiesToSpawn = 5;          
    public int maxZombiesAlive = 3;
    public float spawnRate = 2f;
    public float waveBreakDuration = 30f;  

    [Header("Spawn Area")]
    public float spawnRadius = 5f;

    [Header("UI Canvas Settings")]
    public TextMeshProUGUI waveUIText;       
    public TextMeshProUGUI countdownUIText;  
    public TextMeshProUGUI zombieLeftText;  
    public TextMeshProUGUI scoreUIText;     
    public GameObject upgradePanelObject;   // Panel UI Utama Upgrade

    [Header("UI Button Texts (Gacha Setup)")]
    // Seret komponen TextMeshProUGUI dari Tombol 1, Tombol 2, dan Tombol 3 ke slot ini
    public TextMeshProUGUI buttonTextOption1;
    public TextMeshProUGUI buttonTextOption2;
    public TextMeshProUGUI buttonTextOption3;
    public TextMeshProUGUI upgradeTitleUIText; // Teks Judul Panel (misal: "Pilih 1 Upgrade!")

    private int zombiesSpawned;
    private int zombiesKilled;
    private int totalScore = 0;             
    private bool isWaveActive = false;      

    // Sistem Aturan Jumlah Pilihan per Wave
    private int upgradePointsAvailable = 0; 
    private List<int> currentRolledUpgrades = new List<int>(); // Menyimpan hasil kocokan 3 upgrade aktif

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (upgradePanelObject != null) upgradePanelObject.SetActive(false); 
        UpdateScoreUI();
        UpdateWaveUI(); 
        
        int isContinuing = PlayerPrefs.GetInt("IsContinuingGame", 0);

        if (isContinuing == 1)
        {
            LoadGameProgress(); 
            PlayerPrefs.SetInt("IsContinuingGame", 0); 
            PlayerPrefs.Save();
        }
        else
        {
            currentWave = 1;
            zombiesToSpawn = 5;
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.SetHealthFromSpawner(pHealth.maxHealth);
                }
            }
        }

        if (zombieLeftText != null)
        {
            zombieLeftText.text = $"Zombies: {zombiesToSpawn}";
        }
        
        StartCoroutine(GameStartCountdown());
    }

    private IEnumerator GameStartCountdown()
    {
        isWaveActive = false;
        for (int i = 5; i > 0; i--)
        {
            if (countdownUIText != null) countdownUIText.text = $"Mulai dalam: {i}";
            yield return new WaitForSeconds(1f);
        }
        if (countdownUIText != null) countdownUIText.text = "";
        StartNewWave();
    }

    void StartNewWave()
    {
        zombiesSpawned = 0;
        zombiesKilled = 0;
        isWaveActive = true;

        if (currentWave == 1 && zombiesToSpawn <= 0)
        {
            zombiesToSpawn = 5; 
        }

        Debug.Log($"DeadWave Log: WAVE {currentWave} DIMULAI! Target: {zombiesToSpawn} Zombie.");
        UpdateWaveUI(); 
        UpdateZombieLeftUI();

        StartCoroutine(SpawnZombieRoutine());
    }

    private IEnumerator SpawnZombieRoutine()
    {
        while (isWaveActive && zombiesSpawned < zombiesToSpawn)
        {
            GameObject[] activeZombies = GameObject.FindGameObjectsWithTag("Zombie");
            UpdateZombieLeftUI();

            if (activeZombies.Length < maxZombiesAlive)
            {
                SpawnZombie();
                zombiesSpawned++;
                UpdateZombieLeftUI();
            }

            yield return new WaitForSeconds(spawnRate);
        }
    }

    void Update()
    {
        if (!isWaveActive) return;

        GameObject[] activeZombies = GameObject.FindGameObjectsWithTag("Zombie");
        zombiesKilled = zombiesToSpawn - activeZombies.Length;
        
        if (zombieLeftText != null)
        {
            int remainingInWave = Mathf.Max(0, zombiesToSpawn - zombiesSpawned + activeZombies.Length);
            zombieLeftText.text = $"Zombies: {remainingInWave}";
        }

        if (zombiesSpawned >= zombiesToSpawn && activeZombies.Length == 0 && zombiesToSpawn > 0)
        {
            isWaveActive = false; 
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
        Debug.Log($"DeadWave Log: WAVE {currentWave} SELESAI!");
        if (countdownUIText != null) countdownUIText.text = "WAVE CLEAR!";
        
        SaveGameProgress(); 
        yield return new WaitForSeconds(2f);
        if (countdownUIText != null) countdownUIText.text = "";

        // TENTUKAN POIN UPGRADE SESUAI ATURAN ANDA
        if (currentWave == 1) upgradePointsAvailable = 1;      // Wave 1 beres -> Pilih 1
        else if (currentWave == 2 || currentWave == 3) upgradePointsAvailable = 2; // Wave 2 & 3 -> Pilih 2
        else upgradePointsAvailable = 2;                       // Wave selanjutnya otomatis dapat 2 poin

        // Pemicu Kocokan Gacha 3 Pilihan Acak dari 7 Total Upgrade
        RollUpgradeOptions();

        if (upgradePanelObject != null)
        {
            UpdateUpgradeTitleUI();
            upgradePanelObject.SetActive(true);
            Time.timeScale = 0f; 
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true; 
        }
        else
        {
            StartCoroutine(WaveBreakCountdownRoutine());
        }
    }

    // LOGIKA GACHA: Mengocok 3 angka unik dari jangkauan 0 sampai 6 (Total 7 Upgrade)
    private void RollUpgradeOptions()
    {
        currentRolledUpgrades.Clear();
        List<int> pool = new List<int> { 0, 1, 2, 3, 4, 5, 6 }; // Kumpulan ID 7 tipe upgrade

        // Ambil 3 angka secara acak tanpa kembar
        for (int i = 0; i < 3; i++)
        {
            int index = Random.Range(0, pool.Count);
            currentRolledUpgrades.Add(pool[index]);
            pool.RemoveAt(index); // Hapus agar tidak keluar dua kali
        }

        // Tampilkan teks nama upgrade hasil kocokan ke tombol UI Anda
        if (buttonTextOption1) buttonTextOption1.text = GetUpgradeNameByID(currentRolledUpgrades[0]);
        if (buttonTextOption2) buttonTextOption2.text = GetUpgradeNameByID(currentRolledUpgrades[1]);
        if (buttonTextOption3) buttonTextOption3.text = GetUpgradeNameByID(currentRolledUpgrades[2]);
    }

    // Penerjemah ID angka menjadi teks nama tombol di Canvas
    private string GetUpgradeNameByID(int id)
    {
        return id switch
        {
            0 => "AK74 Damage (+5)",
            1 => "Fire Rate (+10%)",
            2 => "Kapasitas Magasin (+10)",
            3 => "Spam Ayunan Melee (+15%)",
            4 => "Jangkauan Pukulan (+0.5m)",
            5 => "Max Darah Player (+25)",
            6 => "Kapasitas Tas Ammo (+40)",
            _ => "Unknown Upgrade"
        };
    }

    // DIHUBUNGKAN KE TOMBOL 1
    public void ClickedOption1() { ExecuteUpgradeByID(currentRolledUpgrades[0]); }
    // DIHUBUNGKAN KE TOMBOL 2
    public void ClickedOption2() { ExecuteUpgradeByID(currentRolledUpgrades[1]); }
    // DIHUBUNGKAN KE TOMBOL 3
    public void ClickedOption3() { ExecuteUpgradeByID(currentRolledUpgrades[2]); }

    // Eksekutor modifikasi variabel berdasarkan tombol pilihan yang diambil player
    private void ExecuteUpgradeByID(int id)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();

        switch (id)
        {
            case 0: // AK74 Damage
                if (wManager) wManager.fireDamage += 5;
                Debug.Log($"DeadWave Log: Upgrade Damage Senjata Aktif! Total: {wManager.fireDamage}");
                break;
            case 1: // Fire Rate
                if (wManager) wManager.fireRate = Mathf.Max(0.06f, wManager.fireRate - 0.01f);
                Debug.Log($"DeadWave Log: Upgrade Fire Rate Aktif! Jeda: {wManager.fireRate}");
                break;
            case 2: // Kapasitas Magasin
                if (wManager) { wManager.magCapacity += 10; wManager.ammoInMag += 10; wManager.UpdateAmmoUI(); }
                Debug.Log("DeadWave Log: Upgrade Kapasitas Magasin Aktif!");
                break;
            case 3: // Kecepatan Ayunan Melee & Tinju
                if (wManager) { wManager.meleeCooldown = Mathf.Max(0.25f, wManager.meleeCooldown - 0.08f); wManager.punchCooldown = Mathf.Max(0.2f, wManager.punchCooldown - 0.06f); }
                Debug.Log("DeadWave Log: Upgrade Kecepatan Ayunan Melee Aktif!");
                break;
            case 4: // Jangkauan Jarak Serangan
                if (wManager) { wManager.meleeRange += 0.5f; wManager.punchRange += 0.3f; }
                Debug.Log("DeadWave Log: Upgrade Jangkauan Melee Aktif!");
                break;
            case 5: // Max Darah Player
                if (pHealth) { pHealth.maxHealth += 25; pHealth.SetHealthFromSpawner(pHealth.currentHealth + 25); }
                Debug.Log("DeadWave Log: Upgrade Max Darah Player Aktif!");
                break;
            case 6: // Kapasitas Kantong Tas Peluru
                if (wManager) { wManager.maxCarriableAmmo += 40; wManager.AddCarriableAmmo(40); }
                Debug.Log("DeadWave Log: Upgrade Kapasitas Tas Peluru Aktif!");
                break;
        }

        // Potong sisa poin upgrade setelah memilih
        upgradePointsAvailable--;

        if (upgradePointsAvailable > 0)
        {
            // Jika masih punya sisa pilihan (di Wave 2 atau 3), kocok ulang opsi baru biar seru!
            UpdateUpgradeTitleUI();
            RollUpgradeOptions();
        }
        else
        {
            // Jika jatah memilih sudah habis, tutup panel dan lanjutkan game
            ResumeGameAfterUpgrade();
        }
    }

    private void UpdateUpgradeTitleUI()
    {
        if (upgradeTitleUIText != null)
        {
            upgradeTitleUIText.text = $"PILIH {upgradePointsAvailable} UPGRADE!";
        }
    }

    private void ResumeGameAfterUpgrade()
    {
        if (upgradePanelObject) upgradePanelObject.SetActive(false);

        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;

        StartCoroutine(WaveBreakCountdownRoutine());
    }

    private IEnumerator WaveBreakCountdownRoutine()
    {
        currentWave++;
        zombiesToSpawn += 5; 

        for (int i = (int)waveBreakDuration; i > 0; i--)
        {
            if (countdownUIText != null) countdownUIText.text = $"NEXT WAVE IN: {i}s";
            yield return new WaitForSeconds(1f);
        }

        if (countdownUIText != null) countdownUIText.text = "";
        StartNewWave();
    }

    public void SaveGameProgress()
    {
        PlayerPrefs.SetInt("SavedWave", currentWave);
        PlayerPrefs.SetInt("SavedScore", totalScore);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
            if (wManager != null)
            {
                PlayerPrefs.SetInt("HasMelee", wManager.hasMelee ? 1 : 0);
                PlayerPrefs.SetInt("HasFirearm", wManager.hasFirearm ? 1 : 0);
                PlayerPrefs.SetInt("ActiveWeaponIndex", (int)wManager.activeWeapon);
                PlayerPrefs.SetInt("AmmoInMag", wManager.ammoInMag);
                PlayerPrefs.SetInt("CarriableAmmo", wManager.carriableAmmo);
                PlayerPrefs.SetInt("SavedWeaponDamage", wManager.fireDamage);
                PlayerPrefs.SetFloat("SavedWeaponFireRate", wManager.fireRate);
            }

            PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                PlayerPrefs.SetInt("SavedPlayerHealth", pHealth.currentHealth);
            }
        }

        PlayerPrefs.Save();
    }

    public void LoadGameProgress()
    {
        if (PlayerPrefs.HasKey("SavedWave"))
        {
            currentWave = PlayerPrefs.GetInt("SavedWave", 1);
            totalScore = PlayerPrefs.GetInt("SavedScore", 0);
            zombiesToSpawn = 5 + ((currentWave - 1) * 5); 

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
                if (wManager != null)
                {
                    wManager.hasMelee = PlayerPrefs.GetInt("HasMelee", 0) == 1;
                    wManager.hasFirearm = PlayerPrefs.GetInt("HasFirearm", 0) == 1;
                    wManager.ammoInMag = PlayerPrefs.GetInt("AmmoInMag", 30);
                    wManager.carriableAmmo = PlayerPrefs.GetInt("CarriableAmmo", 60);
                    wManager.fireDamage = PlayerPrefs.GetInt("SavedWeaponDamage", wManager.fireDamage);
                    wManager.fireRate = PlayerPrefs.GetFloat("SavedWeaponFireRate", wManager.fireRate);
                    
                    int weaponIndex = PlayerPrefs.GetInt("ActiveWeaponIndex", 0);
                    wManager.activeWeapon = (DeadWaveWeapon)weaponIndex;
                    wManager.OnItemEquip(); 
                }

                PlayerHealth pHealth = pHealth = player.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    int savedHP = PlayerPrefs.GetInt("SavedPlayerHealth", pHealth.maxHealth);
                    pHealth.SetHealthFromSpawner(savedHP);
                }
            }

            UpdateScoreUI();
            UpdateWaveUI();
        }
    }

    private void UpdateWaveUI()
    {
        if (waveUIText != null) waveUIText.text = $"Wave {currentWave}";
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