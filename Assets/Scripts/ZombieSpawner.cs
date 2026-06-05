using UnityEngine;
using System.Collections;
using System.Collections.Generic; // WAJIB: Untuk menggunakan List dalam pengacakan
using TMPro;

public class ZombieSpawner : MonoBehaviour
{
    public static ZombieSpawner Instance;

    [Header("Zombie Prefab & Loot")]
    public GameObject zombiePrefab;
    [Tooltip("Tarik master Prefab Zombie Boss (MonsterMutant7) kalian ke sini")]
    public GameObject bossPrefab; // BARU: Slot untuk menampung master Prefab Boss
    [Tooltip("Tarik master Prefab Box Peluru Bermagnet ke sini")]
    public GameObject ammoBoxPrefab;        
    [Tooltip("Tarik master Prefab Medkit Bermagnet ke sini")]
    public GameObject medkitPrefab;        

    [Header("Wave Settings")]
    public int currentWave = 1;
    [Tooltip("Tentukan Wave berapa Boss Utama akan muncul")]
    public int finalWaveNumber = 3; // BARU: Pengunci pintu tantangan Final Wave
    public int zombiesToSpawn = 10; 
    public int maxZombiesAlive = 3;
    public float spawnRate = 2f;
    public float waveBreakDuration = 30f;  

    [Header("Spawn Area")]
    public float spawnRadius = 5f;

    [Header("UI Canvas Groups (Hanya Untuk Wave)")]
    [Tooltip("Tarik GameObject/Panel Teks 'CountDown' ke sini")]
    public GameObject countdownCanvasObject; 
    [Tooltip("Tarik GameObject/Panel Teks 'Wave' ke sini")]
    public GameObject waveCanvasObject;   
    [Tooltip("Tarik GameObject Parent 'WeaponSuggest' ke sini")]
    public GameObject weaponSuggestPanelObject;

    [Header("UI Canvas Settings")]
    public TextMeshProUGUI waveUIText;       
    public TextMeshProUGUI countdownUIText;  
    public TextMeshProUGUI zombieLeftText;  
    public TextMeshProUGUI scoreUIText;     
    [Tooltip("Tarik objek teks 'quest' ke sini")]
    public TextMeshProUGUI questUIText;      
    public GameObject upgradePanelObject;   

    [Header("UI Button Texts (Gacha Setup)")]
    public TextMeshProUGUI buttonTextOption1;
    public TextMeshProUGUI buttonTextOption2;
    public TextMeshProUGUI buttonTextOption3;
    public TextMeshProUGUI upgradeTitleUIText; 

    private int zombiesSpawned;
    private int zombiesKilled;
    private int totalScore = 0;             
    private bool isWaveActive = false;      
    private bool isBossSpawnedInFinalWave = false; // BARU: Gembok agar Boss hanya keluar tepat 1 kali

    private int upgradePointsAvailable = 0; 
    private List<int> currentRolledUpgrades = new List<int>(); 
    private int wave1KillCounter = 0; 

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (upgradePanelObject != null) upgradePanelObject.SetActive(false); 
        if (weaponSuggestPanelObject != null) weaponSuggestPanelObject.SetActive(false); 

        UpdateScoreUI();
        UpdateWaveUI(); 
        if (questUIText != null) questUIText.text = "";
        
        int isContinuing = PlayerPrefs.GetInt("IsContinuingGame", 0);

