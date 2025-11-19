using UnityEngine;
using UnityEngine.UI;
using TMPro; // یا RTLTMPro
using System; // لازم برای DateTime
using System.Globalization;
// using System.Diagnostics; // لازم برای تبدیل تاریخ

[RequireComponent(typeof(Button))]
public class DailyRewardButton : MonoBehaviour
{
    [Header("Reward Settings")]
    [Tooltip("تعداد سکه‌ای که به عنوان جایزه روزانه داده می‌شود")]
    public int rewardAmount = 50;
    [Tooltip("کلید ذخیره‌سازی زمان آخرین دریافت در PlayerPrefs")]
    private const string LastClaimTimeKey = "DailyRewardLastClaimTime"; // اسم کلید برای ذخیره

    [Header("UI Components")]
    [Tooltip("دکمه اصلی دریافت جایزه")]
    public Button rewardButton;
    [Tooltip("متنی که مقدار جایزه را روی دکمه نشان می‌دهد (مثلاً +50)")]
    public TextMeshProUGUI rewardAmountText; // یا RTLTextMeshPro
    [Tooltip("آبجکت متن توضیحات بالای دکمه")]
    public GameObject descriptionTextObject;
    [Tooltip("آبجکت تصویر سایه/قفل برای حالت غیرفعال")]
    public GameObject shadowOverlay;

    [Header("System Connections")]
    [Tooltip("آبجکت AlarmManager در صحنه")]
    public AlarmManager alarmManager;
    [Tooltip("پیامی که وقتی جایزه قبلاً دریافت شده نمایش داده می‌شود")]
    public string alreadyClaimedMessage = "جایزه امروز رو گرفتی! فردا دوباره سر بزن.";

    [Tooltip("آبجکت ScoreBoxAnimator در صحنه")] // <<< --- این خط جدید --- >>>
    public ScoreBoxAnimator scoreBoxAnimator; // <<< --- این خط جدید --- >>>

    private DateTime nextClaimTime; // زمانی که بازیکن می‌تواند دوباره جایزه بگیرد
    private bool isRewardAvailable = false; // آیا جایزه در حال حاضر قابل دریافت است؟

    void Awake()
    {
        // اگر کامپوننت Button وجود نداشت، به طور خودکار اضافه می‌شود (به خاطر RequireComponent)
        // اما بهتر است خودمان هم بگیریمش
        if (rewardButton == null) rewardButton = GetComponent<Button>();

        // اضافه کردن Listener به کلیک دکمه
        rewardButton.onClick.AddListener(OnRewardButtonClick);
    }

    void Start()
    {
        // نمایش مقدار جایزه روی دکمه
        if (rewardAmountText != null)
        {
            rewardAmountText.text = $"+{rewardAmount}";
        }

        // بررسی وضعیت جایزه و آپدیت ظاهر دکمه
        CheckRewardStatus();
    }

    /// <summary>
    /// وضعیت جایزه روزانه را بررسی و ظاهر دکمه را آپدیت می‌کند.
    /// </summary>
    void CheckRewardStatus()
    {
        // زمان "بعدی" که می‌توان جایزه گرفت را از PlayerPrefs بخوان
        string savedTimeStr = PlayerPrefs.GetString(LastClaimTimeKey, null);

        // آیا قبلاً جایزه‌ای گرفته شده؟
        if (!string.IsNullOrEmpty(savedTimeStr))
        {
            // سعی کن زمان ذخیره شده را به DateTime تبدیل کنی
            if (long.TryParse(savedTimeStr, out long savedTicks))
            {
                nextClaimTime = new DateTime(savedTicks);

                // آیا زمان فعلی از زمان بعدی دریافت گذشته است؟
                if (DateTime.UtcNow >= nextClaimTime)
                {
                    // بله -> جایزه قابل دریافت است
                    isRewardAvailable = true;
                }
                else
                {
                    // خیر -> هنوز در زمان انتظار هستیم
                    isRewardAvailable = false;
                }
            }
            else
            {
                // اگر مشکلی در خواندن زمان بود، فرض کن جایزه در دسترسه
                Debug.LogWarning("Daily Reward: Couldn't parse saved claim time. Assuming reward is available.");
                isRewardAvailable = true;
                nextClaimTime = DateTime.UtcNow; // ریست کردن زمان بعدی
                PlayerPrefs.DeleteKey(LastClaimTimeKey); // کلید خراب رو پاک کن
            }
        }
        else
        {
            // اگر هیچ زمانی ذخیره نشده (اولین بار)، جایزه در دسترسه
            isRewardAvailable = true;
            nextClaimTime = DateTime.UtcNow; // زمان بعدی رو فعلا همین الان در نظر بگیر
        }

        // آپدیت ظاهر دکمه بر اساس وضعیت
        UpdateAppearance();
    }

