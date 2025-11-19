using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// using System.Diagnostics;

public class PlayerController : MonoBehaviour
{
    // ... (تمام متغیرهای شما از Header تا Private Variables بدون هیچ تغییری) ...
    #region Public Variables
    [Header("Movement")]
    [field: SerializeField]
    public float moveSpeed { get; private set; } = 5f;
    [Header("Animation")]
    public Animator animator;
    [Header("Push Attack")]
    public float hitForce = 5f;
    public float hitRadius = 2f;
    public LayerMask hitLayer;
    [Header("Stamina")]
    public Slider staminaBar;
    [Header("Visuals")]
    public DollController dollController;
    public TimerController timerController;
    public Transform visualRoot;
    [Header("System Connections")]
    public UIManager uiManager;
    public ReviveManager reviveManager;
    public FloatingJoystick joystick;
    [SerializeField] private GameObject bloodPrefab;
    [Header("Invincibility Settings")]
    public float invincibilityDuration = 2f;
    [Header("Sound Effects")]
    public AudioSource effectAudioSource;
    public AudioClip walkSound;
    public AudioClip[] pushSounds;
    public AudioClip[] deathSounds;
    [Header("Debug")]
    public float MovingThreshold = 0.0001f;
    public float pushedDetectThreshold = 0.001f;
    #endregion

    [Header("Game Logic")]
    public float winDelay = 3.0f; // مهلت بعد از رسیدن به خط پایان

    #region Private Variables
    private Rigidbody2D rb;
    private CharacterBase selfController;
    private Ragdoll2D ragdoll;
    private Vector2 moveInput;
    private Vector2 lastPosition;
    private Vector3 startPosition;
    private Vector3 baseScale;
    private bool isPushing = false;
    private bool isDead = false;
    private bool isTargetedByGun = false;
    private bool isInvincible = false;
    private AudioSource walkAudioSource;
    private Coroutine speedBoostCoroutine;
    private SpriteRenderer[] visualSpriteRenderers;
    private Vector3 lastShotDirection = Vector3.zero;
    private float originalSpeed;

    // <<< این دو متغیر جدید اضافه شده‌اند >>>
    private float originalHitForce;
    private Coroutine strengthBoostCoroutine;

    #endregion


    void Start()
    {  
        rb = GetComponent<Rigidbody2D>();
        walkAudioSource = GetComponent<AudioSource>();
        selfController = GetComponent<CharacterBase>();
        

        // <<< --- ۱. این خط جدید اضافه شود (ثبت‌نام در رویداد) --- >>>
        if (selfController != null)
        {
            selfController.OnStaminaChanged += UpdateStaminaUI;
        }
        // <<< --- پایان بخش جدید --- >>>


        ragdoll = GetComponent<Ragdoll2D>();

        originalSpeed = moveSpeed;
        // <<< این خط جدید اضافه شده است >>>
        originalHitForce = hitForce;

        startPosition = transform.position;
        baseScale = visualRoot.localScale;

        if (visualRoot != null)
        {
            visualSpriteRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (reviveManager == null)
        {
            reviveManager = FindAnyObjectByType<ReviveManager>();
        }
    }

    // <<< --- ۲. این تابع جدید اضافه شود (تابع اجرا شونده) --- >>>
    /// <summary>
    /// این تابع توسط رویداد OnStaminaChanged از CharacterBase فراخوانی می‌شود.
    /// </summary>
    private void UpdateStaminaUI(float current, float max)
    {
        if (staminaBar != null)
        {
            staminaBar.value = current / max;
        }
    }
    // <<< --- پایان بخش جدید --- >>>


    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        GameManager.OnStageRespawn += Respawn;
        // (اگر selfController در Start مقداردهی شده، اینجا نیازی نیست)
    }

    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        GameManager.OnStageRespawn -= Respawn;
        
