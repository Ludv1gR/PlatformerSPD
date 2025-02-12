using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private int levelToLoad;
    public static bool gameIsPaused = false;
    public GameObject pauseMenuUI;
    public GameObject gameOverUI;


    void Update() {
        if(Input.GetKeyDown(KeyCode.Escape) && gameIsPaused) {
            if(gameIsPaused) {
                Resume();
            } else {
                Pause();
            }
        }
    }

    public void Resume() {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1.0f;
        gameIsPaused = false;
    }

    public void Pause() {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
    }

    public void LoadMenu() {
        SceneManager.LoadScene(0);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1.0f;
        gameIsPaused = false;
    }

    public void QuitGame() {
        Application.Quit();
    }

    // PauseButton function not needed??

    public void GameOver() {
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void tryAgain() {
        SceneManager.LoadScene(levelToLoad);
        Time.timeScale = 1.0f;
    }
}
