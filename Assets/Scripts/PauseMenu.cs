using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;

    public static bool isPaused = false;

    private void Start()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        isPaused = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        Time.timeScale = 0f;
        AudioListener.pause = true;

        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Home(int sceneID)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        isPaused = false;

        SceneManager.LoadScene(sceneID);
    }
}