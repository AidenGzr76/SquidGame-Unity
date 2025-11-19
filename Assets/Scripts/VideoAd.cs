using UnityEngine;
using TapsellPlusSDK;

// اسم فایل باید VideoAd.cs باشد
public class VideoAd : MonoBehaviour
{
    public static string _responseId;
    private bool _adReady = false; 

    // <<<--- این دو متغیر جدید و بسیار مهم هستند --- >>>
    [Header("Reward Settings")]
    [Tooltip("مقدار سکه‌ای که این تبلیغ جایزه می‌دهد")]
    public int adRewardAmount = 50; // مقدار جایزه

    [Tooltip("آبجکت ScoreBoxAnimator در صحنه (برای پخش انیمیشن)")]
    public ScoreBoxAnimator scoreBoxAnimator; // اتصال به انیماتور سکه
    // <<<--- پایان بخش جدید --- >>>


    void Start()
    {
        // استفاده از کلید از فایل TapsellApiKeys.cs (که در پروژه شما TapsellApiKeys.cs.local است)
        TapsellPlus.Initialize(ApiKeys.TapsellAppKey,
            adNetworkName => {
                Debug.Log(adNetworkName + " Initialized Successfully.");
                LoadAd(); 
            },
            error => Debug.Log(error.ToString()));
    }

    // تابع لود کردن تبلیغ در پس‌زمینه
    public void LoadAd()
    {
        Debug.Log("Requesting Ad in background...");
        _adReady = false; 

        // Zone ID تبلیغ جایزه‌ای خودتان را اینجا قرار دهید
        TapsellPlus.RequestRewardedVideoAd(ApiKeys.RewardedZoneId,
            tapsellPlusAdModel =>
            {
                Debug.Log("Ad is ready: " + tapsellPlusAdModel.responseId);
                _responseId = tapsellPlusAdModel.responseId;
                _adReady = true; // تبلیغ آماده نمایش است
            },
            error =>
            {
                Debug.Log("Error loading Ad " + error.message);
            }
        );
    }
    
    // --- دکمه کاربر باید این تابع را صدا بزند ---
    public void ShowAd()
    {
        if (_adReady && !string.IsNullOrEmpty(_responseId))
        {
            Debug.Log("Showing Ad...");
            TapsellPlus.ShowRewardedVideoAd(_responseId,

                // onOpenAd
                tapsellPlusAdModel => {
                    Debug.Log("onOpenAd " + tapsellPlusAdModel.zoneId);
                },

                // onReward (مهم‌ترین بخش)
                tapsellPlusAdModel =>
                {
                    Debug.Log("onReward " + tapsellPlusAdModel.zoneId);
                    
                    // <<<--- اینجا تابع اهدای جایزه را صدا می‌زنیم --- >>>
                    GiveRewardToPlayer(); 
                    
                    _adReady = false; // تبلیغ استفاده شد
                    LoadAd(); // لود کردن تبلیغ بعدی برای دفعه بعد
                },

                // onCloseAd
                tapsellPlusAdModel =>
                {
                    Debug.Log("onCloseAd " + tapsellPlusAdModel.zoneId);
                    // اگر جایزه نگرفت ولی بست، باز هم یکی جدید لود می‌کنیم
                    if (!_adReady)
                    {
                        LoadAd();
                    }
                },

                // onError
                error => {
                    Debug.Log("onError " + error.errorMessage);
                    _adReady = false;
                    LoadAd(); // اگر خطا داد، یکی دیگه لود کن
                }
            );
        }
        else
        {
            Debug.Log("Ad is not ready yet! Trying to load...");
            // (می‌توانید به کاربر پیام دهید: "تبلیغی آماده نیست، لحظاتی بعد تلاش کنید")
            LoadAd(); 
        }
    }

    // <<<--- این تابع جدید و کامل شده است (بر اساس کد DailyReward) --- >>>
    private void GiveRewardToPlayer()
    {
        Debug.Log($"Claiming Ad reward: {adRewardAmount} coins.");

        // ۱. سکه‌ها را اضافه کن (با استفاده از Singleton)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(adRewardAmount);
        }
        else
        {
            Debug.LogError("GameManager not found! Cannot add coins.");
            return; // اگر GameManager نبود، ادامه نده
        }

        // ۲. اجرای انیمیشن سکه (دقیقا مثل کد DailyReward)
        if (scoreBoxAnimator != null)
        {
            // این خطوط را از کد DailyReward شما کپی کردم
            // چون انیماتور شما احتمالا برای کار کردن به این نیاز دارد
            PlayerPrefs.SetInt("CoinsToAdd", adRewardAmount);
            PlayerPrefs.Save(); 

            scoreBoxAnimator.TriggerCoinAnimation(); // اجرای انیمیشن
        }
        else
        {
            Debug.LogWarning("ScoreBoxAnimator به اسکریپت VideoAd وصل نشده! انیمیشن پخش نشد.");
        }
    }
}