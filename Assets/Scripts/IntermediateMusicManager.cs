using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // لازم برای Dictionary
using System.Linq; // لازم برای چک کردن اسم صحنه‌ها

[RequireComponent(typeof(AudioSource))]
public class IntermediateMusicManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static IntermediateMusicManager instance;
    public static IntermediateMusicManager Instance { get { return instance; } }

    private AudioSource audioSource;

    // --- موزیک‌های صحنه‌های واسط ---
    [Header("Intermediate Scene Music")]
    public AudioClip prizeMusicClip;
    public AudioClip votingMusicClip;

    // نام دقیق صحنه‌های واسط
    private string prizeSceneName = "PrizeScene";   // !! اسم صحنه پول رو اینجا دقیق بنویس !!
    private string votingSceneName = "VotingScene"; // !! اسم صحنه رای‌گیری رو اینجا دقیق بنویس !!

    // برای ذخیره زمان پخش هر موزیک
    private Dictionary<AudioClip, float> playbackTimes = new Dictionary<AudioClip, float>();
    private AudioClip currentIntermediateClip = null; // کلیپ فعلی که در حال پخش (یا باید پخش) باشد

    void Awake()
    {
        // --- پیاده‌سازی Singleton ---
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; // موزیک‌های واسط معمولاً لوپ می‌شن
        // --- پایان Singleton ---
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string loadedSceneName = scene.name;
        AudioClip clipToPlay = null;

        // ۱. تشخیص بده کدوم موزیک باید پخش بشه
        if (loadedSceneName == prizeSceneName)
        {
            clipToPlay = prizeMusicClip;
        }
        else if (loadedSceneName == votingSceneName)
        {
            clipToPlay = votingMusicClip;
        }
        // اگر صحنه منو بود، خودت رو نابود کن
        else if (loadedSceneName.Contains("Menu"))
        {
             if (audioSource.isPlaying) audioSource.Stop();
             playbackTimes.Clear(); // حافظه زمان رو پاک کن
             Destroy(this.gameObject);
             instance = null;
             return;
        }
        // else: اگر مرحله اصلی بود، clipToPlay همچنان null می‌ماند

        // ۲. مدیریت پخش/توقف/ادامه
        if (clipToPlay != null) // اگر در یکی از صحنه‌های واسط هستیم
        {
            // آیا کلیپ عوض شده یا قبلاً موزیکی پخش نمی‌شده؟
            if (currentIntermediateClip != clipToPlay || !audioSource.isPlaying)
            {
                // اگر موزیکی در حال پخش بود، زمانش رو ذخیره کن
                if (audioSource.isPlaying && currentIntermediateClip != null)
                {
                    playbackTimes[currentIntermediateClip] = audioSource.time;
                    Debug.Log($"[IntermediateMusic] Saved time {audioSource.time} for {currentIntermediateClip.name}");
                }

                // کلیپ جدید رو تنظیم کن
                currentIntermediateClip = clipToPlay;
                audioSource.clip = currentIntermediateClip;

                // آیا زمان ذخیره شده‌ای برای این کلیپ داریم؟
                if (playbackTimes.ContainsKey(currentIntermediateClip))
                {
                    audioSource.time = playbackTimes[currentIntermediateClip];
                    Debug.Log($"[IntermediateMusic] Resuming {currentIntermediateClip.name} from {audioSource.time}");
                }
                else
                {
                    audioSource.time = 0f; // اگر نداشتیم، از اول پخش کن
                    Debug.Log($"[IntermediateMusic] Playing {currentIntermediateClip.name} from beginning.");
                }
                audioSource.Play();
            }
            // else: اگر کلیپ همونه و در حال پخش هم هست، هیچ کاری نکن (ادامه پیدا می‌کنه)
        }
        else // اگر در مرحله اصلی یا منو هستیم
        {
            // اگر موزیک واسط در حال پخش بود، متوقفش کن و زمانش رو ذخیره کن
            if (audioSource.isPlaying && currentIntermediateClip != null)
            {
                playbackTimes[currentIntermediateClip] = audioSource.time;
                audioSource.Stop();
                Debug.Log($"[IntermediateMusic] Main stage loaded. Stopped {currentIntermediateClip.name} at {playbackTimes[currentIntermediateClip]}");
                // currentIntermediateClip = null; // کلیپ فعلی رو پاک می‌کنیم
            }
        }
    }
}