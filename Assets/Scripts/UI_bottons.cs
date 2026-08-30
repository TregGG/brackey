using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_bottons : MonoBehaviour
{
    // Loads Level 1 (Index 1) or Index 0 depending on your Build Settings setup
    public void StartGame()
    {
        // Option A: Loads by scene index 1 (standard for Level 1 when 0 is Main Menu)
        SceneManager.LoadScene(1);

        // Option B: If your Level 1 is index 0 or named "Level 1", use one of these instead:
        // SceneManager.LoadScene(0);
        // SceneManager.LoadScene("Level 1");
    }

    // Restarts the currently active scene
    public void Retry()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    // Quits the application
    public void QuitGame()
    {
        // Logs a message in the Unity Editor (since Application.Quit() only works in built games)
        Debug.Log("Quit Game requested.");
        
        Application.Quit();
    }
}