        // 1. CARI PLAYER CUKUP SATU KALI DI SINI (Deklarasi Utama)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (isContinuing == 1)
        {
            LoadGameProgress(); 
            PlayerPrefs.SetInt("IsContinuingGame", 0); 
            PlayerPrefs.Save();
        }
        else
        {
            currentWave = 1;
            zombiesToSpawn = 10; 
            
            // Menggunakan variabel playerObj yang sudah dicari di atas
            if (playerObj != null)
            {
                PlayerHealth pHealth = playerObj.GetComponent<PlayerHealth>();
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
        
        // 2. LOGIKA RESET INVECTOR (Sudah disatukan menggunakan playerObj tanpa membuat variabel baru)
        if (playerObj != null && isContinuing == 0)
        {
            var cc = playerObj.GetComponent<Invector.vCharacterController.vThirdPersonController>();
            if (cc != null)
            {
                cc.enabled = false;
                cc.enabled = true;
            }
        }

        StartCoroutine(GameStartCountdown());
    }

    private IEnumerator GameStartCountdown()
    {
        isWaveActive = false;

        // GANTI-GANTIAN: Munculkan Teks Hitung Mundur, Sembunyikan Kotak Wave & Zombie Left
        if (countdownCanvasObject != null) countdownCanvasObject.SetActive(true);
        if (waveCanvasObject != null) waveCanvasObject.SetActive(false);

        for (int i = 5; i > 0; i--)
        {
            if (countdownUIText != null) countdownUIText.text = $"Mulai dalam: {i}";
            yield return new WaitForSeconds(1f);
        }
        
        // GANTI-GANTIAN: Matikan Teks Hitung Mundur, Munculkan Kotak Wave kembali
        if (countdownCanvasObject != null) countdownCanvasObject.SetActive(false);
        if (waveCanvasObject != null) waveCanvasObject.SetActive(true);

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

        // BARU: Jika masuk Final Wave, ubah kuota zombie dan tampilkan peringatan horror
        if (currentWave >= finalWaveNumber)
        {
            isBossSpawnedInFinalWave = false;
            zombiesToSpawn = 15; // Contoh: 1 Boss + 14 Kroco Pengawal
            if (countdownCanvasObject != null) countdownCanvasObject.SetActive(true);
            if (countdownUIText != null) countdownUIText.text = "BOSS MUNCUL!";
            Invoke(nameof(HideFinalWaveAlert), 4f);
        }

        Debug.Log($"DeadWave Log: WAVE {currentWave} DIMULAI! Target: {zombiesToSpawn} Zombie.");
        UpdateWaveUI(); 
        UpdateZombieLeftUI();

        StartCoroutine(SpawnZombieRoutine());
    }

    private void HideFinalWaveAlert()
    {
        if (countdownUIText != null) countdownUIText.text = "";
        if (countdownCanvasObject != null) countdownCanvasObject.SetActive(false);
    }

    private IEnumerator SpawnZombieRoutine()
    {
        while (isWaveActive && zombiesSpawned < zombiesToSpawn)
        {
            GameObject[] activeZombies = GameObject.FindGameObjectsWithTag("Zombie");
            UpdateZombieLeftUI();

            if (activeZombies.Length < maxZombiesAlive)
            {
                // BARU: Logika peniupan terompet Boss Final Wave
                if (currentWave >= finalWaveNumber && !isBossSpawnedInFinalWave)
                {
                    SpawnBoss();
                    isBossSpawnedInFinalWave = true;
                    zombiesSpawned++;
                }
                else
                {
                    SpawnZombie();
                    zombiesSpawned++;
                }
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

            // BARU: Tamatkan permainan jika Final Wave sukses dibersihkan bersih
            if (currentWave >= finalWaveNumber)
            {
                if (countdownCanvasObject != null) countdownCanvasObject.SetActive(true);
                if (countdownUIText != null) countdownUIText.text = "CONGRATULATIONS! GAME CLEAR!";
                Time.timeScale = 0.2f; // Efek lambat kemenangan yang dramatis saat dinilai dosen
            }
            else
            {
                StartCoroutine(WaveClearRoutine());
            }
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

    // BARU: Fungsi internal pembuat bodi Boss di arena pertempuran
    void SpawnBoss()
    {
        Vector3 randomPosition = transform.position + new Vector3(
            Random.Range(-2f, 2f),
            0,
            Random.Range(-2f, 2f)
        );

        if (bossPrefab != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, randomPosition, Quaternion.identity);
            bossObj.tag = "Zombie"; // Paksa tag menjadi Zombie agar sinkron dengan baris penghitung sisa musuh
            Debug.Log("<color=purple>DeadWave Log: Master Boss Tercipta di Peta!</color>");
        }
        else
        {
            Debug.LogError("Spawner Error: Prefab Boss belum ditarik ke dalam slot ZombieSpawner!");
        }
    }

    public void RegisterZombieDeath()
    {
        zombiesKilled++;
        AddScore(100); // BARU: Tambah EXP/Score otomatis tiap kali musuh mati

        if (currentWave == 1)
        {
            wave1CounterLogic();
        }
    }

    private void wave1CounterLogic()
    {
        wave1KillCounter++;

        if (wave1KillCounter == 2)
        {
            string pesan = "ZOMBIE TUMBANG! IKUTI JALUR DI LANTAI UNTUK MENGAMBIL LINGGIS";
            StartCoroutine(ShowQuestAndFreezeRoutine(pesan, 1));
        }
        else if (wave1KillCounter == 5) 
        {
            string pesan = "PERSENJATAAN BERAT! IKUTI JALUR LANTAI MENUJU RIFLE AK74";
            StartCoroutine(ShowQuestAndFreezeRoutine(pesan, 2));
        }
    }

    private IEnumerator ShowQuestAndFreezeRoutine(string questMessage, int routeID)
    {
        if (questUIText != null) questUIText.text = questMessage;
        if (weaponSuggestPanelObject != null) weaponSuggestPanelObject.SetActive(true);

        // Freeze game total
        Time.timeScale = 0f;

        if (DeadWaveQuestTracker.Instance != null) DeadWaveQuestTracker.Instance.ActivationWeaponRoute(routeID);

        // Tunggu 5 detik nyata walaupun game sedang freeze
        yield return new WaitForSecondsRealtime(5f);

        // Unfreeze game kembali normal
        Time.timeScale = 1f;

        if (questUIText != null) questUIText.text = "";
        if (weaponSuggestPanelObject != null) weaponSuggestPanelObject.SetActive(false);
    }

    public void DropLootCrystal(Vector3 zombiePosition)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool playerHasGun = false;

        if (player != null)
        {
            PlayerWeaponCameraManager wManager = player.GetComponent<PlayerWeaponCameraManager>();
            if (wManager != null)
            {
                playerHasGun = wManager.hasFirearm;
            }
        }

        if (!playerHasGun)
        {
            int medkitChance = Random.Range(1, 101);
            if (medkitChance <= 60)
            {
                if (medkitPrefab != null)
                {
                    Instantiate(medkitPrefab, zombiePosition + Vector3.up * 0.5f, Quaternion.identity);
                }
            }
            return; 
        }

        int chance = Random.Range(1, 101); 

        if (chance <= 50) 
        {
            if (ammoBoxPrefab != null)
            {
                Instantiate(ammoBoxPrefab, zombiePosition + Vector3.up * 0.5f, Quaternion.identity);
            }
        }
        else 
        {
            if (medkitPrefab != null)
            {
                Instantiate(medkitPrefab, zombiePosition + Vector3.up * 0.5f, Quaternion.identity);
            }
        }
    }

    private IEnumerator WaveClearRoutine()
    {
        Debug.Log($"DeadWave Log: WAVE {currentWave} SELESAI!");
        
        if (countdownCanvasObject != null) countdownCanvasObject.SetActive(true);
        if (waveCanvasObject != null) waveCanvasObject.SetActive(false);
        if (countdownUIText != null) countdownUIText.text = "WAVE CLEAR!";
        
        SaveGameProgress();
        yield return new WaitForSeconds(2f);
        if (countdownUIText != null) countdownUIText.text = "";

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
            1 => "Fire Rate (+10%)", 
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
            case 1: 
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
        zombiesToSpawn = 10 * currentWave; 

        if (countdownCanvasObject != null) countdownCanvasObject.SetActive(true);
        if (waveCanvasObject != null) waveCanvasObject.SetActive(false);

        for (int i = (int)waveBreakDuration; i > 0; i--)
        {
            if (countdownUIText != null) countdownUIText.text = $"NEXT WAVE IN: {i}s";
            yield return new WaitForSeconds(1f);
        }

        if (countdownCanvasObject != null) countdownCanvasObject.SetActive(false);
        if (waveCanvasObject != null) waveCanvasObject.SetActive(true);

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
        if (waveUIText != null)
        {
            // Jika wave saat ini sudah mencapai atau melewati batas finalWaveNumber
            if (currentWave >= finalWaveNumber)
            {
                waveUIText.text = "Final Wave"; // Ubah tampilan teks Canvas menjadi "Final Wave"
            }
            else
            {
                waveUIText.text = $"Wave {currentWave}"; // Wave biasa tetap menampilkan angka
            }
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