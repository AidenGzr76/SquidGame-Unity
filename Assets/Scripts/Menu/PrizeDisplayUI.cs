using UnityEngine;
using TMPro;
using RTLTMPro; // اگر از RTL استفاده می‌کنی
using System;

// <<< --- این اسکریپت باید به باکس متن "پول" (دلار سبز) وصل شود --- >>>
public class PrizeDisplayUI : MonoBehaviour
{
    private RTLTextMeshPro prizeText; 

    void Awake()
    {
        prizeText = GetComponent<RTLTextMeshPro>();
        if (prizeText == null)
        {
            Debug.LogError("RTLTextMeshPro component not found!", this.gameObject);
            return;
        }

        // <<< به رویداد جدید "پول" گوش می‌دهد >>>
        GameManager.OnPrizeChanged += UpdatePrizeText;
    }

    void OnDestroy()
    {
        GameManager.OnPrizeChanged -= UpdatePrizeText;
    }

    void Start()
    {
        // در شروع، مقدار اولیه پول را می‌خواند
        if (GameManager.Instance != null)
        {
            UpdatePrizeText(GameManager.Instance.totalPrize);
        }
    }

    void UpdatePrizeText(int newAmount)
    {
        // <<< از همان پرچم استاتیک برای توقف آپدیت استفاده می‌کند >>>
        if (CoinDisplayUI.IsAnimatingScore) return; 

        if(prizeText != null) prizeText.text = newAmount.ToString("N0");
    }
}