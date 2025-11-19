using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MenuMusic : MonoBehaviour
{
    private static MenuMusic instance;
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // اگه وارد منوی اصلی یا منوی مراحل شدیم → موزیک منو پخش یا ادامه پیدا کنه
        if (scene.name.Contains("Menu"))
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            // اگه وارد مرحله شدیم، موزیک منو خاموش بشه
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    public static void StopMusic()
    {
        if (instance != null && instance.audioSource != null)
            instance.audioSource.Stop();
    }

    public static void PlayMusic()
    {
        if (instance != null && instance.audioSource != null && !instance.audioSource.isPlaying)
            instance.audioSource.Play();
    }
}
