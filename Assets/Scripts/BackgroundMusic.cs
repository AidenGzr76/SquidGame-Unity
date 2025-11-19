using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.Play(); // با شروع صحنه خودش پخش میشه
    }

    public void StopMusic()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    public void PauseMusic()
    {
        if (audioSource != null)
            audioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (audioSource != null)
            audioSource.UnPause();
    }
}















// using UnityEngine;

// [RequireComponent(typeof(AudioSource))]
// public class BackgroundMusic : MonoBehaviour
// {
//     private AudioSource audioSource;

//     void Awake()
//     {
//         // فقط کامپوننت AudioSource رو بگیر
//         audioSource = GetComponent<AudioSource>();
        
//         // و پخشش کن
//         if (audioSource != null && !audioSource.isPlaying)
//         {
//             audioSource.Play();
//         }
//     }

//     // تمام!
//     // وقتی صحنه عوض بشه، این آبجکت "همراه با صحنه" نابود می‌شه.
//     // و آبجکت موزیک صحنه بعدی به همین سادگی اجرا می‌شه.
// }