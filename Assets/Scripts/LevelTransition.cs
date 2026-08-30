using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    // You can call this from any UnityEvent!
    public void LoadNextLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}