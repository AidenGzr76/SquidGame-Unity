using UnityEngine;

public class TugOfWarSoundManager : MonoBehaviour
{
    [Header("Straining Sounds")]
    public AudioClip[] strainingSounds;

    [Header("Falling Screams")]
    public AudioClip[] maleScreams;   // صداهای جیغ مرد
    public AudioClip[] femaleScreams; // صداهای جیغ زن

    public static TugOfWarSoundManager Instance { get; private set; }

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

    public AudioClip GetRandomStrainingSound()
    {
        if (strainingSounds == null || strainingSounds.Length == 0) return null;
        return strainingSounds[Random.Range(0, strainingSounds.Length)];
    }

    // --- این تابع جدید را اضافه کن ---
    // این تابع بر اساس جنسیت، یک صدای جیغ تصادفی برمی‌گرداند
    public AudioClip GetRandomScream(Gender gender)
    {
        if (gender == Gender.Male)
        {
            if (maleScreams == null || maleScreams.Length == 0) return null;
            return maleScreams[Random.Range(0, maleScreams.Length)];
        }
        else // if (gender == Gender.Female)
        {
            if (femaleScreams == null || femaleScreams.Length == 0) return null;
            return femaleScreams[Random.Range(0, femaleScreams.Length)];
        }
    }
}

// یک enum برای تعریف جنسیت می‌سازیم تا کد خواناتر شود
public enum Gender
{
    Male,
    Female
}