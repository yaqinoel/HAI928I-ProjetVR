using UnityEngine;
using UnityEngine.SceneManagement;

public class LibraryScreen : MonoBehaviour
{
    public void GoHome()
    {
        SceneManager.LoadScene("Home");
    }

    public void OpenARViewer()
    {
        SceneManager.LoadScene("ARSceneViewer");
    }
}