        // <<< --- ۳. این خط جدید اضافه شود (لغو ثبت‌نام) --- >>>
        if (selfController != null)
        {
            selfController.OnStaminaChanged -= UpdateStaminaUI;
        }
        // <<< --- پایان بخش جدید --- >>>
    }

    // <<< تابع Update بازنویسی شده: فقط مسئول خواندن ورودی و ظاهر است >>>
    void Update()
    {
        // ۱. ورودی را بخوان
        ReadInput();

        // ۲. ظاهر (انیمیشن، صدا، جهت) را بر اساس ورودی و وضعیت، مدیریت کن
        UpdateVisuals();

    }

    // <<< تابع FixedUpdate بازنویسی شده: فقط مسئول فیزیک است >>>
    void FixedUpdate()
    {
        // ۱. آیا بازیکن اجازه حرکت دارد؟
        bool canMove = !isDead && !isPushing && !selfController.isPushed;

        // ۲. اگر اجازه دارد، حرکت را اعمال کن
        if (canMove)
        {
            ApplyMovement();
        }

        // ۳. منطق چراغ قرمز را بررسی کن
        HandleRedLightCheck();

        // ۴. در انتها، موقعیت را برای فریم بعدی ذخیره کن
        lastPosition = rb.position;
    }

    // --- توابع کمکی بازنویسی شده ---

    private void ReadInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
#else
            moveInput.x = joystick.Horizontal;
            moveInput.y = joystick.Vertical;
