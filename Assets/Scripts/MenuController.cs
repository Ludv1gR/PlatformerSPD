using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject levelPanel;

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void MainMenuFromGame() {
        SceneManager.LoadScene(0);
    }

    public void ShowLevelSelect() {
        levelPanel.SetActive(true);
    }

    public void LevelSelect(int i) { // varje knapp måste skicka med en int för den level man klickat på
        SceneManager.LoadScene(i);
    }

    public void CloseLevelSelect() {
        levelPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        creditsPanel.SetActive(false);
    }
}
