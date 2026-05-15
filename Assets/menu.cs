using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject infoPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuPanel.SetActive(true);
        infoPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
