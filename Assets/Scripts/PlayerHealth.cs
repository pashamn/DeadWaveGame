using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    // Diubah menjadi properti public agar bisa dibaca oleh ZombieSpawner saat Save
    public int currentHealth { get; private set; }

    public bool IsDead { get; private set; }

    [Header("UI")]
    public Image healthFill;

    void Start()
    {
        // Fungsi Start dikosongkan dari pengaturan darah default.
        // Sekarang, ZombieSpawner yang akan bertanggung jawab menyetel darah di awal game.
    }

    void Update()
    {
        // PERBAIKAN: Tombol tes damage dipindahkan dari SPACE ke tombol K 
        // agar tidak bentrok dengan tombol Lompat (Jump) bawaan Invector.
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        Debug.Log("Player HP : " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Fungsi khusus yang dipanggil oleh ZombieSpawner saat game dimulai / di-load
    public void SetHealthFromSpawner(int amount)
    {
        currentHealth = amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (currentHealth > 0)
        {
            IsDead = false;
        }

        UpdateHealthBar();
    }

    public void UpdateHealthBar()
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    void Die()
    {
        IsDead = true;
        Debug.Log("PLAYER DEAD");
    }
}