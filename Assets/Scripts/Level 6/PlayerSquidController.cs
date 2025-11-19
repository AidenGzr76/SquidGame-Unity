using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // لازم برای List
using System.Linq; // لازم برای LINQ
using System; // لازم برای Action
using TMPro; // لازم برای TextMeshPro (اگر استفاده می‌شود)
using Random = UnityEngine.Random;

public class PlayerSquidController : MonoBehaviour
{
    #region Public Variables
    [Header("Movement")]
    public float moveSpeed = 5f;
    public Animator animator;

    [Header("Spin Attack")]
    public float hitForce = 5f;
    public float hitRadius = 2f;
    public LayerMask hitLayer;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaCostPerSpin = 20f;
    public float staminaRegenRate = 1f;
    public float staminaRegenInterval = 3f;
    public float staminaDamageToOthers = 10f;
    public Slider staminaBar;

    [Header("Visuals")]
    public float moveRotationSpeed = 180f;
    public TimerController timerController; // مرجع کنترلر تایمر    

    [Header("Knockback")]
    public float knockbackScale = 1.0f;
    public float stopAfterSeconds = 0.25f;
    public bool topDownNoGravity = true;
    public float dragOnHit = 0.5f;
    public float angularDragOnHit = 0.5f;

    [Header("Animation")]
    public string pushedBool = "pushed";   // اسم پارامتر بولین
    
    [Header("Sounds")]
    private AudioSource walkAudioSource;
    public AudioSource effectAudioSource;
    public AudioClip walkSound;
    public AudioClip[] pushSounds;
    public AudioClip[] deathSounds;

    [Header("System Connections")]
    public UIManager uiManager;
    public FloatingJoystick joystick; // به جوی‌استیک وصل کن
    public Transform visualRoot; // همون آبجکت فرزند که همه بون‌ها و رندررها داخلشن
    #endregion

    #region Private Variables
    private Rigidbody2D rb;
    private bool isPushing = false; // آیا در حال هل دادن هستیم؟
    public bool isPushed = false; // <<-- آیا هل داده شده‌ایم؟
    private Coroutine stopCo; // <<-- کوروتین برای توقف بعد از ضربه
    private Vector2 moveInput;
    public float currentStamina;
    private Vector2 lastPosition;
    private bool isTargetedByGun = false;
    [SerializeField] private GameObject bloodPrefab;
    private Vector3 lastShotDirection = Vector3.zero;
    private Vector3 baseScale; // مقیاس اولیه برای حفظ اندازه
    private Vector3 startPosition; // <<< اضافه شد: برای ریست

    // --- متغیرهای جدید برای مدیریت فیچرها ---
    private float originalHitForce;
    private float originalStaminaRegenRate;
    private bool isStaminaInfinite = false;
    private Coroutine strengthBoostCoroutine;
    private Coroutine staminaBoostCoroutine;
    private Coroutine staminaRegenCoroutine; // برای مدیریت کوروتین
    private Ragdoll2D ragdoll; // <<< --- این خط جدید اضافه شود --- >>>
    #endregion

