using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // WAJIB: Untuk berpindah scene

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    // Properti public get agar bisa dibaca oleh ZombieSpawner saat Save Progress
    public int currentHealth { get; private set; }

    public bool IsDead { get; private set; }

    [Header("UI Status Bar")]
    public Image healthFill;

    [Header("Main Menu Scene ID Settings")]
    [Tooltip("Masukkan ID Scene Main Menu kamu dari Build Settings (biasanya 0)")]
    public int mainMenuSceneIndex = 0;

    void Start()
    {
        // Fungsi Start bersih dari intervensi setelan darah awal
    }

    void Update()
    {
        // TEST DAMAGE (Tombol Space)
        if (Input.GetKeyDown(KeyCode.Space))
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
        Debug.Log("PLAYER DEAD - LANGSUNG KEMBALI KE MAIN MENU");

        // 1. Pastikan waktu game berjalan normal (1f) agar scene baru tidak ikut membeku
        Time.timeScale = 1f;

        // 2. Matikan status pause agar script manajemen lain ikut lepas dari kondisi terkunci
        if (PauseMenu.isPaused)
        {
            PauseMenu.isPaused = false;
        }

        // 3. Reset data simpanan agar pemain tidak bisa curang "Continue" di wave tinggi setelah mati
        PlayerPrefs.SetInt("IsContinuingGame", 0);
        PlayerPrefs.DeleteKey("SavedWave"); 
        PlayerPrefs.Save();

        // 4. Bebaskan kursor mouse total agar siap digunakan bernavigasi di Main Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 5. Eksekusi lempar player langsung ke scene Main Menu
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}