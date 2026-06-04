using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject infoPanel;
    
    [Header("Save System UI Element")]
    public Button continueButton; // Tarik komponen Tombol Continue kamu ke sini via Inspector

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuPanel.SetActive(true);
        infoPanel.SetActive(false);

        // SYSTEM CHECK: Mengecek apakah pemain memiliki data simpanan wave sebelumnya
        if (PlayerPrefs.HasKey("SavedWave"))
        {
            // Jika ada data save, tombol Continue aktif (bisa diklik)
            if (continueButton != null) continueButton.interactable = true;
        }
        else
        {
            // Jika TIDAK ADA data save, tombol Continue otomatis abu-abu (mati/tidak bisa diklik)
            if (continueButton != null) continueButton.interactable = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // FUNGSI BARU: Untuk tombol NEW GAME
    public void NewGameButton(string sceneName)
    {
        // Beri tahu spawner: JANGAN memuat data lama (mulai dari Wave 1)
        PlayerPrefs.SetInt("IsContinuingGame", 0);
        PlayerPrefs.Save();

        // Memuat scene sesuai nama yang diinput di Unity Editor atau langsung "SampleScene"
        SceneManager.LoadScene(sceneName);
    }

    // FUNGSI BARU: Untuk tombol CONTINUE
    public void ContinueGameButton(string sceneName)
    {
        // Beri tahu spawner: WAJIB memuat data lama saat masuk gameplay
        PlayerPrefs.SetInt("IsContinuingGame", 1);
        PlayerPrefs.Save();

        // Memuat scene sesuai nama yang diinput di Unity Editor atau langsung "SampleScene"
        SceneManager.LoadScene(sceneName);
    }

    // Fungsi lamamu (startButton) tetap dipertahankan sebagai cadangan jika masih terikat ke UI lama
    public void startButton(string sceneName)
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void infoButton()
    {
        menuPanel.SetActive(false);
        infoPanel.SetActive(true);
    }

    public void backButton()
    {
        menuPanel.SetActive(true);
        infoPanel.SetActive(false);
    }

    public void quitButton()
    {
        Application.Quit();
        Debug.Log("Quit");
    }
}