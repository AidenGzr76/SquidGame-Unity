using UnityEngine;

[CreateAssetMenu(fileName = "New Feature", menuName = "Squid Game/Feature Data")]
public class FeatureData : ScriptableObject
{
    // <<< این بخش جدید اضافه شده است >>>
    public enum FeatureType
    {
        SpeedBoost,     // افزایش سرعت
        StaminaBoost,  // افزایش استقامت
        StrengthBoost, // افزایش قدرت

        // <<< این سه خط جدید اضافه شده‌اند >>>
        ChangeDalgonaShape,      // تغییر شکل دالگونا
        AddTimeDalgona,          // افزایش زمان دالگونا
        ErrorMarginBoostDalgona,  // کاهش درصد خطای دالگونا


        // <<< این سه خط جدید اضافه شده‌اند >>>
        TugOfWar_PowerBoost,     // قدرت بیشتر در هر کلیک
        TugOfWar_PowerLock,      // قفل شدن قدرت روی ۱۰۰٪
        TugOfWar_DecayResist,     // کاهش سرعت خالی شدن قدرت

        // <<< این سه خط جدید اضافه شده‌اند >>>
        Marbles_RemoveOption,      // حذف یک گزینه در حدس زوج/فرد
        Marbles_AddTime,           // افزایش زمان


        // <<< این دو خط جدید اضافه شده‌اند >>>
        GlassBridge_RemoveWrong,   // حذف پلتفرم شکستنی
        GlassBridge_AddTime,        // افزایش زمان


        Stage6_StrengthBoost,    // افزایش قدرت پرش در مرحله ۶
        Stage6_StaminaBoost,     // افزایش استقامت در مرحله ۶



    }

    [Header("Feature Logic")]
    [Tooltip("این فیچر از چه نوعی است؟")]
    public FeatureType type;

    [Tooltip("مقدار تاثیر فیچر (مثلاً برای سرعت، ضریب افزایش سرعت)")]
    public float value;
    // <<< پایان بخش جدید >>>

    [Header("Main Feature Info")]
    public string featureName = "نام فیچر";
    public string activationMessage = "سرعت افزایش یافت!";
    
    [Tooltip("پیام هشداری که اگر بازیکن دکمه را زود رها کند، نمایش داده می‌شود.")]
    [TextArea] // این باعث می‌شود کادر متن در Inspector بزرگتر و چند خطی باشد
    public string holdWarningMessage = "برای فعال‌سازی باید دکمه را نگه دارید!";
    // public Sprite icon;
    public int cost = 50;
    public float duration = 10f;
    public float cooldown = 20f;

    [Header("Sound Effects")]
    public AudioClip activationSound;
    public AudioClip cooldownFinishSound;
    public AudioClip holdStartSound;
}
