using UnityEngine;
using TMPro;
using RTLTMPro;
using System;

public class CoinDisplayUI : MonoBehaviour
{
    private RTLTextMeshPro coinText;

    // <<< این متغیر جدید و کلیدی اضافه شده است >>>
    // این یک پرچم عمومی است که به این اسکریپت می‌گوید آپدیت را متوقف کند
    public static bool IsAnimatingScore = false;

    void Awake()
    {
        coinText = GetComponent<RTLTextMeshPro>();
        if (coinText == null)
        {
            Debug.LogError("RTLTextMeshPro component not found on this object!", this.gameObject);
            return;
        }

        GameManager.OnCoinsChanged += UpdateCoinText;
    }

    void OnDestroy()
    {
        GameManager.OnCoinsChanged -= UpdateCoinText;
    }

    void Start()
    {
        // در ابتدای بازی، مقدار اولیه سکه‌ها را از GameManager بخوان
        if (GameManager.Instance != null)
        {
            UpdateCoinText(GameManager.Instance.totalCoins);
        }
    }

    void UpdateCoinText(int newAmount)
    {
        // <<< این شرط جدید و حیاتی اضافه شده است >>>
        // فقط در صورتی متن را آپدیت کن که انیمیشنی در حال اجرا نباشد
        if (IsAnimatingScore) return;

        // Debug.Log($"CoinDisplayUI: Updating Coins to {newAmount}");
        if(coinText != null) coinText.text = newAmount.ToString("N0"); // "N0" برای کاما
    }
}