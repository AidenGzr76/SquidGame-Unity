using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // مطمئن می‌شه Rigidbody2D وجود داره
public class SmartCash : MonoBehaviour
{
    private Rigidbody2D rb;
    private float stationaryTimer = 0f;
    private bool isSettled = false;

    // --- تنظیمات بهینه‌سازی ---
    // آستانه سرعت کمی بالاتر رفته تا زودتر تشخیص سکون بده
    private const float SLEEP_VELOCITY_THRESHOLD = 0.1f;
    // زمان لازم برای سکون کمتر شده
    private const float TIME_TO_SETTLE = 0.15f;
    // --- پایان تنظیمات ---

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // تنظیمات Rigidbody برای عملکرد بهتر (می‌تونی از Inspector هم تنظیم کنی)
        if (rb != null)
        {
            // Interpolation رو خاموش می‌کنیم (حرکت کمی پرش‌دارتر ولی سریع‌تر)
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.sleepMode = RigidbodySleepMode2D.StartAwake; // باید اول بیدار باشه تا بیفته
            // حالت تشخیص برخورد Discrete سریع‌ترینه (معمولاً پیش‌فرضه)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        }
    }

    void OnEnable()
    {
        // وقتی از Pool دوباره فعال می‌شه، حالتش رو ریست کن
        isSettled = false;
        stationaryTimer = 0f;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // دوباره فعالش کن
            rb.linearVelocity = Vector2.zero;          // سرعت قبلی رو صفر کن
            rb.angularVelocity = 0f;          // سرعت چرخش قبلی رو صفر کن
            rb.WakeUp();                         // مطمئن شو که خواب نیست
        }
        // <<< مهم: اسکریپت رو دوباره فعال کن >>>
        this.enabled = true;
    }

    void FixedUpdate() // محاسبات فیزیک بهتره در FixedUpdate باشه
    {
        // اگر از قبل ساکن شده یا Rigidbody نداره، کاری نکن
        if (isSettled || rb == null)
        {
            return;
        }

        // چک کن آیا سرعت خطی و زاویه‌ای کمتر از آستانه هست
        if (rb.linearVelocity.magnitude < SLEEP_VELOCITY_THRESHOLD && Mathf.Abs(rb.angularVelocity) < SLEEP_VELOCITY_THRESHOLD)
        {
            stationaryTimer += Time.fixedDeltaTime; // تایمر سکون رو زیاد کن
        }
        else
        {
            stationaryTimer = 0f; // اگر حرکت کرد، تایمر رو ریست کن
        }

        // اگر به اندازه کافی ساکن مونده، ساکنش کن
        if (stationaryTimer >= TIME_TO_SETTLE)
        {
            Settle();
        }
    }

    private void Settle()
    {
        isSettled = true;
        rb.bodyType = RigidbodyType2D.Kinematic; // فیزیک رو خاموش کن
        rb.linearVelocity = Vector2.zero;             // سرعت رو صفر کن
        rb.angularVelocity = 0f;             // سرعت چرخش رو صفر کن

        // <<< مهم: خود این اسکریپت رو غیرفعال کن تا دیگه FixedUpdate اجرا نشه >>>
        this.enabled = false;
    }

    // این تابع برای PreSpawnCashRoutine هنوز لازمه
    public void SettleInstantly()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) return; // با RequireComponent نباید اتفاق بیفته

        isSettled = true;
        stationaryTimer = TIME_TO_SETTLE;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        // <<< مهم: اینجا هم اسکریپت رو غیرفعال کن >>>
        this.enabled = false;
    }

    /// <summary>
    /// اسکناس را در حالت Kinematic (بدون فیزیک فعال) قرار می‌دهد.
    /// برای ایجاد اولیه اسکناس‌های قبلی استفاده می‌شود.
    /// </summary>
    public void SetKinematicInitially()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        isSettled = false; // هنوز ساکن نشده
        stationaryTimer = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic; // <<-- فقط Kinematic می‌کنیم
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        this.enabled = true; // <<-- اسکریپت باید فعال بماند
    }

    /// <summary>
    /// فیزیک اسکناس را دوباره فعال (Dynamic) می‌کند.
    /// </summary>
    public void ActivatePhysics()
    {
        if (rb == null || rb.bodyType == RigidbodyType2D.Dynamic) return; // اگر از قبل فعال بود کاری نکن

        isSettled = false;
        stationaryTimer = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.WakeUp(); // مطمئن شو که فیزیکش بیدار می‌شود
        this.enabled = true; // مطمئن شو اسکریپت فعاله
    }
}









