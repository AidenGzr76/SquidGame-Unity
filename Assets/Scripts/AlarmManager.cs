using UnityEngine;
using TMPro;
using System.Collections;
using RTLTMPro;
using UnityEngine.UI; // <<<--- مطمئن شو این خط وجود دارد ---<<<

public class AlarmManager : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject alarmPanel; 
    public RTLTextMeshPro alarmText;

    [Header("Sound Effects")]
    public AudioSource uiAudioSource;
    public AudioClip warningSound; 

    private Coroutine hideCoroutine;
    
    // <<< --- این متغیر جدید اضافه شده است --- >>>
    private RectTransform panelRectTransform; // برای مجبور کردن به آپدیت

    void Awake() // <<< --- اضافه کردن تابع Awake --- >>>
    {
        if (alarmPanel != null)
        {
            panelRectTransform = alarmPanel.GetComponent<RectTransform>();
        }
    }

    public void ShowAlarm(string message)
    {
        Debug.Log($"AlarmManager ShowAlarm called with message: {message}");
        
        if (alarmPanel == null || alarmText == null || panelRectTransform == null)
        {
            Debug.LogError("Alarm Panel, Alarm Text, or Panel RectTransform is not assigned!");
            return;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        if (warningSound != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(warningSound);
        }
        
        // ۱. پنل را (اگر لازم بود) خاموش کن
        if (alarmPanel.activeSelf)
        {
            alarmPanel.SetActive(false);
        }
        
        // ۲. متن جدید را تنظیم کن
        alarmText.text = message;
        
        // ۳. پنل را دوباره فعال کن
        alarmPanel.SetActive(true);

        // <<< --- ۴. اصلاح کلیدی و حیاتی اینجاست --- >>>
        // یونیتی را مجبور کن "همین الان" layout این پنل را بازسازی کند
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);
        // <<< --- پایان اصلاح --- >>>

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(4f);
        alarmPanel.SetActive(false);
        hideCoroutine = null;
    }
}








// using UnityEngine;
// using TMPro;
// using System.Collections;
// using RTLTMPro; // (اگر از RTL استفاده می‌کنی این رو نگه دار)

// public class AlarmManager : MonoBehaviour
// {
//     [Header("UI Components")]
//     public GameObject alarmPanel; 
//     public RTLTextMeshPro alarmText; // (یا TextMeshProUGUI معمولی)

//     [Header("Sound Effects")]
//     public AudioSource uiAudioSource;
//     public AudioClip warningSound; 

//     private Coroutine hideCoroutine;

//     /// <summary>
//     /// یک پیام هشدار را با متن دلخواه نمایش می‌دهد.
//     /// </summary>
//     public void ShowAlarm(string message)
//     {
//         Debug.Log($"AlarmManager ShowAlarm called with message: {message}");
//         
//         if (alarmPanel == null || alarmText == null)
//         {
//             Debug.LogError("Alarm Panel or Alarm Text is not assigned in the AlarmManager Inspector!");
//             return;
//         }

//         // ۱. اگر کوروتین قبلی در حال اجرا بود، متوقفش کن
//         if (hideCoroutine != null)
//         {
//             StopCoroutine(hideCoroutine);
//         }

//         // ۲. پخش صدا
//         if (warningSound != null && uiAudioSource != null)
//         {
//             uiAudioSource.PlayOneShot(warningSound);
//         }
//         
//         // <<< --- اصلاح کلیدی اینجاست --- >>>
//         // ۳. پنل را "قبل" از تغییر متن، غیرفعال کن
//         // این کار تضمین می‌کنه که SetActive(true) بعدی، یک "فعال‌سازی تازه" باشه
//         alarmPanel.SetActive(false); 
//         
//         // ۴. متن جدید را تنظیم کن
//         alarmText.text = message;
//         
//         // ۵. پنل را "دوباره" فعال کن
//         // (حالا یونیتی مجبور می‌شه اندازه باکس را بر اساس متن جدید محاسبه کنه)
//         alarmPanel.SetActive(true);
//         // <<< --- پایان اصلاح --- >>>

//         // ۶. کوروتین جدید را برای مخفی کردن پنل اجرا کن
//         hideCoroutine = StartCoroutine(HideAfterDelay());
//     }

//     private IEnumerator HideAfterDelay()
//     {
//         yield return new WaitForSeconds(4f);
//         alarmPanel.SetActive(false);
//         hideCoroutine = null;
//     }
// }