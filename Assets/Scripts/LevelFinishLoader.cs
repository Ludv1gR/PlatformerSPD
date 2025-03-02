using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFinishLoader : MonoBehaviour
{
    [SerializeField] private int levelToLoad;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
                Invoke("NextLevel", 4.0f);
        }
    }

    private void NextLevel()
    {
        SceneManager.LoadScene(levelToLoad);
    }
}
