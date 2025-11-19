using UnityEngine;
using TMPro;
using System; // <<< اضافه شد: برای استفاده از Action

public class Simple_Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText; // متن تایمر
    public float startTime = 60f; // زمان اولیه بر حسب ثانیه
    private float currentTime;
    private bool timerRunning = false;
    public UIManager uiManager;

    #region Unity Lifecycle & Event Handling
    // <<< این بخش جدید اضافه شده است >>>
    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        GameManager.OnStageReset += ResetTimer; // به سیگنال ریست گوش بده
    }
    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        GameManager.OnStageReset -= ResetTimer; // لغو ثبت‌نام
    }
    #endregion

    void Start()
    {
        // <<< به جای Start، ریست اولیه را صدا می‌زنیم >>>
        ResetTimer();
    }

    void Update()
    {
        if (timerRunning)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                timerRunning = false;
                
                // <<< اضافه شد: قبل از نمایش باخت، مطمئن شو هنوز تایمر در حال اجرا بود >>>
                if(uiManager != null) uiManager.TimeEnd(); // وقتی تایمر تموم شد
                
                // از کار انداختن این کامپوننت برای جلوگیری از اجرای مجدد TimeEnd
                this.enabled = false;
            }
            UpdateTimerUI();
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    #region Feature and Reset Logic
    // <<< این بخش کاملاً جدید است >>>

    /// <summary>
    /// این تابع به رویداد سراسری GameManager گوش می‌دهد و فیچر مربوطه را فعال می‌کند.
    /// </summary>
    private void HandleFeatureActivation(FeatureData data)
    {
        // آیا این فیچر افزایش زمان است؟
        if (data.type == FeatureData.FeatureType.GlassBridge_AddTime) // یا نوع عمومی‌تر
        {
            AddTime(data.value);
        }
    }

    /// <summary>
    /// مقدار مشخصی ثانیه به زمان فعلی اضافه می‌کند.
    /// </summary>
    public void AddTime(float secondsToAdd)
    {
        if (timerRunning)
        {
            currentTime += secondsToAdd;
            UpdateTimerUI(); // بلافاصله متن را آپدیت کن
            Debug.Log($"{secondsToAdd} seconds added to the timer!");
        }
    }

    /// <summary>
    /// تایمر را به حالت اولیه برمی‌گرداند.
    /// </summary>
    public void ResetTimer()
    {
        currentTime = startTime;
        timerRunning = true;
        UpdateTimerUI();
        this.enabled = true; // کامپوننت را دوباره فعال کن
        Debug.Log("Timer has been reset!");
    }
    #endregion
}








// using UnityEngine;
// using TMPro;

// public class Simple_Timer : MonoBehaviour
// {
//     public TextMeshProUGUI timerText; // متن تایمر
//     public float startTime = 60f; // زمان اولیه بر حسب ثانیه
//     private float currentTime;
//     private bool timerRunning = false;
//     public UIManager uiManager;

//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         // شروع تایمر
//         currentTime = startTime;
//         timerRunning = true;

//     }

//     // Update is called once per frame
//     void Update()
//     {
//         if (timerRunning)
//         {
//             currentTime -= Time.deltaTime;
//             if (currentTime <= 0)
//             {
//                 currentTime = 0;
//                 timerRunning = false;
//                 uiManager.ShowLosePanel(); // وقتی تایمر تموم شد
//             }
//             UpdateTimerUI();
//         }
//     }

//     private void UpdateTimerUI()
//     {
//         int minutes = Mathf.FloorToInt(currentTime / 60);
//         int seconds = Mathf.FloorToInt(currentTime % 60);
//         timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
//     }
// }