    /// <summary>
    /// ظاهر دکمه (فعال/غیرفعال، سایه، متن توضیحات) را تنظیم می‌کند.
    /// </summary>
    void UpdateAppearance()
    {
        // rewardButton.interactable = isRewardAvailable; // دکمه فقط وقتی جایزه هست فعاله

        if (shadowOverlay != null)
        {
            shadowOverlay.SetActive(!isRewardAvailable); // اگر جایزه نیست، سایه رو نشون بده
        }

        if (descriptionTextObject != null)
        {
            descriptionTextObject.SetActive(isRewardAvailable); // اگر جایزه هست، متن توضیحات رو نشون بده
        }
    }

    /// <summary>
    /// این تابع زمانی که روی دکمه کلیک می‌شود، اجرا می‌شود.
    /// </summary>
    private void OnRewardButtonClick()
    {

        // یک بار دیگه وضعیت رو چک می‌کنیم (محض احتیاط)
        CheckRewardStatus();

        if (isRewardAvailable)
        {
            // اگر جایزه در دسترس بود -> دریافتش کن
            ClaimReward();
        }
        else
        {
            // اگر در دسترس نبود -> آلارم نشان بده
            if (alarmManager != null)
            {
                // (اختیاری) می‌تونی زمان باقی‌مانده رو هم محاسبه و به پیام اضافه کنی
                TimeSpan remainingTime = nextClaimTime - DateTime.UtcNow;
                string formattedTime = $"{remainingTime.Hours:00}:{remainingTime.Minutes:00}:{remainingTime.Seconds:00}";
                // string message = $"{alreadyClaimedMessage}\nزمان باقی‌مانده: {formattedTime}";
                // alarmManager.ShowAlarm(message);
                alarmManager.ShowAlarm(alreadyClaimedMessage); // فعلا پیام ساده
            }
            else
            {
                Debug.LogError("AlarmManager به DailyRewardButton وصل نشده!");
            }
        }
    }

    /// <summary>
    /// جایزه را به بازیکن می‌دهد و زمان انتظار بعدی را ثبت می‌کند.
    /// </summary>
    private void ClaimReward()
    {
        Debug.Log($"Claiming daily reward: {rewardAmount} coins.");

        // ۱. سکه‌ها را اضافه کن
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(rewardAmount);
        }
        else
        {
            Debug.LogError("GameManager not found! Cannot add coins.");
            return; // اگر GameManager نبود، ادامه نده
        }

        // --- <<< ۲. اجرای انیمیشن سکه (بخش جدید) >>> ---
        if (scoreBoxAnimator != null)
        {
            // از تابع TriggerCoinAnimation که برای تبدیل پول ساخته بودیم، استفاده می‌کنیم
            // اول باید مقدار اضافه‌شده رو در PlayerPrefs بذاریم تا اون تابع بخونه
            PlayerPrefs.SetInt("CoinsToAdd", rewardAmount);
            PlayerPrefs.Save(); // ذخیره کردن تغییر

            scoreBoxAnimator.TriggerCoinAnimation(); // اجرای انیمیشن
        }
        else
        {
            Debug.LogWarning("ScoreBoxAnimator به DailyRewardButton وصل نشده!");
        }
        // --- <<< پایان بخش جدید >>> ---
    


        // ۲. زمان انتظار بعدی را محاسبه کن (۲۴ ساعت بعد از "الان")
        nextClaimTime = DateTime.UtcNow.AddHours(24);

        // ۳. زمان بعدی (به صورت Ticks) را در PlayerPrefs ذخیره کن
        PlayerPrefs.SetString(LastClaimTimeKey, nextClaimTime.Ticks.ToString());
        PlayerPrefs.Save(); // ذخیره کردن تغییرات

        // ۴. وضعیت را به "غیرقابل دریافت" تغییر بده
        isRewardAvailable = false;

        // ۵. ظاهر دکمه را آپدیت کن (غیرفعال، سایه‌دار، متن مخفی)
        UpdateAppearance();

        // (اختیاری) می‌تونی یه افکت صوتی یا انیمیشن کوچیک هم اینجا پخش کنی
    }

    // (اختیاری) برای تست راحت‌تر، می‌تونی یه دکمه بسازی که این تابع رو صدا بزنه
    public void ResetDailyRewardTimer_TestOnly()
    {
        PlayerPrefs.DeleteKey(LastClaimTimeKey);
        PlayerPrefs.Save();
        CheckRewardStatus();
        Debug.LogWarning("Daily Reward Timer Reset (FOR TESTING ONLY)");
    }
}