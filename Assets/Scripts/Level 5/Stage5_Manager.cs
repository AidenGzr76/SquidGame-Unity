using UnityEngine;

/// <summary>
/// این اسکریپت مدیر مرحله 5 (پل شیشه‌ای) است.
/// وظیفه آن شمردن تعداد AIهایی است که می‌افتند.
/// </summary>
public class Stage5_Manager : MonoBehaviour
{
    // الگوی Singleton ساده برای دسترسی راحت
    public static Stage5_Manager Instance { get; private set; }

    private int eliminatedAICount = 0;
    private bool stageEnded = false; // برای جلوگیری از چند بار حساب شدن

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// اسکریپت GlassBridgeAI این تابع را صدا می‌زند.
    /// </summary>
    public void ReportAIFallen()
    {
        if (stageEnded) return;
        
        eliminatedAICount++;
        Debug.Log($"AI fell. Total elims for this stage: {eliminatedAICount}");
    }

    /// <summary>
    /// اسکریپت PlayerClickMove این تابع را صدا می‌زند تا تعداد نهایی را بگیرد
    /// </summary>
    public int GetEliminatedCount()
    {
        stageEnded = true; // بعد از اینکه بازیکن برد، دیگه شمارش نکن
        return eliminatedAICount;
    }
}