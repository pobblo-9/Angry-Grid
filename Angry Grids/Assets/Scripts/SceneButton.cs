using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneButton : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button startButton;
    public Button QuitButton;

    public void GameSceneLoader()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void MultiPLayerSessionCreatorScene()
    {
        SceneManager.LoadScene("MultiPLayerSessionCreatorScene");
    }

    public void Quit()
    {
        UnityEditor.EditorApplication.isPlaying = false;
    }

    public void SettingsUiPullUp()
    {

    }

    public void CreditsScene()
    {
        SceneManager.LoadScene("Credits");
    }

    public void MenuScene()
    {
        SceneManager.LoadScene("MenuScene");
    }

    // Generic loader so you can configure any scene name from a UI Button parameter
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }
}
