using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SimpleEnemy))]
[RequireComponent(typeof(Rigidbody2D))]
public class MarblesAI : MonoBehaviour
{
    private enum AIState { Wandering, Chasing, Pushing, Fleeing }
    private AIState currentState = AIState.Wandering;

    [Header("AI Behaviour")]
    public float wanderSpeed = 1.5f;
    public float chaseSpeed = 3f;
    [Tooltip("حداکثر سرعت فرار (با استقامت کامل)")]
    public float maxFleeSpeed = 4f;
    [Tooltip("حداقل سرعت فرار (با استقامت کم)")]
    public float minFleeSpeed = 1f;
    public float lowStaminaThreshold = 30f;
    public float visionRadius = 5f;
    public float pushDistance = 1f;
    public float decisionInterval = 0.5f;

    [Header("Push Attack")]
    [Range(0, 1)]
    public float pushChance = 0.7f;
    public float pushForce = 15f;
    public float staminaDamageOnPush = 10f;
    [Tooltip("زمان استراحت بعد از هر هل دادن (ثانیه)")]
    public float pushCooldown = 3.0f;

    [Header("Wandering Behaviour")]
    [Tooltip("هر چند ثانیه جهت گشت و گذار عوض شود")]
    public float wanderDirectionChangeInterval = 2.0f;

    // --- بخش جدید: آگاهی از مرزهای زمین ---
    // --- بخش آگاهی از مرزها اصلاح شد ---
    [Header("Boundary Awareness")]
    [Tooltip("کلایدری که محدوده زمین بازی را مشخص می‌کند")]
    public Collider2D arenaBoundary;
    [Tooltip("قدرت نیروی بازگرداننده از لبه‌ها")]
    public float boundaryForce = 5f;

    [Header("Components")]
    public LayerMask characterLayer;

