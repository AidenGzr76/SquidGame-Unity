using UnityEngine;
using RTLTMPro;
using System.Collections;
using System;

public class PrizeAnimatorUI : MonoBehaviour
{
    [Header("UI References")]
    public RTLTextMeshPro totalPrizeText; // <<< متن "پول" (سبز)
    
    [Header("Animation Settings")]
    public float animationDuration = 2.0f;
    public float startDelay = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip moneyTickSound; 

    private int prizeToAdd = 0;

    void Start()
    {
        // ۱. مقدار نهایی "پول" رو از گیم منیجر بخوان
        int finalPrize = GameManager.Instance.totalPrize;

        // ۲. مقدار "پول" اضافه شده رو از "PrizeToAdd" بخوان
        // (این کلید "فقط" توسط GameManager.EndMainGameRun ساخته می‌شه)
        prizeToAdd = PlayerPrefs.GetInt("PrizeToAdd", 0);

        if (prizeToAdd > 0)
        {
            // ۳. پرچم رو تنظیم کن
            CoinDisplayUI.IsAnimatingScore = true;
            
            // ۴. مقدار اولیه پول = مقدار نهایی - مقدار اضافه شده
            int initialPrize = finalPrize - prizeToAdd;

            totalPrizeText.text = initialPrize.ToString("N0");
            StartCoroutine(AnimatePrizeRoutine(initialPrize, finalPrize));
        }
        else
        {
            // اگر پولی اضافه نشده، فقط مقدار نهایی رو نشون بده
            totalPrizeText.text = finalPrize.ToString("N0");
        }
    }
    
    // (این تابع از CurrencyExchangeManager حذف شد چون فقط در Start لازمه)
    private IEnumerator AnimatePrizeRoutine(int oldPrize, int newPrize)
    {
        yield return new WaitForSeconds(startDelay);
        
        // تنظیم مقادیر اولیه متن‌ها
        totalPrizeText.text = oldPrize.ToString("N0");
        // (باکس دومی برای نمایش جایزه لازم نیست، چون خودمون داریم انیمیتش می‌کنیم)

        float elapsedTime = 0f;
        int lastSoundFrame = 0; 

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration; 

            int displayedTotal = (int)Mathf.Lerp(oldPrize, newPrize, t);
            totalPrizeText.text = displayedTotal.ToString("N0");
            
            // پخش صدا
            int currentSoundFrame = (int)(t * 10);
            if (currentSoundFrame > lastSoundFrame && audioSource != null && moneyTickSound != null)
            {
                audioSource.PlayOneShot(moneyTickSound);
                lastSoundFrame = currentSoundFrame;
            }
            yield return null;
        }

        // تنظیم مقادیر نهایی
        totalPrizeText.text = newPrize.ToString("N0");

        // پاک کردن جایزه از حافظه
        PlayerPrefs.SetInt("PrizeToAdd", 0);
        PlayerPrefs.Save();
        
        // آزادسازی پرچم
        CoinDisplayUI.IsAnimatingScore = false;
        
        // خبر دادن به UI ها برای آپدیت نهایی
        GameManager.OnCoinsChanged?.Invoke(GameManager.Instance.totalCoins);
        GameManager.OnPrizeChanged?.Invoke(GameManager.Instance.totalPrize);
    }
}