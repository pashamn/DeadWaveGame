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

    void Start()
    {
        currentHealth = maxHealth;

        bossController = GetComponent<ZombieBossController>();
        animator = GetComponent<Animator>();

        // =========================================================================
        // PENCARIAN UI CANVAS OTOMATIS (Solusi Mutlak untuk Sistem Prefab)
        // =========================================================================
        
        // 1. Cari objek induk Bar Darah Boss di Canvas berdasarkan namanya
        if (screenBossHealthPanel == null)
        {
            screenBossHealthPanel = GameObject.Find("BossHealthBar"); 
        }

        // 2. Jika objek induknya ketemu, cari komponen anak di dalamnya
        if (screenBossHealthPanel != null)
        {
            // Nyalakan panelnya sesaat agar komponen di dalamnya bisa dideteksi oleh script
            screenBossHealthPanel.SetActive(true);

            // Cari objek gambar merah bernama "Fill" di dalam BossHealthBar
            if (screenFill == null)
            {
                Transform fillTransform = screenBossHealthPanel.transform.Find("Fill");
                if (fillTransform != null) 
                {
                    screenFill = fillTransform.GetComponent<Image>();
                }
            }

            // Cari komponen teks angka TMP yang ada di dalam BossHealthBar
            if (screenBossHealthText == null)
            {
                screenBossHealthText = screenBossHealthPanel.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        else
        {
            Debug.LogError("DeadWave Boss Error: Tidak menemukan GameObject bernama 'BossHealthBar' di Canvas!");
        }
        // =========================================================================

        // Setel tampilan awal bar darah ke posisi penuh (100% / 1f)
        if (screenFill != null)
        {
            screenFill.fillAmount = 1f;
        }

        // Perbarui teks angka HP pertama kali saat Boss lahir
        UpdateBossHealthUI(); 
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
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Pengaman agar HP tidak minus
        
        Debug.Log($"DeadWave Boss Log: HP Boss berkurang! Sisa HP: {currentHealth}");

        // UPDATE UI FILL DI LAYAR
        if (screenFill != null)
        {
            screenFill.fillAmount = (float)currentHealth / maxHealth;
        }

        UpdateBossHealthUI(); // Perbarui teks angka setiap kali boss terluka

        if (currentHealth > 0)
        {
            if (animator != null) animator.Play("gethit1");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Mengontrol tulisan angka HP di layar pemain
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

        // 2. MATIKAN UTAMA: Saat Boss mati total, matikan/sembunyikan Bar Darah dari layar pemain
        if (screenBossHealthPanel != null)
        {
            screenBossHealthPanel.SetActive(false);
        }

        Destroy(gameObject, 5f);
    }
}