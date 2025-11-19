using TMPro;
using UnityEngine;
using UnityEngine.UI; // اگر از TMPro استفاده می‌کنی، اینو تغییر بده به: using TMPro;

public class GameTimer : MonoBehaviour
{
    public float timeLimit = 30f;
    private float timer;
    private bool isRunning = true;

    public TextMeshProUGUI timerText; // اگر از TextMeshPro استفاده می‌کنی: public TMP_Text timerText;

    void Start()
    {
        timer = timeLimit;
        UpdateTimerText();
    }

    void Update()
    {
        if (!isRunning) return;

        timer -= Time.deltaTime;
        if (timer < 0f) timer = 0f;

        UpdateTimerText();

        if (timer <= 0f)
        {
            isRunning = false;
            Debug.LogWarning("زمان تموم شد!");
            FindFirstObjectByType<UIManager>().TimeEnd();
        }
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // <<< این تابع کاملاً جدید است >>>
    /// <summary>
    /// مقدار مشخصی ثانیه به زمان فعلی اضافه می‌کند.
    /// </summary>
    public void AddTime(float secondsToAdd)
    {
        if (isRunning)
        {
            timer += secondsToAdd;
            UpdateTimerText(); // بلافاصله متن را آپدیت کن
            Debug.Log($"{secondsToAdd} seconds added to the timer!");
        }
    }



    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        GameManager.OnStageRespawn += ResetTimer;
    }

    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        GameManager.OnStageRespawn -= ResetTimer;
    }

    private void HandleFeatureActivation(FeatureData data)
    {
        if (data.type == FeatureData.FeatureType.AddTimeDalgona)
        {
            AddTime(data.value);
        }
    }

    public void ResetTimer()
    {
        timer = timeLimit;
        isRunning = true;
        UpdateTimerText();
        Debug.Log("Timer has been reset!");
    }


}
