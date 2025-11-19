using UnityEngine;

public class GlassBridgeSoundManager : MonoBehaviour
{
    [Header("Jumping Sounds")]
    public AudioClip[] maleJumpSounds;   // صداهای پرش مرد
    public AudioClip[] femaleJumpSounds; // صداهای پرش زن

    [Header("Landing Sounds")]
    public AudioClip[] landingSounds;    // صداهای فرود روی شیشه

    [Header("Falling Screams")]
    public AudioClip[] maleScreams;
    public AudioClip[] femaleScreams;

    public static GlassBridgeSoundManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // این تابع بر اساس جنسیت، یک صدای پرش تصادفی برمی‌گرداند
    public AudioClip GetRandomJumpSound(Gender gender)
    {
        if (gender == Gender.Male)
        {
            if (maleJumpSounds == null || maleJumpSounds.Length == 0) return null;
            return maleJumpSounds[Random.Range(0, maleJumpSounds.Length)];
        }
        else
        {
            if (femaleJumpSounds == null || femaleJumpSounds.Length == 0) return null;
            return femaleJumpSounds[Random.Range(0, femaleJumpSounds.Length)];
        }
    }

    // این تابع یک صدای فرود تصادفی برمی‌گرداند
    public AudioClip GetRandomLandingSound()
    {
        if (landingSounds == null || landingSounds.Length == 0) return null;
        return landingSounds[Random.Range(0, landingSounds.Length)];
    }

    // تابع صدای جیغ بدون تغییر باقی می‌ماند
    public AudioClip GetRandomScream(Gender gender)
    {
        if (gender == Gender.Male)
        {
            if (maleScreams == null || maleScreams.Length == 0) return null;
            return maleScreams[Random.Range(0, maleScreams.Length)];
        }
        else
        {
            if (femaleScreams == null || femaleScreams.Length == 0) return null;
            return femaleScreams[Random.Range(0, femaleScreams.Length)];
        }
    }
}

// اگر این enum را در فایل جداگانه‌ای ندارید، اینجا باشد
// public enum Gender
// {
//     Male,
//     Female
// }