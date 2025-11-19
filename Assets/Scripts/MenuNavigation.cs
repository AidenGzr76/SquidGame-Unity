using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    public AudioSource uiAudioSource;    // صدا پخش کن
    public AudioClip clickSound;         // افکت کلیک


    public void LoadNextScene(string sceneName)
    {
        // اول به فرمانده خبر بده که داریم میریم تمرین!
        GameManager.Instance.SetPracticeMode();

        uiAudioSource.PlayOneShot(clickSound);
        SceneManager.LoadScene(sceneName);
    }


    public void OnStartMainGameButtonClicked()
    {
        // همیشه آخرین و تنها نمونه GameManager را پیدا کرده و تابعش را صدا بزن
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMainGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance پیدا نشد! آیا GameManager در صحنه اولیه وجود دارد؟");
        }
    }
}
