using UnityEngine;
using System.Collections;
using UnityEngine.UI; // <<< --- ۱. این خط جدید اضافه شود --- >>>

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class SimpleEnemy : MonoBehaviour
{
    // <<< --- ۲. این بخش جدید اضافه شود --- >>>
    [Header("UI")]
    [Tooltip("اسلایدر استقامت این دشمن را اینجا بکشید")]
    public Slider staminaSlider;
    // <<< --- پایان بخش جدید --- >>>

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = -1f;
    public float staminaRegenRate = 5f;
    public float staminaRegenInterval = 3f;

    [Header("Knockback")]
    public float knockbackScale = 1.0f;
    public float stopAfterSeconds = 0.5f;

    [Header("Red Light Kill")]
    public bool obeyRedLight = true;
    public float moveDetectThreshold = 0.0001f;

    [Header("Death / Ragdoll")]
    public Ragdoll2D ragdoll;

    [Header("Animation")]
    public Animator animator;
    public string pushedBool = "pushed";

    [Header("Visual Facing")]
    public Transform visualRoot;
    private Vector3 baseScale;

    [Header("Blood")]
    public GameObject bloodPrefab;

    // --- Public State ---
    public bool isAlive { get; private set; } = true;
    public bool isPushed { get; private set; } = false;

    // --- Private ---
    private Rigidbody2D rb;
    private MarblesAI aiController; // رفرنس به اسکریپت هوش مصنوعی
    private Coroutine stopCo;
    private Coroutine regenCo;
    private Vector2 lastPos; // برای منطق چراغ قرمز (اگرچه AI آن را کنترل می‌کند)
    // private bool isTargetedByGun = false;
    private bool isDead = false;
    private Vector3 lastShotDirection = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        aiController = GetComponent<MarblesAI>();
        if (currentStamina < 0f) currentStamina = maxStamina;
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!visualRoot) visualRoot = transform;
        baseScale = visualRoot.localScale;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // <<< --- ۳. این بخش جدید اضافه شود --- >>>
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
        // <<< --- پایان بخش جدید --- >>>
    }
    
    void Start()
    {
        // رجیستر کردن دشمن در مدیر دشمنان
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(this);
        }
    }

    void OnEnable()
    {
        if (staminaRegenRate > 0f)
            regenCo = StartCoroutine(RegenLoop());
    }

    void OnDisable()
    {
        if (regenCo != null) StopCoroutine(regenCo);
    }

    IEnumerator RegenLoop()
    {
        var w = new WaitForSeconds(staminaRegenInterval);
        while (true)
        {
            if (currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate);

                // <<< --- ۵. این خط جدید اضافه شود --- >>>
                UpdateStaminaUI();
            }
            yield return w;
        }
    }
    
    public void ApplyKnockback(Vector2 dir, float force, Transform pusherTransform = null)
    {
        // اگر دشمن زنده نیست یا از قبل در حال هل داده شدن است، کاری نکن
        if (!isAlive || isPushed) return;

        // هوش مصنوعی را موقتاً غیرفعال می‌کنیم تا دخالت نکند
        if (aiController != null) 
            aiController.enabled = false;
        
        // ریجیدبادی را برای دریافت نیرو آماده می‌کنیم
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.AddForce(dir.normalized * (force * knockbackScale), ForceMode2D.Impulse);
        
        if (animator) animator.SetBool(pushedBool, true);
        if (pusherTransform != null) FacePlayer(pusherTransform);

        // کوروتین توقف را شروع می‌کنیم
        if (stopCo != null) StopCoroutine(stopCo);
        stopCo = StartCoroutine(StopAfter(stopAfterSeconds));
    }

    IEnumerator StopAfter(float t)
    {
        isPushed = true;
        yield return new WaitForSeconds(t);
        rb.linearVelocity = Vector2.zero;
        
        // ریجیدبادی را به حالت سینماتیک برمی‌گردانیم تا هوش مصنوعی دوباره کنترل را به دست بگیرد
        rb.bodyType = RigidbodyType2D.Kinematic; 
        
        if (animator) animator.SetBool(pushedBool, false);
        isPushed = false;

        // هوش مصنوعی را دوباره فعال می‌کنیم
        if (aiController != null && isAlive) 
            aiController.enabled = true;
        
        stopCo = null;
    }

    public void TakeStaminaDamage(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);

        // <<< --- ۶. این خط جدید اضافه شود --- >>>
        UpdateStaminaUI();
    }

    public void FacePlayer(Transform playerTransform)
    {
        if (playerTransform == null || visualRoot == null) return;
        
        // این تابع باید بر اساس موقعیت مکانی عمل کند
        float directionX = playerTransform.position.x - transform.position.x;
        Vector3 myScale = visualRoot.localScale;

        if (directionX > 0) // هل‌دهنده در سمت راست است، پس به راست نگاه کن
            myScale.x = Mathf.Abs(baseScale.x);
        else // هل‌دهنده در سمت چپ است، پس به چپ نگاه کن
            myScale.x = -Mathf.Abs(baseScale.x);
            
        visualRoot.localScale = myScale;
    }

    public void OnKilledByGun()
    {
        if (isDead) return;
        isDead = true;

        // هوش مصنوعی را برای همیشه غیرفعال کن
        if (aiController != null)
            aiController.enabled = false;

        // از مدیر دشمنان حذف شو
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(this);

        GunManager.Instance.ShootAtTarget(transform, () =>
        {
            ShowBlood();
        });
    }

    public void ShowBlood()
    {
        if (bloodPrefab == null) return;

        float distance = Random.Range(-1f, -1.5f);
        Vector3 offset = (-lastShotDirection).normalized * distance;

        offset += new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.05f, 0.05f),
            0f
        );

        GetComponent<Ragdoll2D>().Die(offset);

        Vector3 bloodPos = transform.position + offset;
        bloodPos.z = -0.35f;

        GameObject blood = Instantiate(bloodPrefab, bloodPos, Quaternion.identity);

        float randomScale = Random.Range(0.9f, 1.4f);
        blood.transform.localScale = new Vector3(randomScale, randomScale, 1f);

        float randomRotation = Random.Range(0f, 360f);
        blood.transform.rotation = Quaternion.Euler(0, 0, randomRotation);
    }

    public void SetLastShotDirection(Vector3 dir)
    {
        lastShotDirection = dir;
    }
    

    // <<< --- ۴. این تابع کامل جدید است --- >>>
    /// <summary>
    /// مقدار اسلایدر استقامت را به‌روزرسانی می‌کند.
    /// </summary>
    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }
}

