using UnityEngine;
using UnityEngine.UI; // WAJIB: Untuk mengontrol komponen UI Image bawaan Unity
using TMPro; // WAJIB: Untuk mengontrol komponen teks angka TextMeshPro

public class ZombieBossHealth : MonoBehaviour
{
    [Header("Boss Health Settings")]
    public int maxHealth = 500; 
    private int currentHealth;

    private Animator animator;
    private ZombieBossController bossController;

    public bool IsDead { get; private set; }

    [Header("UI Screen Space Health Bar (DI LAYAR PEMAIN)")]
    [Tooltip("Tarik objek bernama 'Fill' dari CANVAS UTAMA ke slot ini")]
    public Image screenFill; 
    [Tooltip("Tarik objek induk Utama Bar Darah Boss dari CANVAS ke slot ini")]
    public GameObject screenBossHealthPanel;
    [Tooltip("Tarik komponen Text (TMP) angka darah Boss dari CANVAS ke slot ini")]
    public TextMeshProUGUI screenBossHealthText; 

    [Header("UI World Space Health Bar (MELAYANG DI KEPALA BOSS - BARU)")]
    [Tooltip("Tarik objek anak bernama 'Fill' dari Canvas melayang kepala Boss ke slot ini")]
    public Image fill; 
    [Tooltip("Tarik objek induk bernama 'Healt' (Canvas/Gambar Induk) dari kepala Boss ke slot ini")]
    public GameObject healt;

    private Transform mainCameraTransform;

    void Start()
    {
        currentHealth = maxHealth;

        bossController = GetComponent<ZombieBossController>();
        animator = GetComponent<Animator>();

        // Ambil referensi kamera utama untuk efek melayang menghadap player
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // =========================================================================
        // PENCARIAN UI CANVAS SCREEN SPACE OTOMATIS (Solusi Mutlak untuk Sistem Prefab)
        // =========================================================================
        if (screenBossHealthPanel == null)
        {
            screenBossHealthPanel = GameObject.Find("BossHealthBar"); 
        }

        if (screenBossHealthPanel != null)
        {
            screenBossHealthPanel.SetActive(true);

            if (screenFill == null)
            {
                Transform fillTransform = screenBossHealthPanel.transform.Find("Fill");
                if (fillTransform != null) 
                {
                    screenFill = fillTransform.GetComponent<Image>();
                }
            }

            if (screenBossHealthText == null)
            {
                screenBossHealthText = screenBossHealthPanel.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        else
        {
            Debug.LogWarning("DeadWave Boss Warning: Tidak menemukan GameObject bernama 'BossHealthBar' di Canvas Layar Utama!");
        }
        // =========================================================================

        // Setel tampilan awal kedua bar darah ke posisi penuh (100% / 1f)
        if (screenFill != null) screenFill.fillAmount = 1f;
        if (fill != null) fill.fillAmount = 1f;

        UpdateBossHealthUI(); 
    }

    void LateUpdate()
    {
        // BILLBOARD EFFECT: Memaksa objek 'Healt' melayang agar selalu berputar lurus menghadap kamera player
        if (healt != null && mainCameraTransform != null)
        {
            healt.transform.LookAt(healt.transform.position + mainCameraTransform.forward);
        }
    }

    void Update()
    {
        // MANUAL TESTING DAMAGE (Tekan K di keyboard untuk mengurangi darah Boss)
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(50);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); 
        
        Debug.Log($"DeadWave Boss Log: HP Boss berkurang! Sisa HP: {currentHealth}");

        float healthPercentage = (float)currentHealth / maxHealth;

        // 1. UPDATE UI BAR DI LAYAR UTAMA
        if (screenFill != null)
        {
            screenFill.fillAmount = healthPercentage;
        }

        // 2. UPDATE UI BAR MELAYANG DI ATAS KEPALA (BARU)
        if (fill != null)
        {
            fill.fillAmount = healthPercentage;
        }

        UpdateBossHealthUI(); 

        if (currentHealth > 0)
        {
            // Menggunakan sistem interupsi hit kode C# teranyar agar animasi tidak bug kaku
            if (bossController != null)
            {
                bossController.PlayHitAnimationDirectly();
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateBossHealthUI()
    {
        if (screenBossHealthText != null)
        {
            screenBossHealthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    void Die()
    {
        if (IsDead) return; 
        IsDead = true;

        if (bossController != null)
        {
            bossController.TriggerBossDeath();
        }

        if (ZombieSpawner.Instance != null)
        {
            ZombieSpawner.Instance.DropLootCrystal(transform.position);
            ZombieSpawner.Instance.RegisterBossDeath();
        }

        // Matikan Bar Darah Utama di Layar
        if (screenBossHealthPanel != null)
        {
            screenBossHealthPanel.SetActive(false);
        }

        // Matikan Bar Darah Melayang di Atas Kepala Boss (BARU)
        if (healt != null)
        {
            healt.SetActive(false);
        }

        Destroy(gameObject, 5f);
    }
}