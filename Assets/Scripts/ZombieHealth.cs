using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    private int currentHealth;
    private Animator animator;
    private ZombieAI zombieAI;

    public bool IsDead { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;

        zombieAI = GetComponent<ZombieAI>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // TEST DAMAGE (Bisa dihapus jika sudah tidak digunakan untuk testing)
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(25);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHealth -= damage;

        // Jika masih hidup → mainkan hit
        if (currentHealth > 0)
        {
            animator.SetTrigger("hit");
        }

        // Jika HP habis → mati
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (IsDead) return; // Pengaman ganda agar tidak mati dua kali
        
        IsDead = true;

        // 1. Eksekusi logika mati bawaan AI zombie Anda (misal menghentikan navmesh/pergerakan)
        if (zombieAI != null)
        {
            zombieAI.Die();
        }

        // 2. BARU: Perintahkan ZombieSpawner untuk menjatuhkan Kristal EXP/Score di titik kematian ini
        if (ZombieSpawner.Instance != null)
        {
            ZombieSpawner.Instance.DropLootCrystal(transform.position);
            ZombieSpawner.Instance.RegisterZombieDeath();
        }

        // 3. BARU: Hancurkan objek zombie dari arena permainan
        // Jika zombie Anda punya animasi mati ("die"), Anda bisa memberi jeda waktu hancur, misal: Destroy(gameObject, 2f);
        // Jika tidak ada atau ingin instan hilang, gunakan baris di bawah ini:
        Destroy(gameObject);
    }
}