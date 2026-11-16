using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeMenu : MonoBehaviour
{
    public void OpenCapture()
    {
        SceneManager.LoadScene("Capture");  
    }

    public void OpenSceneLibrary()
    {
        SceneManager.LoadScene("SceneLibrary"); 
    }
    
}
