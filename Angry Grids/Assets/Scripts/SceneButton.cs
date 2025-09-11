using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButton : MonoBehaviour
{
    public void GameSceneLoader()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void MultiPLayerSessionCreatorScene()
    {
        SceneManager.LoadScene("MultiPLayerSessionCreatorScene");
    }
}
