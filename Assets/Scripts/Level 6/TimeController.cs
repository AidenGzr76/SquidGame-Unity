using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TimeController : MonoBehaviour
{

    [Header("Timer Settings")]
    public int startMinutes = 10;
    public int startSeconds = 10;

    [Header("UI")]
    public TextMeshProUGUI timerText; 
    public UIManager uiManager; // <<< این رو از کد قبلیتون اضافه کردم

    private float remainingTime;
    private bool isRunning = true;

    void Start()
    {
        remainingTime = startMinutes * 60 + startSeconds;
        UpdateTimerUI();
        StartCoroutine(TimerCoroutine());
    }

    // <<< --- فقط این تابع اصلاح شده است --- >>>
    private IEnumerator TimerCoroutine()
    {
        while (remainingTime > 0 && isRunning)
        {
            yield return new WaitForSeconds(1f);
            remainingTime--;
            UpdateTimerUI();
        }

        // اگر تایمر تمام شد "و" هنوز در حال اجرا بود (یعنی بازیکن نمرده)
        if (remainingTime <= 0 && isRunning)
        {
            Debug.Log("⏰ Timer finished! Survivors win.");

            // --- <<< تغییرات اساسی اینجاست >>> ---

            if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
            {
                // در حالت تمرینی، فقط پنل برد رو نشون بده
                if (uiManager != null) 
                    uiManager.ShowWinPanel();
            }
            else // در حالت MainFlow
            {
                Debug.Log($"Stage 6 Won (by Time). Telling GameManager.");

                // <<< --- تغییر کلیدی --- >>>
                // ما دیگر تعداد حذف شده‌ها را "نمی‌فرستیم".
                // ما نسخه "بدون پارامتر" StageComplete را صدا می‌زنیم.
                // GameManager خودش حساب می‌کند چند نفر حذف شده‌اند.
                GameManager.Instance.StageComplete();
            }
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // public void StopTimer()
    // {
    //     isRunning = false;
    //     Debug.Log("Timer stopped (likely player death).");
    // }

    // public void StopTimer()
    // {
    //     isRunning = false;
    //     Debug.Log("Timer stopped (likely player death).");
    // }
}

