using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField]
    private string sceneName;

    public void OnClick()
    {
        SceneManager.LoadScene(sceneName);
    }
}