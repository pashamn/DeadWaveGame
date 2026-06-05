using UnityEngine;

public class ZombieBossHealth : MonoBehaviour
{
    [Header("Boss Health Settings")]
    public int maxHealth = 500; // Darah Boss lebih tebal dari kroco
    private int currentHealth;

    private Animator animator;
    private ZombieBossController bossController;

    public bool IsDead { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;
        bossController = GetComponent<ZombieBossController>();
        animator = GetComponent<Animator>();
    }

    // Fungsi ini dipanggil oleh sistem senjata Player saat menembak Boss
    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        Debug.Log($"Boss HP: {currentHealth}");

        // Jika masih hidup, mainkan animasi "gethit1" bawaan Mutant 7
        if (currentHealth > 0)
        {
            if (animator != null) animator.Play("gethit1");
        }
        else
        {
            Die();
        }
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // 1. Jalankan fungsi mati di controller agar Boss berhenti bergerak dan memutar animasi death
        if (bossController != null)
        {
            bossController.TriggerBossDeath();
        }

        // 2. Lapor ke Spawner agar Wave bisa dianggap selesai/score bertambah
        if (ZombieSpawner.Instance != null)
        {
            ZombieSpawner.Instance.DropLootCrystal(transform.position);
            ZombieSpawner.Instance.RegisterZombieDeath();
        }

        // 3. Hancurkan objek setelah 5 detik agar animasi mati selesai diputar
        Destroy(gameObject, 5f);
    }
}