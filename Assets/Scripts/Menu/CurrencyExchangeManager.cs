using UnityEngine;
using UnityEngine.UI;
using RTLTMPro; 
using System.Collections;

public class CurrencyExchangeManager : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject popupPanel; 

    [Header("Buttons")]
    public Button openPopupButton;
    public Button closeButton; 
    public Button plusButton; 
    public Button minusButton;
    public Button confirmButton;

    [Header("UI Texts")]
    public RTLTextMeshPro prizeToConvertText; // متن "پول" (سبز)
    public RTLTextMeshPro coinsToReceiveText; // متن "سکه" (طلایی)

    [Header("Conversion Settings")]
    public int prizeStep = 1000; // <<< --- اصلاح شد: واحد "پول"
    public int coinStep = 50;    // <<< --- اصلاح شد: واحد "سکه"
    public int minPrizeAmount = 1000; // <<< --- اصلاح شد: حداقل "پول"

    // این متغیر حالا "تعداد قدم‌ها" رو نگه می‌داره (1, 2, 3, ...)
    private int currentStepLevel = 1; 
    private Image confirmButtonImage; // <<< --- این خط جدید --- >>> (برای تغییر رنگ دکمه تایید)
    
    private ScoreBoxAnimator coinAnimator; // <<< --- رفرنس به انیماتور "سکه"

    void Start()
    {
        // رفرنس به انیماتور "سکه" رو پیدا کن
        coinAnimator = FindAnyObjectByType<ScoreBoxAnimator>(); 

        openPopupButton.onClick.AddListener(OpenPopup);
        closeButton.onClick.AddListener(ClosePopup);
        plusButton.onClick.AddListener(IncreaseAmount);
        minusButton.onClick.AddListener(DecreaseAmount);
        confirmButton.onClick.AddListener(ConfirmConversion);

        popupPanel.SetActive(false);

        // <<< --- این خط جدید اضافه شود --- >>>
        // کامپوننت Image دکمه تایید را برای تغییر رنگ می‌گیریم
        if (confirmButton != null) confirmButtonImage = confirmButton.GetComponent<Image>();
    }

    public void OpenPopup()
    {
        currentStepLevel = 1; // همیشه با قدم 1 (حداقل مقدار) شروع کن
        popupPanel.SetActive(true);

        // <<< --- تغییر اصلی اینجاست --- >>>
        // به جای صدا زدن مستقیم، کوروتین را صدا می‌زنیم
        StartCoroutine(OpenPopupRoutine());
    }
    
    /// <summary>
    /// این کوروتین یک فریم صبر می‌کند تا پنل بیدار شود
    /// و سپس UI را آپدیت می‌کند.
    /// </summary>
    private IEnumerator OpenPopupRoutine()
    {
        // <<< --- جادو اینجاست --- >>>
        // یک فریم صبر کن تا یونیتی پنل و متن‌ها را کامل لود کند
        yield return null; 
        
        // حالا که مطمئنیم همه چیز بیدار است، UI را آپدیت کن
        UpdateUI();
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }

    void IncreaseAmount()
    {
        currentStepLevel++;
        UpdateUI();
    }

    void DecreaseAmount()
    {
        currentStepLevel--;
        UpdateUI();
    }

    void UpdateUI()
    {
        // ۱. محاسبه مقادیر فعلی
        int prizeAmount = currentStepLevel * prizeStep;
        int coinAmount = currentStepLevel * coinStep;

        // ۲. آپدیت متن‌ها
        prizeToConvertText.text = prizeAmount.ToString("N0");
        coinsToReceiveText.text = coinAmount.ToString("N0");

        // ۳. گرفتن موجودی فعلی پول از GameManager
        int currentPlayerPrize = 0;
        if (GameManager.Instance != null)
        {
            currentPlayerPrize = GameManager.Instance.totalPrize; // موجودی پول (سبز)
        }
        else
        {
             // اگر GameManager نبود، همه دکمه‌ها را غیرفعال کن
             minusButton.interactable = false;
             plusButton.interactable = false;
             confirmButton.interactable = false;
             if (confirmButtonImage != null) confirmButtonImage.color = new Color(1f, 1f, 1f, 0.5f); // کمرنگ کردن
             Debug.LogError("GameManager not found in UpdateUI!");
             return;
        }

        // ۴. فعال/غیرفعال کردن دکمه منفی (مثل قبل)
        minusButton.interactable = (currentStepLevel > 1);

        // --- <<< ۵. منطق جدید برای دکمه مثبت >>> ---
        // مقدار "بعدی" پول مورد نیاز چقدر است؟
        int nextPrizeAmount = (currentStepLevel + 1) * prizeStep;
        // آیا بازیکن می‌تواند مقدار بعدی را بپردازد؟
        bool canAffordNext = currentPlayerPrize >= nextPrizeAmount;
        plusButton.interactable = canAffordNext;
        // --- <<< پایان منطق جدید مثبت >>> ---

        // --- <<< ۶. منطق جدید برای دکمه تایید (و کمرنگ شدن) >>> ---
        // آیا بازیکن می‌تواند مقدار "فعلی" را بپردازد؟
        bool canAffordCurrent = currentPlayerPrize >= prizeAmount;
        // آیا بازیکن "حداقل" مقدار لازم (minPrizeAmount) را دارد؟
        bool hasMinimumRequired = currentPlayerPrize >= minPrizeAmount;

        // دکمه تایید فقط زمانی فعال است که هم حداقل لازم را داشته باشد و هم بتواند مقدار فعلی را بپردازد
        confirmButton.interactable = hasMinimumRequired && canAffordCurrent;

        // تغییر رنگ (کمرنگ کردن) دکمه تایید بر اساس فعال بودن
        if (confirmButtonImage != null)
        {
            // اگر فعال است سفید، اگر غیرفعال است خاکستری نیمه‌شفاف
            confirmButtonImage.color = confirmButton.interactable ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
        // --- <<< پایان منطق جدید تایید >>> ---
    }

    void ConfirmConversion()
    {
        // <<< --- منطق جدید تایید --- >>>
        int prizeToSpend = currentStepLevel * prizeStep;
        int coinsToGain = currentStepLevel * coinStep;

        // ۴. چک نهایی
        if (!GameManager.Instance.HasEnoughPrize(prizeToSpend))
        {
            return;
        }

        // ۵. پاپ‌آپ را ببند
        ClosePopup();

        // ۶. از "پول" کم کن (UI پول بلافاصله آپدیت می‌شه چون پرچم انیمیشن ست نیست)
        GameManager.Instance.SpendPrize(prizeToSpend);
        
        // ۷. به "سکه" اضافه کن
        GameManager.Instance.AddCoins(coinsToGain);

        // ۸. داده "CoinsToAdd" را برای انیماتور "سکه" تنظیم کن
        PlayerPrefs.SetInt("CoinsToAdd", coinsToGain);
        PlayerPrefs.Save();

        // ۹. انیماتور "سکه" را به صورت دستی اجرا کن
        if (coinAnimator != null)
        {
            // انیماتور سکه رو مجبور به اجرای دوباره تابع Start می‌کنیم
            // (این ساده‌ترین راهه، اگرچه می‌شد تابع Trigger بهش اضافه کرد)
            
            // یک راه بهتر: یک تابع عمومی در ScoreBoxAnimator بسازیم
            // فعلا فرض می‌کنیم که بستن و باز کردن صحنه یا ری‌استارت
            // باعث اجرای مجدد انیماتور می‌شه.
            
            // --- بیایید انیماتور سکه را هم اصلاح کنیم ---
            // (لطفا تغییر بخش ۵ را هم ببین)
            coinAnimator.TriggerCoinAnimation();
        }
    }
}