#endif
    }

    private void UpdateVisuals()
    {
        // اگر بازیکن کنترلی ندارد (مرده، هل داده شده)، انیمیشن راه رفتن را متوقف کن
        bool canAnimateWalk = !isDead && !isPushing && !selfController.isPushed;
        bool isWalking = canAnimateWalk && moveInput.sqrMagnitude > 0.01f;

        if (animator != null) animator.SetBool("isWalking", isWalking);

        if (isWalking && !walkAudioSource.isPlaying)
        {
            walkAudioSource.clip = walkSound;
            walkAudioSource.loop = true;
            walkAudioSource.Play();
        }
        else if (!isWalking && walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }

        // فقط زمانی جهت را تغییر بده که بازیکن در حال حرکت است
        if (isWalking)
        {
            if (moveInput.x < -0.1f)
                visualRoot.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
            else if (moveInput.x > 0.1f)
                visualRoot.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
    }

    private void ApplyMovement()
    {
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }


    // ... (بقیه کد شما از HandleRedLightCheck تا انتها بدون تغییر باقی می‌ماند) ...
    private void HandleRedLightCheck()
    {
        if (LightManager.Instance != null && LightManager.Instance.redLight.activeSelf)
        {
            if (isDead || isInvincible) return;
            bool isMoving = (rb.position - lastPosition).sqrMagnitude > MovingThreshold;
            bool isPushed = false;

            if (!isMoving && selfController.isPushed)
            {
                isPushed = (rb.position - lastPosition).sqrMagnitude > pushedDetectThreshold;
            }
            if ((isMoving || isPushing || isPushed) && (transform.position.y < timerController.finishLine.position.y))
            {
                if (!isTargetedByGun)
                {
                    isDead = true;
                    isTargetedByGun = true;
                    GunManager.Instance.ShootAtTarget(transform, () =>
                    {
                        isTargetedByGun = false;
                        int index = Random.Range(0, deathSounds.Length);
                        effectAudioSource.PlayOneShot(deathSounds[index]);
                        ShowBlood();
                        StartCoroutine(DeathWait());
                    });
                }
            }
        }
    }
    public void SpinAttack()
    {
        // اگر بازیکن در حال هل دادن، مرده، یا استقامت کافی ندارد، کاری نکن
        if (isPushing || isDead)// || selfController.currentStamina < staminaCostPerSpin)
        {
            return;
        }

        // حالا بقیه کارهای هل دادن را انجام بده
        animator.SetTrigger("doPush");
        StartCoroutine(SpinCoroutine());
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }
    public void SetLastShotDirection(Vector3 dir)
    {
        lastShotDirection = dir;
    }
    public void ShowBlood()
    {
        if (bloodPrefab == null) return;
        Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle * 0.1f);
        pos.z = -0.35f;
        var go = Instantiate(bloodPrefab, pos, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
        float s = Random.Range(0.9f, 1.4f);
        go.transform.localScale = new Vector3(s, s, 1f);

        GetComponent<Ragdoll2D>().Die(Vector2.zero);
    }
    public void Respawn()
    {
        if (ragdoll != null) ragdoll.SetAliveState(true);
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        lastPosition = rb.position;
        isDead = false;
        isTargetedByGun = false;
        moveSpeed = originalSpeed;
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
            speedBoostCoroutine = null;
        }

        // <<< تغییر کلیدی و حیاتی اینجاست! >>>
        // به تمام سیستم‌ها (از جمله دکمه‌های فیچر) خبر بده که وضعیت باید ریست شود
        GameManager.OnStageReset?.Invoke();

        StartCoroutine(InvincibilityRoutine());
        Debug.Log("Player has respawned!");
    }
    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        moveSpeed = originalSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
        speedBoostCoroutine = null;
    }
    private IEnumerator DeathWait()
    {
        yield return new WaitForSeconds(2f);
        uiManager.ShowLosePanel();
        // if (GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
        // {
        //     if (reviveManager != null) reviveManager.ShowRevivePanel();
        //     else GameManager.Instance.StageFailed();
        // }
        // else
        // {
        //     uiManager.ShowLosePanel();
        // }
    }
    private IEnumerator PlayerWonWait()
    {
        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            yield return new WaitForSeconds(1.5f);
            uiManager.ShowWinPanel();
        }
        else
        {
            uiManager.uiAudioSource.PlayOneShot(uiManager.WinEffect);

            // <<< --- این همان مهلت (Grace Period) است که می‌خواستی --- >>>
            yield return new WaitForSeconds(winDelay);
            // <<< --- پایان بخش جدید --- >>>
        
            // <<< --- این خط جدید جایگزین میشه --- >>>
            // به داور مرحله میگیم "من بردم"، داور خودش بقیه کارها رو می‌کنه
            if (Stage1_Manager.Instance != null)
            {
                Stage1_Manager.Instance.ReportPlayerWon();
            }
        }
    }

    private IEnumerator SpinCoroutine()
    {
        isPushing = true;

        // پخش صدای هل دادن
        int index = Random.Range(0, pushSounds.Length);
        effectAudioSource.PlayOneShot(pushSounds[index]);

        // ۱. متغیری برای ذخیره بدترین نتیجه برخورد
        // 0 = خطا (Miss), 1 = برد (Win), 2 = باخت (Lose)
        int pushOutcome = 0;

        // تنظیمات محدوده هل دادن
        float pushFOV = 90f;
        float cosThreshold = Mathf.Cos(pushFOV * 0.5f * Mathf.Deg2Rad);
        Vector2 facingDir = (visualRoot.localScale.x >= 0f) ? Vector2.right : Vector2.left;

        var enemy = null as CharacterBase;

        // پیدا کردن تمام آبجکت‌های قابل برخورد در اطراف بازیکن
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, hitLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // به خودت برخورد نکن

            Rigidbody2D hitRb = hit.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                // چک کن آیا دشمن در جهت نگاه تو قرار دارد
                Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                float dot = Vector2.Dot(facingDir, toTarget);
                if (dot < cosThreshold) continue;

                enemy = hit.GetComponent<CharacterBase>() ?? hit.GetComponentInParent<CharacterBase>();
                if (enemy != null)
                {
                    // محاسبه استقامت مؤثر با در نظر گرفتن فیچر قدرت
                    float myEffectiveStamina = selfController.currentStamina;
                    if (strengthBoostCoroutine != null)
                    {
                        myEffectiveStamina += 100; // مقدار বোনাস
                    }

                    float staminaDiff = myEffectiveStamina - enemy.currentStamina;

                    // محاسبه نیروی نهایی با در نظر گرفتن فیچر قدرت
                    float force = hitForce + Mathf.Abs(staminaDiff) * 0.1f;

                    Debug.LogWarning($"PUSH ATTEMPT on {enemy.name}: My Effective Stamina={myEffectiveStamina}, Enemy Stamina={enemy.currentStamina}, Final Force={force}");

                    enemy.FacePlayer(visualRoot);

                    if (staminaDiff >= 0)
                    {
                        // شما دشمن را هل می‌دهی
                        hitRb.linearVelocity = Vector2.zero;
                        enemy.ApplyKnockback(toTarget, force);
                        
                        if (pushOutcome != 2) pushOutcome = 1;
                    }
                    else
                    {
                        // شما به عقب پرت می‌شوی
                        rb.linearVelocity = Vector2.zero;
                        selfController.ApplyKnockback(-toTarget, force);

                        // نتیجه "باخت" همیشه اولویت داره
                        pushOutcome = 2;
                    }


                    continue; // برو سراغ هدف بعدی
                }
            }
        }
        
        // --- <<< ۳. اعمال هزینه نهایی بر اساس نتیجه --- >>>
        switch (pushOutcome)
        {
            case 0: // خطا (Miss)
                selfController.TakeStaminaDamage(selfController.staminaCost_Miss);
                Debug.Log("Push Missed. Cost: " + selfController.staminaCost_Miss);
                break;
            case 1: // برد (Win)
                selfController.TakeStaminaDamage(selfController.staminaCost_WinPush);
                enemy.TakeStaminaDamage(selfController.staminaCost_LosePush);
                Debug.Log("Push Won. Cost: " + selfController.staminaCost_WinPush);
                break;
            case 2: // باخت (Lose)
                selfController.TakeStaminaDamage(selfController.staminaCost_LosePush);
                enemy.TakeStaminaDamage(selfController.staminaCost_WinPush);
                Debug.Log("Push Lost. Cost: " + selfController.staminaCost_LosePush);
                break;
        }

        // آپدیت نوار استقامت
        if (staminaBar != null)
            UpdateStaminaUI(selfController.currentStamina, selfController.maxStamina);

        // بعد از یک تاخیر کوتاه، حالت هل دادن را تمام کن
        yield return new WaitForSeconds(0.5f);
        isPushing = false;
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        StartCoroutine(BlinkingEffectRoutine());
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }
    private IEnumerator BlinkingEffectRoutine()
    {
        if (visualSpriteRenderers.Length == 0) yield break;
        while (isInvincible)
        {
            foreach (var renderer in visualSpriteRenderers) renderer.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            foreach (var renderer in visualSpriteRenderers) renderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
        foreach (var renderer in visualSpriteRenderers) renderer.color = Color.white;
    }

    public void ActivateStaminaFeature(float duration)
    {
        if (selfController != null)
        {
            // ۱. استقامت را پر کن
            selfController.RefillStamina();

            // <<< تغییر کلیدی و حیاتی اینجاست! >>>
            // ۲. بلافاصله نوار استقامت را هم آپدیت کن تا پر شدنش نمایش داده شود.
            UpdateStaminaUI(selfController.currentStamina, selfController.maxStamina);

            // ۳. حالت بی‌نهایت را فعال کن
            selfController.ActivateInfiniteStamina(duration);
        }
    }

    /// <summary>
    /// فیچر افزایش قدرت هل دادن را برای مدت زمان مشخصی فعال می‌کند.
    /// </summary>
    public void ActivateStrengthBoost(float multiplier, float duration)
    {
        if (strengthBoostCoroutine != null)
        {
            StopCoroutine(strengthBoostCoroutine);
        }
        strengthBoostCoroutine = StartCoroutine(StrengthBoostRoutine(multiplier, duration));
    }

    private IEnumerator StrengthBoostRoutine(float multiplier, float duration)
    {
        hitForce = originalHitForce * multiplier;
        Debug.Log($"Strength Boost Activated! New hit force: {hitForce}");

        yield return new WaitForSeconds(duration);

        hitForce = originalHitForce;
        Debug.Log("Strength Boost Finished. Hit force reverted to normal.");
        strengthBoostCoroutine = null;
    }


    private void HandleFeatureActivation(FeatureData data)
    {
        switch (data.type)
        {
            case FeatureData.FeatureType.SpeedBoost:
                ActivateSpeedBoost(data.value, data.duration);
                break;
            case FeatureData.FeatureType.StrengthBoost:
                ActivateStrengthBoost(data.value, data.duration);
                break;
            case FeatureData.FeatureType.StaminaBoost:
                ActivateStaminaFeature(data.duration);
                break;
        }
    }


    /// <summary>
    /// زمانی که بازیکن وارد یک Trigger می‌شود، این تابع اجرا می‌شود.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // اگر زنده هستیم، اسکریپت فعال است و وارد محوطه برد (با تگ FinishZone) شدیم
        if (!isDead && this.enabled && other.CompareTag("Finish"))
        {
            // کوروتین برد را (که حالا تاخیر دارد) اجرا کن
            StartCoroutine(PlayerWonWait());
        }
    }
    

}