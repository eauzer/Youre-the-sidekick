using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScenes : MonoBehaviour
{
    [SerializeField] private string path;
    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;  // Application.Quit() does nothing in the editor
#else
        Application.Quit();
#endif
    }

    public void OnSwitchScene()
    {
        SceneManager.LoadScene(path);
    }
}