    #region Unity Lifecycle & Event Handling
    // <<< اضافه شد: گوش دادن به اعلان‌های GameManager >>>
    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        GameManager.OnStageRespawn += Respawn; // به سیگنال ریست گوش بده
    }

    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        GameManager.OnStageRespawn -= Respawn; // لغو ثبت‌نام
    }
    #endregion

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        walkAudioSource = GetComponent<AudioSource>();
        ragdoll = GetComponent<Ragdoll2D>(); // <<< --- این خط جدید اضافه شود --- >>>

        // <<< اصلاح شد: ذخیره مقادیر اولیه برای ریست >>>
        currentStamina = maxStamina;
        baseScale = visualRoot.localScale;
        startPosition = transform.position;
        originalHitForce = hitForce;
        originalStaminaRegenRate = staminaRegenRate;


        // <<< --- این بخش جدید اضافه شده است --- >>>
        // خودمان را به UIManager (یا مدیر مرحله) معرفی می‌کنیم
        if (uiManager != null)
        {
            uiManager.RegisterPlayer(this); // تابع جدیدی که در UIManager می‌سازیم
        }
        else
        {
            // سعی کن UIManager را پیدا کنی اگر از Inspector وصل نشده بود
            UIManager foundUIManager = FindAnyObjectByType<UIManager>();
            if (foundUIManager != null)
            {
                uiManager = foundUIManager;
                uiManager.RegisterPlayer(this);
            }
            else
            {
                Debug.LogError("UIManager در PlayerSquidController پیدا یا وصل نشده!");
            }
        }
        // <<< --- پایان بخش جدید --- >>>
     


        // <<< اصلاح شد: تابع ریست را صدا می‌زنیم تا همه چیز تنظیم شود >>>
        Respawn();
    }

    void Update()
    {
        // <<< اضافه شد: اگر بازیکن هل داده شده یا مرده، ورودی را نگیر >>>
        if (isPushed || isTargetedByGun)
        {
            moveInput = Vector2.zero;
            // انیمیشن راه رفتن را متوقف کن
            if (animator != null) animator.SetBool("isWalking", false);
            if (walkAudioSource.isPlaying) walkAudioSource.Stop();
            return;
        }

        if (!isPushing)
        {
            #if UNITY_EDITOR || UNITY_STANDALONE
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            #else
            moveInput.x = joystick.Horizontal;
            moveInput.y = joystick.Vertical;
            #endif

            if (moveInput.x != 0 || moveInput.y != 0)
            {
                if (animator != null)
                    animator.SetBool("isWalking", true);

                if (!walkAudioSource.isPlaying)
                {
                    walkAudioSource.clip = walkSound;
                    walkAudioSource.loop = true;
                    walkAudioSource.Play();
                }
            }
            else
            {
                if (animator != null)
                    animator.SetBool("isWalking", false);

                if (walkAudioSource.isPlaying)
                {
                    walkAudioSource.Stop();
                    walkAudioSource.loop = false;
                }
            }

            if (moveInput.x < 0)
                visualRoot.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
            else if (moveInput.x > 0)
                visualRoot.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
        else
        {
            moveInput = Vector2.zero;
        }

        if (staminaBar != null)
            staminaBar.value = currentStamina / maxStamina;
    }

    void FixedUpdate()
    {
        if (!isPushing && !isPushed)
            rb.linearVelocity = moveInput.normalized * moveSpeed;

        lastPosition = rb.position;
    }


    #region Feature Activation Logic
    // <<< این بخش کاملاً جدید است >>>

    /// <summary>
    /// به رویداد فعال شدن فیچر از GameManager گوش می‌دهد.
    /// </summary>
    private void HandleFeatureActivation(FeatureData data)
    {
        // آیا بازیکن در حال انجام کار مهمی است؟
        // if (isPushing || isPushed || isTargetedByGun)
        if (isTargetedByGun)
        {
            Debug.Log("Cannot use feature while busy (pushing, pushed, or dead).");
            return;
        }

        switch (data.type)
        {
            // فیچر استقامت (پر کردن و قفل کردن)
            case FeatureData.FeatureType.Stage6_StaminaBoost:
                ActivateStaminaFeature(data.duration);
                break;
            
            // فیچر قدرت (افزایش نیروی هل دادن)
            case FeatureData.FeatureType.Stage6_StrengthBoost:
                ActivateStrengthBoost(data.value, data.duration);
                break;
        }
    }

    /// <summary>
    /// فیچر استقامت (پر کردن و قفل کردن موقت) را فعال می‌کند.
    /// </summary>
    public void ActivateStaminaFeature(float duration)
    {
        // ۱. استقامت را پر کن
        currentStamina = maxStamina;
        // ۲. بلافاصله نوار استقامت را آپدیت کن
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;
        }
        
        // ۳. حالت بی‌نهایت (قفل) را فعال کن
        if (staminaBoostCoroutine != null) StopCoroutine(staminaBoostCoroutine);
        staminaBoostCoroutine = StartCoroutine(InfiniteStaminaRoutine(duration));
    }

    private IEnumerator InfiniteStaminaRoutine(float duration)
    {
        isStaminaInfinite = true;
        Debug.Log("Infinite Stamina Activated!");
        yield return new WaitForSeconds(duration);
        isStaminaInfinite = false;
        Debug.Log("Infinite Stamina Finished.");
    }

    /// <summary>
    /// فیچر افزایش قدرت هل دادن را برای مدت زمان مشخصی فعال می‌کند.
    /// </summary>
    public void ActivateStrengthBoost(float multiplier, float duration)
    {
        if (strengthBoostCoroutine != null) StopCoroutine(strengthBoostCoroutine);
        strengthBoostCoroutine = StartCoroutine(StrengthBoostRoutine(multiplier, duration));
    }

    private IEnumerator StrengthBoostRoutine(float multiplier, float duration)
    {
        hitForce = originalHitForce * multiplier;
        Debug.Log($"Strength Boost Activated! New hit force: {hitForce}");
        yield return new WaitForSeconds(duration);
        hitForce = originalHitForce;
        Debug.Log("Strength Boost Finished.");
        strengthBoostCoroutine = null;
    }
    #endregion

    #region Core Gameplay Logic
    public void OnKilledByGun()
    {
        if (isTargetedByGun) return; // <<< اضافه شد: جلوگیری از مرگ چندباره

        isTargetedByGun = true;
        GunManager.Instance.ShootAtTarget(transform, () =>
        {
            ShowBlood();
            int index = Random.Range(0, deathSounds.Length);
            effectAudioSource.PlayOneShot(deathSounds[index]);
            StartCoroutine(DeathWait());
            // isTargetedByGun = false; // <<< این خط باید بعد از ریست شدن true شود
        });
        TimeController timer = FindFirstObjectByType<TimeController>();
        // timer.StopTimer();
    }

    public void SpinAttack()
    {
        if (!isPushing && currentStamina >= staminaCostPerSpin && !isTargetedByGun) // <<< اضافه شد: چک کردن مرگ
        {
            animator.SetTrigger("doPush");
            StartCoroutine(SpinCoroutine());
            
            // <<< اصلاح شد: فقط اگر فیچر فعال نیست، استقامت کم شود >>>
            if (!isStaminaInfinite)
            {
                TakeStaminaDamage(staminaCostPerSpin); // به جای کم کردن مستقیم
            }
        }
    }

    private IEnumerator SpinCoroutine()
    {
        isPushing = true;
        int index = Random.Range(0, pushSounds.Length);
        effectAudioSource.PlayOneShot(pushSounds[index]);
        float pushFOV = 90f;
        float cosThreshold = Mathf.Cos(pushFOV * 0.5f * Mathf.Deg2Rad);
        Vector2 facingDir = (visualRoot.localScale.x >= 0f) ? Vector2.right : Vector2.left;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, hitLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            Rigidbody2D hitRb = hit.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                float dot = Vector2.Dot(facingDir, toTarget);
                if (dot < cosThreshold) continue;

                var enemy = hit.GetComponent<SimpleEnemy>() ?? hit.GetComponentInParent<SimpleEnemy>();
                if (enemy != null)
                {
                    float staminaDiff = currentStamina - enemy.currentStamina;
                    
                    // <<< اضافه شد: اگر فیچر قدرت فعال است، به اختلاف استقامت اضافه کن >>>
                    if (strengthBoostCoroutine != null)
                    {
                        staminaDiff += 50; // مقدار বোনাস (می‌توانید این را هم در FeatureData بگذارید)
                        Debug.Log("Strength Boost is active! Applying stamina bonus.");
                    }

                    Vector2 direction = toTarget;
                    
                    // <<< اصلاح شد: از hitForce (که ممکن است تقویت شده باشد) استفاده کن >>>
                    float force = hitForce + Mathf.Abs(staminaDiff) * 0.1f; 

                    if (!isStaminaInfinite) // <<< اصلاح شد: فقط اگر فیچر فعال نیست، استقامت کم شود
                    {
                        TakeStaminaDamage(staminaDamageToOthers / 2f);
                    }

                    if (staminaDiff >= 0)
                    {
                        hitRb.linearVelocity = Vector2.zero;
                        enemy.ApplyKnockback(direction, force, visualRoot.transform);
                        enemy.TakeStaminaDamage(staminaDamageToOthers);
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                        ApplyKnockback(-direction, force);
                        enemy.TakeStaminaDamage(staminaDamageToOthers / 2f);
                    }
                    continue;
                }
            }
        }
        isPushing = false;
        yield return null;
    }

    public void TakeStaminaDamage(float amount)
    {
        // <<< اصلاح شد: چک کردن فیچر استقامت >>>
        if (isStaminaInfinite) return; // اگر فیچر فعال است، آسیب نبین
        
        currentStamina = Mathf.Max(currentStamina - amount, 0f);
    }

    public void ApplyKnockback(Vector2 dir, float force)
    {
        if (stopCo != null) StopCoroutine(stopCo);
        rb.WakeUp();
        rb.AddForce(dir.normalized * force, ForceMode2D.Impulse);
        if (animator) animator.SetBool("pushed", true);
        stopCo = StartCoroutine(StopAfter(0.5f));
    }

    private IEnumerator StopAfter(float t)
    {
        isPushed = true;
        yield return new WaitForSeconds(t);
        rb.linearVelocity = Vector2.zero;
        if (animator) animator.SetBool("pushed", false);
        isPushed = false;
    }

    public void FacePlayer(Transform playerTransform)
    {
        if (playerTransform == null) return;
        Vector3 playerScale = playerTransform.localScale;
        Vector3 myScale = visualRoot != null ? visualRoot.localScale : transform.localScale;
        if (playerScale.x > 0)
            myScale.x = Mathf.Abs(myScale.x);
        else
            myScale.x = -Mathf.Abs(myScale.x);
        if (visualRoot != null)
            visualRoot.localScale = myScale;
        else
            transform.localScale = myScale;
    }

    private IEnumerator RegenerateStamina()
    {
        while (true)
        {
            // <<< اصلاح شد: اگر فیچر فعال است، استقامت را پر نگه دار >>>
            if (isStaminaInfinite)
            {
                currentStamina = maxStamina;
            }
            else if (!isPushing && currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(currentStamina + staminaRegenRate, maxStamina);
            }
            yield return new WaitForSeconds(staminaRegenInterval);
        }
    }

    /// <summary>
    /// تابع ریست بازیکن که توسط سیگنال OnStageReset صدا زده می‌شود.
    /// </summary>
    public void Respawn() // پارامتر اضافه شد تا در Start خطایی ندهد
    {
        // <<< --- این خط جدید اضافه شده است (مثل مرحله ۱) --- >>>
        if (ragdoll != null) ragdoll.SetAliveState(true);

        StopAllCoroutines(); // مهم: تمام کوروتین‌های قبلی (فیچرها، هل دادن، ...) را متوقف کن

        // ریست کردن موقعیت و ظاهر
        // فقط اگر بار اول نیست، به موقعیت شروع برگردان (چون در Start، موقعیت اولیه درست است)
        transform.position = startPosition;
        visualRoot.localScale = baseScale;


        // <<< --- این بخش جدید اضافه شده است --- >>>
        // به AreaExitWatcher (اگر وجود دارد) می‌گوییم که فلگ افتادن را ریست کند
        if (AreaExitWatcher.Instance != null)
        {
            AreaExitWatcher.Instance.ResetPlayerFallState();
        }
        // <<< --- پایان بخش جدید --- >>>
        


        // ریست کردن فیزیک
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        // ریست کردن وضعیت‌های داخلی
        isPushing = false;
        isPushed = false;
        isTargetedByGun = false;
        moveInput = Vector2.zero;
        
        // ریست کردن فیچرها
        hitForce = originalHitForce;
        staminaRegenRate = originalStaminaRegenRate;
        isStaminaInfinite = false;
        strengthBoostCoroutine = null; // کوروتین‌ها را هم null کن
        staminaBoostCoroutine = null;

        // ریست کردن استقامت
        currentStamina = maxStamina;
        if (staminaBar != null) staminaBar.value = 1f; // نوار را پر کن

        // ریست کردن انیمیشن
        if (animator)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("pushed", false);
            animator.ResetTrigger("doPush");
            animator.Play("Idle", 0, 0f); // << اجبار به پخش انیمیشن Idle از ابتدا
        }
        
        // اطمینان از فعال بودن
        gameObject.SetActive(true);
        this.enabled = true; // اسکریپت را فعال کن

        // شروع مجدد کوروتین‌های ضروری
        staminaRegenCoroutine = StartCoroutine(RegenerateStamina());
    }
    
    #endregion

    #region Coroutines & Visuals
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
    public void SetLastShotDirection(Vector3 dir)
    {
        lastShotDirection = dir;
    }
    public void ShowBlood()
    {
        if (bloodPrefab == null) return;
        
        // <<< اضافه شد: چک کردن وجود کامپوننت Ragdoll2D >>>
        Ragdoll2D ragdoll = GetComponent<Ragdoll2D>();
        if (ragdoll != null)
        {
            float distance = Random.Range(-1f, -1.5f);
            Vector3 offset = (-lastShotDirection).normalized * distance;
            offset += new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0f);
            ragdoll.Die(offset); // <<< اجرای رگدال
            Vector3 bloodPos = transform.position + offset;
            bloodPos.z = -0.35f;
            GameObject blood = Instantiate(bloodPrefab, bloodPos, Quaternion.identity);
            float randomScale = Random.Range(0.9f, 1.4f);
            blood.transform.localScale = new Vector3(randomScale, randomScale, 1f);
            float randomRotation = Random.Range(0f, 360f);
            blood.transform.rotation = Quaternion.Euler(0, 0, randomRotation);
        }
        else
        {
            Debug.LogError("Ragdoll2D component not found on player!", gameObject);
        }
    }

    private IEnumerator DeathWait()
    {
        // this.enabled = false; // <<< اضافه شد: اسکریپت را غیرفعال کن
        yield return new WaitForSeconds(2f);
        uiManager.ShowLosePanel();
    }
    private IEnumerator PlayerWonWait()
    {
        this.enabled = false; // <<< اضافه شد: اسکریپت را غیرفعال کن
        uiManager.uiAudioSource.PlayOneShot(uiManager.WinEffect);
        yield return new WaitForSeconds(2f);
        uiManager.ShowWinPanel();
    }
    #endregion
}