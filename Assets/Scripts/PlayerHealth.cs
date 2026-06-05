using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    public int currentHealth { get; private set; }

    public bool IsDead { get; private set; }

    [Header("UI Status Bar")]
    public Image healthFill;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Tooltip("Lama tampilan Game Over sebelum kembali ke Main Menu")]
    public float gameOverDelay = 3f;

    [Header("Main Menu Scene ID Settings")]
    [Tooltip("ID Scene Main Menu pada Build Settings")]
    public int mainMenuSceneIndex = 0;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("PLAYER DEAD");

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        // Tampilkan UI Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Tampilkan cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Freeze game
        Time.timeScale = 0f;

        // Freeze semua audio
        AudioListener.pause = true;

        // Tunggu 3 detik real time
        yield return new WaitForSecondsRealtime(gameOverDelay);

        // Reset save
        PlayerPrefs.SetInt("IsContinuingGame", 0);
        PlayerPrefs.DeleteKey("SavedWave");
        PlayerPrefs.Save();

        if (PauseMenu.isPaused)
        {
            PauseMenu.isPaused = false;
        }

        // Aktifkan kembali audio
        AudioListener.pause = false;

        // Aktifkan kembali waktu
        Time.timeScale = 1f;

        // Pindah ke Main Menu
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}