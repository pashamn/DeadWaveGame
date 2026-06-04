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
    public int zombiesToSpawn = 10; // Setel bawaan ke 10 sesuai rencana Wave 1 Anda
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
    public TextMeshProUGUI buttonTextOption1;
    public TextMeshProUGUI buttonTextOption2;
    public TextMeshProUGUI buttonTextOption3;
    public TextMeshProUGUI upgradeTitleUIText; 

    private int zombiesSpawned;
    private int zombiesKilled;
    private int totalScore = 0;             
    private bool isWaveActive = false;      

    // Sistem Aturan Jumlah Pilihan per Wave
    private int upgradePointsAvailable = 0; 
    private List<int> currentRolledUpgrades = new List<int>(); 

    // PERBAIKAN TUTORIAL: Variabel khusus melacak total kill di Wave 1
    private int wave1KillCounter = 0; 

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
            zombiesToSpawn = 10; // Mengunci target awal Wave 1 = 10 Zombie
            
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
            zombiesToSpawn = 10; 
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

    // PERBAIKAN TUTORIAL: Dipanggil otomatis oleh ZombieHealth saat zombie mati
    public void RegisterZombieDeath()
    {
        zombiesKilled++;

        if (currentWave == 1)
        {
            wave1KillCounter++;

            if (wave1KillCounter == 2)
            {
                // Memicu instruksi teks tengah dan menyalakan Garis GPS menuju Crowbar
                if (countdownUIText != null) 
                {
                    countdownUIText.text = "ZOMBIE TUMBANG! IKUTI JALUR DI LANTAI UNTUK MENGAMBIL LINGGIS (TEKAN E)";
                    Invoke(nameof(ClearCountdownText), 5f);
                }
                if (DeadWaveQuestTracker.Instance != null) DeadWaveQuestTracker.Instance.ActivationWeaponRoute(1);
                Debug.Log("DeadWave Log: Pemicu jalur Melee aktif.");
            }
            else if (wave1KillCounter == 5) // 2 kill punch + 3 kill melee
            {
                // Memicu instruksi teks tengah dan menyalakan Garis GPS menuju AK74 Rifle
                if (countdownUIText != null) 
                {
                    countdownUIText.text = "PERSENJATAAN BERAT! IKUTI JALUR LANTAI MENUJU RIFLE AK74 (TEKAN E)";
                    Invoke(nameof(ClearCountdownText), 5f);
                }
                if (DeadWaveQuestTracker.Instance != null) DeadWaveQuestTracker.Instance.ActivationWeaponRoute(2);
                Debug.Log("DeadWave Log: Pemicu jalur Rifle aktif.");
            }
        }
    }

    private void ClearCountdownText()
    {
        if (countdownUIText != null) countdownUIText.text = "";
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

        // Poin upgrade disesuaikan aturan Anda (Wave 1 = 1 poin, Wave 2 & 3 = 2 poin)
        if (currentWave == 1) upgradePointsAvailable = 1;      
        else upgradePointsAvailable = 2;                       

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

    private void RollUpgradeOptions()
    {
        currentRolledUpgrades.Clear();
        List<int> pool = new List<int> { 0, 1, 2, 3, 4, 5, 6 }; 

        for (int i = 0; i < 3; i++)
        {
            int index = Random.Range(0, pool.Count);
            currentRolledUpgrades.Add(pool[index]);
            pool.RemoveAt(index); 
        }

        if (buttonTextOption1) buttonTextOption1.text = GetUpgradeNameByID(currentRolledUpgrades[0]);
        if (buttonTextOption2) buttonTextOption2.text = GetUpgradeNameByID(currentRolledUpgrades[1]);
        if (buttonTextOption3) buttonTextOption3.text = GetUpgradeNameByID(currentRolledUpgrades[2]);
    }

    private string GetUpgradeNameByID(int id)
    {
        return id switch
        {
            0 => "AK74 Damage (+5)",
            1 => "Fire Rate (+10%)", // Narasi UI disesuaikan jadi 10%
            2 => "Kapasitas Magasin (+10)",
            3 => "Spam Ayunan Melee (+15%)",
            4 => "Jangkauan Pukulan (+0.5m)",
            5 => "Max Darah Player (+25)",
            6 => "Kapasitas Tas Ammo (+40)",
            _ => "Unknown Upgrade"
        };
    }

    public void ClickedOption1() { ExecuteUpgradeByID(currentRolledUpgrades[0]); }
    public void ClickedOption2() { ExecuteUpgradeByID(currentRolledUpgrades[1]); }
    public void ClickedOption3() { ExecuteUpgradeByID(currentRolledUpgrades[2]); }

    private void ExecuteUpgradeByID(int id)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();

        switch (id)
        {
            case 0: 
                if (wManager) wManager.fireDamage += 5;
                break;
            case 1: // Fire Rate
                // PERBAIKAN UPGRADE: Penyeimbangan dikurangi 0.01f agar setara peningkatan 10%
                if (wManager) wManager.fireRate = Mathf.Max(0.06f, wManager.fireRate - 0.01f);
                break;
            case 2: 
                if (wManager) { wManager.magCapacity += 10; wManager.ammoInMag += 10; wManager.UpdateAmmoUI(); }
                break;
            case 3: 
                if (wManager) { wManager.meleeCooldown = Mathf.Max(0.25f, wManager.meleeCooldown - 0.08f); wManager.punchCooldown = Mathf.Max(0.2f, wManager.punchCooldown - 0.06f); }
                break;
            case 4: 
                if (wManager) { wManager.meleeRange += 0.5f; wManager.punchRange += 0.3f; }
                break;
            case 5: 
                if (pHealth) { pHealth.maxHealth += 25; pHealth.SetHealthFromSpawner(pHealth.currentHealth + 25); }
                break;
            case 6: 
                if (wManager) { wManager.maxCarriableAmmo += 40; wManager.AddCarriableAmmo(40); }
                break;
        }

        upgradePointsAvailable--;

        if (upgradePointsAvailable > 0)
        {
            UpdateUpgradeTitleUI();
            RollUpgradeOptions();
        }
        else
        {
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
        // Aturan penambahan jumlah zombie otomatis per wave: Wave 2 = 20, Wave 3 = 30
        zombiesToSpawn = 10 * currentWave; 

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
            zombiesToSpawn = 10 * currentWave; 

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