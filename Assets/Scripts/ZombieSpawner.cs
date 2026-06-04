using UnityEngine;
using System.Collections;
using TMPro; // WAJIB: Untuk mengatur UI TextMeshPro

public class ZombieSpawner : MonoBehaviour
{
    public static ZombieSpawner Instance; // Singleton agar gampang dipanggil dari script zombie

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
    public TextMeshProUGUI zombieLeftText;  
    public TextMeshProUGUI scoreUIText;     
    public GameObject upgradePanelObject;   

    private int zombiesSpawned;
    private int zombiesKilled;
    
    private int totalScore = 0;             
    private bool isWaveActive = false;      

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (upgradePanelObject != null) upgradePanelObject.SetActive(false); 
        UpdateScoreUI();
        UpdateWaveUI(); 
        
        // Ambil data status menu (1 = Continue, 0 = New Game)
        int isContinuing = PlayerPrefs.GetInt("IsContinuingGame", 0);

        if (isContinuing == 1)
        {
            // Jika Continue, load progress wave, exp, senjata, DAN darah
            LoadGameProgress(); 
            PlayerPrefs.SetInt("IsContinuingGame", 0); 
            PlayerPrefs.Save();
        }
        else
        {
            // Jika New Game, setel semua ke kondisi awal dasar pabrik
            currentWave = 1;
            zombiesToSpawn = 5;
            
            // Setel darah player ke penuh (100) karena ini game baru
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
        if (waveUIText != null) waveUIText.text = "WAVE CLEAR!";
        
        // AUTOSAVE: Menyimpan seluruh state awal wave saat ini
        SaveGameProgress(); 

        yield return new WaitForSeconds(2f);

        if (upgradePanelObject != null)
        {
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

    public void ChooseUpgradeDamage()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
            if (wManager != null)
            {
                wManager.fireDamage += 5;
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
            }
        }
        ResumeGameAfterUpgrade();
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
            if (waveUIText != null) waveUIText.text = $"NEXT WAVE IN: {i}s";
            yield return new WaitForSeconds(1f);
        }

        StartNewWave();
    }

    // ===================================================
    //            SYSTEM UTAMA SAVE DAN LOAD
    // ===================================================

    public void SaveGameProgress()
    {
        PlayerPrefs.SetInt("SavedWave", currentWave);
        PlayerPrefs.SetInt("SavedScore", totalScore);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 1. Simpan Semua Data Senjata & Ammo
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

            // 2. Simpan Data Darah Player Aktif
            PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                PlayerPrefs.SetInt("SavedPlayerHealth", pHealth.currentHealth);
            }
        }

        PlayerPrefs.Save();
        Debug.Log("DeadWave Log: Data Awal Wave Berhasil Disimpan!");
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
                // 1. Muat Ulang Persenjataan & Amunisi
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

                // 2. Muat Ulang Darah Player Berdasarkan Save Data
                PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
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

    [ContextMenu("Hapus Save Data")]
    public void DeleteSaveData()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("DeadWave Log: Semua data save game dihapus!");
    }

    // ===================================================
    //                    UPDATE UI
    // ===================================================

    private void UpdateWaveUI()
    {
        if (waveUIText != null)
        {
            waveUIText.text = $"WAVE: {currentWave}";
        }
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