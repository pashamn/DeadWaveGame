using UnityEngine;
using UnityEngine.UI; // WAJIB: Untuk mengontrol komponen UI Image bawaan Unity

public class ZombieBossHealth : MonoBehaviour
{
    [Header("Boss Health Settings")]
    public int maxHealth = 500; 
    private int currentHealth;

    private Animator animator;
    private ZombieBossController bossController;

    public bool IsDead { get; private set; }

    [Header("UI World Space Health Bar (Sesuai Gambar Hierarchy)")]
    [Tooltip("Tarik objek bernama 'Fill' dari Hierarchy kepala Boss ke slot ini")]
    public Image fill; 
    [Tooltip("Tarik objek induk bernama 'Healt' dari Hierarchy kepala Boss ke slot ini")]
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

        // Setel tampilan awal bar darah ke posisi penuh (100% / 1f)
        if (fill != null)
        {
            fill.fillAmount = 1f;
        }
    }

    void LateUpdate()
    {
        // BILLBOARD EFFECT: Memaksa objek Healt di atas kepala Boss agar selalu 
        // berputar menghadap lurus ke mata kamera Player (Anti-Miring)
        if (healt != null && mainCameraTransform != null)
        {
            healt.transform.LookAt(healt.transform.position + mainCameraTransform.forward);
        }
    }

    void Update()
    {
        // MANUAL TESTING DAMAGE (Tekan K di keyboard untuk mengurangi darah Boss saat demo)
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(50);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        Debug.Log($"DeadWave Boss Log: HP Boss berkurang! Sisa HP: {currentHealth}");

        // UPDATE UI FILL: Hitung persentase darah menggunakan pembagian float (current / max)
        if (fill != null)
        {
            fill.fillAmount = (float)currentHealth / maxHealth;
        }

        if (currentHealth > 0)
        {
            if (animator != null) animator.Play("gethit1");
        }

        if (currentHealth <= 0)
        {
            Die();
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

        // TAMAT INSTAN: Panggil fungsi interupsi spawner agar langsung memicu Game Clear tanpa nunggu kroco mati semua
        if (ZombieSpawner.Instance != null)
        {
            ZombieSpawner.Instance.DropLootCrystal(transform.position);
            ZombieSpawner.Instance.RegisterBossDeath();
        }

        // Hancurkan/Matikan UI induk Healt melayang seketika saat Boss terkapar jatuh
        if (healt != null)
        {
            healt.SetActive(false);
        }

        Destroy(gameObject, 5f);
    }
}