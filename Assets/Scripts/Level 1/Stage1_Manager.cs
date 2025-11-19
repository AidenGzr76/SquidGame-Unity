using UnityEngine;

/// <summary>
/// (تغییر یافته) این اسکریپت "داور" مرحله 1 است.
/// وظیفه آن فقط اطلاع دادن برد یا باخت بازیکن به GameManager است.
/// </summary>
public class Stage1_Manager : MonoBehaviour
{
    public static Stage1_Manager Instance { get; private set; }
    private bool stageEnded = false;

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
    /// اسکریپت PlayerController (یا اسکریپت خط پایان) این تابع را صدا می‌زند.
    /// </summary>
    public void ReportPlayerWon()
    {
        if (stageEnded) return;
        stageEnded = true;

        Debug.Log("Player WON Stage 1!");
        
        // <<< --- تغییر اساسی --- >>>
        // ما دیگر تعداد کشته‌ها را "نمی‌فرستیم".
        // ما نسخه "بدون پارامتر" StageComplete را صدا می‌زنیم.
        // GameManager خودش با مقایسه لیست فعلی و لیست اولیه، تعداد حذف‌شده‌ها را محاسبه می‌کند.
        GameManager.Instance.StageComplete(); 
    }

    /// <summary>
    /// این تابع زمانی صدا زده میشه که بازیکن ببازه (مثلاً توسط اسکریپت سلامتی بازیکن).
    /// </summary>
    public void ReportPlayerLost()
    {
        if (stageEnded) return;
        stageEnded = true;

        Debug.Log("Player LOST Stage 1!");
        
        // این خط درست بود و بدون تغییر باقی می‌ماند.
        GameManager.Instance.StageFailed();
    }
}