// // SmartCash.cs
// using UnityEngine;

// public class SmartCash : MonoBehaviour
// {
//     private Rigidbody2D rb;
//     private float stationaryTimer = 0f;
//     private bool isSettled = false;
    
//     // این آستانه سرعت است. اگر سرعت کمتر از این باشد، پول در حال سکون در نظر گرفته می‌شود.
//     private const float SLEEP_VELOCITY_THRESHOLD = 0.05f;
//     // این مدت زمانی است که پول باید تقریباً بی‌حرکت بماند تا فیزیکش خاموش شود.
//     private const float TIME_TO_SETTLE = 0.2f;

//     void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();
//     }

//     void OnEnable()
//     {
//         // هر بار که پول از Pool فعال می‌شود، باید فیزیکش را دوباره روشن کنیم!
//         isSettled = false;
//         stationaryTimer = 0f;
//         if (rb != null)
//         {
//             rb.bodyType = RigidbodyType2D.Dynamic; // اطمینان از اینکه Rigidbody دوباره Dynamic است
//         }
//     }

//     void FixedUpdate() // محاسبات فیزیک بهتر است در FixedUpdate انجام شود
//     {
//         // اگر پول از قبل ساکن شده، دیگر کاری انجام نده
//         if (isSettled)
//         {
//             return;
//         }

//         // اگر سرعت حرکت و چرخش پول بسیار کم باشد...
//         if (rb.linearVelocity.magnitude < SLEEP_VELOCITY_THRESHOLD && Mathf.Abs(rb.angularVelocity) < SLEEP_VELOCITY_THRESHOLD)
//         {
//             // ... تایمر سکون را افزایش بده
//             stationaryTimer += Time.fixedDeltaTime;
//         }
//         else
//         {
//             // اگر دوباره حرکت کرد، تایمر را ریست کن
//             stationaryTimer = 0f;
//         }

//         // اگر تایمر از حد معین گذشت، یعنی پول ساکن شده است
//         if (stationaryTimer >= TIME_TO_SETTLE)
//         {
//             Settle();
//         }
//     }

//     private void Settle()
//     {
//         isSettled = true;
//         // مهم‌ترین بخش: Rigidbody را به Kinematic تغییر می‌دهیم.
//         // این کار فیزیک آن را خاموش می‌کند، اما آبجکت در جای خود باقی می‌ماند.
//         rb.bodyType = RigidbodyType2D.Kinematic;

//         // (اختیاری) برای بهینه‌سازی بیشتر، می‌توانیم خود این اسکریپت را هم غیرفعال کنیم
//         // this.enabled = false; 
//     }


//     /// <summary>
//     /// این تابع اسکناس را فوراً ساکن (Kinematic) می‌کند.
//     /// برای اسکناس‌هایی که از قبل در باکس هستند استفاده می‌شود.
//     /// </summary>
//     public void SettleInstantly()
//     {
//         if (rb == null) rb = GetComponent<Rigidbody2D>();

//         isSettled = true;
//         stationaryTimer = TIME_TO_SETTLE; // تایمر را پر می‌کند
//         rb.bodyType = RigidbodyType2D.Kinematic;
//         rb.linearVelocity = Vector2.zero;
//         rb.angularVelocity = 0;
//     }
    
// }