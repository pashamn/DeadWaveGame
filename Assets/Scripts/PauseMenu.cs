using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    
    // Variabel statis untuk mengecek apakah game sedang pause atau tidak dari script lain
    public static bool isPaused = false;

    private void Start()
    {
        // Pastikan saat awal game dimulai, menu pause dalam kondisi mati
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        isPaused = false;
    }

    private void Update()
    {
        // Mendeteksi tombol Escape dijalankan
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // Memunculkan dan membebaskan cursor agar bisa mengklik tombol UI Pause
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // Mengunci kembali cursor ke tengah layar game setelah resume
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Home(int sceneID)
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(sceneID);
    }
}