    // --- Private ---
    private Rigidbody2D rb;
    private SimpleEnemy selfController;
    private Transform currentTarget;
    private Vector2 wanderDirection;
    private float nextWanderDirectionTime = 0f;
    private float nextDecisionTime = 0f;
    private Coroutine pushCoroutine;
    private float nextPushAvailableTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfController = GetComponent<SimpleEnemy>();
    }

    void Update()
    {
        if (!selfController.isAlive || selfController.isPushed)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time > nextDecisionTime)
        {
            DecideNextAction();
            nextDecisionTime = Time.time + decisionInterval;
        }
    }

    void FixedUpdate()
    {
        // اگر زنده نیستیم یا هل داده شده‌ایم، هیچ حرکتی نکن
        if (!selfController.isAlive || selfController.isPushed)
        {
            return;
        }

        // در هر فریم فیزیک، وضعیت فعلی را اجرا می‌کنیم
        ExecuteCurrentState();
    }

    // مغز اصلی هوش مصنوعی
    void DecideNextAction()
    {
        // ۱. پیدا کردن نزدیک‌ترین کاراکتر در شعاع دید
        currentTarget = FindClosestCharacter();

        // ۲. تصمیم‌گیری بر اساس استقامت و وجود هدف
        if (selfController.currentStamina < lowStaminaThreshold)
        {
            currentState = AIState.Fleeing; // استقامت کمه، فرار کن!
        }
        else if (currentTarget != null)
        {
            currentState = AIState.Chasing; // هدف هست، تعقیب کن!
        }
        else
        {
            currentState = AIState.Wandering; // هدفی نیست، گشت بزن!
        }
    }

    // اجرای حرکت بر اساس وضعیت فعلی
    void ExecuteCurrentState()
    {
        Vector2 desiredVelocity = Vector2.zero;
        switch (currentState)
        {
            case AIState.Wandering:
                desiredVelocity = GetWanderVelocity();
                break;
            case AIState.Chasing:
                desiredVelocity = GetChaseVelocity();
                break;
            case AIState.Fleeing:
                desiredVelocity = GetFleeVelocity();
                break;
        }

        // --- بخش جدید: اعمال نیروی بازگرداننده از لبه‌ها ---
        Vector2 boundaryAvoidance = CalculateBoundaryAvoidance();
        desiredVelocity += boundaryAvoidance * boundaryForce;

        // سرعت نهایی را به ریجیدبادی اعمال می‌کنیم
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * 10f);

        UpdateVisuals(rb.linearVelocity);
    }

    Vector2 GetWanderVelocity()
    {
        if (Time.time > nextWanderDirectionTime)
        {
            wanderDirection = Random.insideUnitCircle.normalized;
            nextWanderDirectionTime = Time.time + wanderDirectionChangeInterval + Random.Range(-0.5f, 0.5f);
        }

        // ... (منطق Wander اصلاح شده از قبل) ...
        return wanderDirection * wanderSpeed;
    }

    Vector2 GetChaseVelocity()
    {
        if (currentTarget == null) { currentState = AIState.Wandering; return Vector2.zero; }
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distanceToTarget > pushDistance)
        {
            Vector2 direction = (currentTarget.position - transform.position).normalized;
            return direction * chaseSpeed;
        }
        else
        {
            // --- بخش جدید: چک کردن Cooldown ---
            if (Time.time > nextPushAvailableTime && Random.value < pushChance && pushCoroutine == null)
            {
                pushCoroutine = StartCoroutine(PushCoroutine(currentTarget));
            }
            return Vector2.zero;
        }
    }

    Vector2 GetFleeVelocity()
    {
        if (currentTarget != null)
        {
            // --- بخش جدید: سرعت فرار وابسته به استقامت ---
            float staminaPercent = selfController.currentStamina / selfController.maxStamina;
            float currentFleeSpeed = Mathf.Lerp(minFleeSpeed, maxFleeSpeed, staminaPercent);

            Vector2 direction = (transform.position - currentTarget.position).normalized;
            return direction * currentFleeSpeed;
        }
        else
        {
            currentState = AIState.Wandering;
            return Vector2.zero;
        }
    }

    // --- این تابع به طور کامل بازنویسی شده است ---
    Vector2 CalculateBoundaryAvoidance()
    {
        if (arenaBoundary == null) return Vector2.zero;

        // نزدیک‌ترین نقطه روی مرز کلایدر به موقعیت فعلی ما
        Vector2 closestPointOnBoundary = arenaBoundary.ClosestPoint(rb.position);

        // اگر فاصله ما از نزدیک‌ترین نقطه روی مرز، خیلی کم باشد، یعنی داخل محدوده هستیم
        if (Vector2.Distance(rb.position, closestPointOnBoundary) < 0.01f)
        {
            return Vector2.zero; // هیچ نیرویی لازم نیست
        }
        else
        {
            // اگر فاصله وجود داشت، یعنی ما خارج از محدوده هستیم.
            // یک بردار از موقعیت فعلی ما به سمت نزدیک‌ترین نقطه روی مرز ایجاد می‌کنیم.
            return (closestPointOnBoundary - rb.position).normalized;
        }
    }

    private IEnumerator PushCoroutine(Transform target)
    {
        currentState = AIState.Pushing;

        Vector2 direction = (target.position - transform.position).normalized;
        UpdateVisuals(direction);

        if (selfController.animator)
            selfController.animator.SetTrigger("doPush");

        yield return new WaitForSeconds(0.2f);

        if (target != null)
        {
            // تلاش برای پیدا کردن هر دو نوع کنترلر روی هدف
            var targetEnemyController = target.GetComponent<SimpleEnemy>();
            var playerController = target.GetComponent<PlayerSquidController>();

            if (targetEnemyController != null)
            {
                selfController.TakeStaminaDamage(staminaDamageOnPush / 2); // خودمان هم کمی استامینا از دست می‌دهیم
                targetEnemyController.TakeStaminaDamage(staminaDamageOnPush);
                targetEnemyController.ApplyKnockback(direction, pushForce, transform);
            }
            else if (playerController != null)
            {
                selfController.TakeStaminaDamage(staminaDamageOnPush / 2);
                playerController.TakeStaminaDamage(staminaDamageOnPush);
                // فرض می‌کنیم شما یک تابع ApplyKnockback به بازیکن خود اضافه کرده‌اید
                playerController.ApplyKnockback(direction, pushForce);
                // Debug.Log("Pushing the player!");
            }
        }

        yield return new WaitForSeconds(0.5f);

        // --- بخش جدید: تنظیم Cooldown ---
        nextPushAvailableTime = Time.time + pushCooldown;
        pushCoroutine = null;
        currentState = AIState.Wandering;
    }

    Transform FindClosestCharacter()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRadius, characterLayer);
        Transform closest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = hit.transform;
            }
        }
        return closest;
    }

    void UpdateVisuals(Vector2 velocity)
    {
        if (selfController.visualRoot == null) return;

        if (velocity.x > 0.1f)
            selfController.visualRoot.localScale = new Vector3(1, 1, 1);
        else if (velocity.x < -0.1f)
            selfController.visualRoot.localScale = new Vector3(-1, 1, 1);

        if (selfController.animator)
            selfController.animator.SetBool("isWalking", velocity.sqrMagnitude > 0.01f);
    }

}