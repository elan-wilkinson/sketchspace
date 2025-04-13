using UnityEngine;
using UnityEngine.SceneManagement;

public class App_Manager : MonoBehaviour
{
    public void Reset()
    {
        SceneManager.LoadScene(0);
    }
}
