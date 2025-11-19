using UnityEngine;
using UnityEngine.UI;
using TMPro; // لازم برای TextMeshProUGUI
using RTLTMPro; // اگر از RTLTextMeshPro استفاده می‌کنی

[RequireComponent(typeof(Button))] // مطمئن می‌شه که روی دکمه است
public class StartGameButton : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("کامپوننت TextMeshProUGUI روی دکمه که هزینه را نشان می‌دهد (مثلاً -25)")]
    public TextMeshProUGUI costText; // یا RTLTextMeshPro
    [Tooltip("آبجکت تصویری که سایه/قفل را روی دکمه نشان می‌دهد")]
    public GameObject shadowOverlay; // این آبجکت باید فرزند دکمه باشد

    [Header("System Connections")]
    [Tooltip("آبجکت AlarmManager در صحنه منو")]
    public AlarmManager alarmManager;
    [Tooltip("پیامی که وقتی سکه کافی نیست نمایش داده می‌شود")]
    public string notEnoughCoinsMessage = "سکه کافی برای شروع بازی نداری!";

    private Button startGameButton;
    private int requiredCost;

    void Awake()
    {
        startGameButton = GetComponent<Button>();
        // اضافه کردن Listener به صورت کدنویسی
        startGameButton.onClick.AddListener(OnStartButtonClick);
    }

    void Start()
    {
        // گرفتن هزینه از GameManager و نمایش اولیه
        if (GameManager.Instance != null)
        {
            requiredCost = GameManager.Instance.startGameCost;
            if (costText != null)
            {
                costText.text = $"-{requiredCost}"; // نمایش هزینه به شکل "-25"
            }
            // آپدیت ظاهر اولیه دکمه
            UpdateAppearance();
        }
        else
        {
            Debug.LogError("GameManager.Instance not found!");
            // در صورت نبود GameManager، دکمه را غیرفعال کن
            if (costText != null) costText.text = "-??";
            SetButtonState(false); // تابع کمکی برای فعال/غیرفعال کردن
        }

        // گوش دادن به تغییرات سکه
        GameManager.OnCoinsChanged += OnCoinsUpdated;
    }

    void OnDestroy()
    {
        // لغو ثبت‌نام برای جلوگیری از خطا
        GameManager.OnCoinsChanged -= OnCoinsUpdated;
    }

    /// <summary>
    /// این تابع هر بار که تعداد سکه‌های بازیکن تغییر کند، صدا زده می‌شود.
    /// </summary>
    private void OnCoinsUpdated(int newTotalCoins)
    {
        // ظاهر دکمه را بر اساس موجودی جدید آپدیت کن
        UpdateAppearance();
    }

    /// <summary>
    /// ظاهر دکمه (فعال/غیرفعال بودن و سایه) را بر اساس موجودی سکه تنظیم می‌کند.
    /// </summary>
    private void UpdateAppearance()
    {
        if (GameManager.Instance != null)
        {
            bool hasEnough = GameManager.Instance.HasEnoughCoins(requiredCost);
            SetButtonState(hasEnough);
        }
    }

    /// <summary>
    /// وضعیت فعال/غیرفعال بودن دکمه و سایه را تنظیم می‌کند.
    /// </summary>
    private void SetButtonState(bool enabled)
    {
        startGameButton.interactable = enabled; // کلیک‌پذیری دکمه
        if (shadowOverlay != null)
        {
            shadowOverlay.SetActive(!enabled); // اگر فعال نیست، سایه را نشان بده
        }
    }

    /// <summary>
    /// این تابع زمانی که روی دکمه کلیک می‌شود، اجرا می‌شود.
    /// </summary>
    private void OnStartButtonClick()
    {
        if (GameManager.Instance != null)
        {
            // تلاش برای شروع بازی
            bool success = GameManager.Instance.StartMainGame();

            // اگر موفق نبود (سکه کافی نداشت)...
            if (!success)
            {
                // ... آلارم را نشان بده
                if (alarmManager != null)
                {
                    alarmManager.ShowAlarm(notEnoughCoinsMessage);
                }
                else
                {
                    Debug.LogError("AlarmManager به StartGameButton وصل نشده!");
                }
            }
            // اگر موفق بود، GameManager خودش بازی را شروع می‌کند و نیازی به کار دیگری نیست.
        }
    }
}