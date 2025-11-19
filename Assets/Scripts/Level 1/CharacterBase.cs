using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System;

public class CharacterBase : MonoBehaviour
{
    public event Action<float, float> OnStaminaChanged;

    [Header("Stamina")]
    public float maxStamina = 100f; // << فرض می‌کنم maxStamina از اینجا خوانده شود
    public float currentStamina;
    public float staminaRegenTime = 3f;
    public float staminaRegenValue = 10f;
    [Tooltip("هزینه استقامت اگر به هیچکس نخورید")]
    public float staminaCost_Miss = 15f; 
    [Tooltip("هزینه استقامت اگر دشمن را هل بدهید (برد)")]
    public float staminaCost_WinPush = 30f; 
    [Tooltip("هزینه استقامت اگر دشمن شما را هل بدهد (باخت)")]
    public float staminaCost_LosePush = 20f;
    [Tooltip("آسیب استقامتی که این کاراکتر به دیگران وارد می‌کند")] // <<< این متغیر مهم بود
    public float staminaDamageToOthers = 15f;

    [Header("Knockback")]
    // <<< --- این خط جدید اضافه شده است --- >>>
    [SerializeField]
    [Tooltip("آیا این کاراکتر توسط NavMeshAgent کنترل می‌شود؟ (برای دشمنان تیک بزنید)")]
    private bool isNavMeshAgentControlled = false;
    
    public float knockbackScale = 1.0f;
    public float stopAfterSeconds = 0.25f;
    public bool topDownNoGravity = true;
    public float dragOnHit = 0.5f;
    public float angularDragOnHit = 0.5f;

    [Header("Animation")]
    public Animator animator;
    public string pushedBool = "pushed";
    public string pushingBoolName = "doPush";

    [Header("Visual Facing")]
    public Transform visualRoot;
    private Rigidbody2D rb;
    private Coroutine stopCo;
    private Coroutine regenCo;
    [SerializeField]
    public bool isPushed = false;

    private bool isStaminaInfinite = false;
    private Coroutine staminaBoostCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // <<< --- این بخش جدید اضافه شده است --- >>>
        // تنظیم نوع Rigidbody بر اساس نوع کاراکتر
        if (isNavMeshAgentControlled)
        {
            // اگر دشمن (AI) است، باید Kinematic باشد
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            // اگر بازیکن است، باید Dynamic باشد تا به دیوارها برخورد کند
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        // <<< --- پایان بخش جدید --- >>>
        
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!visualRoot) visualRoot = transform;

        currentStamina = maxStamina;

        if (topDownNoGravity)
        {
            rb.gravityScale = 0f;
        }

        if (staminaRegenTime > 0f) // <<< اصلاح شد: استفاده از متغیر درست
            regenCo = StartCoroutine(RegenLoop());

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    void OnDisable()
    {
        if (regenCo != null) StopCoroutine(regenCo);
    }

    IEnumerator RegenLoop()
    {
        var w = new WaitForSeconds(staminaRegenTime); // <<< اصلاح شد: استفاده از متغیر درست
        while (true)
        {
            if (currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenValue); // <<< اصلاح شد
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }
            yield return w;
        }
    }

    public void ApplyKnockback(Vector2 dir, float force)
    {
        rb.bodyType = RigidbodyType2D.Dynamic; // پویا کن تا پرت بشه
        rb.WakeUp();
        rb.linearDamping = dragOnHit;
        rb.angularDamping = angularDragOnHit;
        rb.AddForce(dir.normalized * (force * knockbackScale), ForceMode2D.Impulse);

        if (animator)
        {
            animator.SetBool(pushedBool, true);
        }

        if (stopCo != null) StopCoroutine(stopCo);
        stopCo = StartCoroutine(StopAfter(stopAfterSeconds));
    }

    IEnumerator StopAfter(float t)
    {
        isPushed = true;
        yield return new WaitForSeconds(t);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // <<< --- این بخش کامل اصلاح شد --- >>>
        // چک کن که بعد از توقف باید به چه حالتی برگردد
        if (isNavMeshAgentControlled)
        {
            // دشمن به Kinematic برمی‌گردد تا AI کنترلش کند
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            // بازیکن به Dynamic برمی‌گردد تا به دیوارها برخورد کند
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        // <<< --- پایان اصلاح --- >>>
        

        if (animator) animator.SetBool(pushedBool, false);
        isPushed = false;
    }

    public void TakeStaminaDamage(float amount)
    {
        if (isStaminaInfinite) return;
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
    
    public void FacePlayer(Transform pusherTransform)
    {
        if (pusherTransform == null) return;
        Vector3 PusherScale = pusherTransform.localScale;
        Vector3 getPushedScale = visualRoot != null ? visualRoot.localScale : transform.localScale;
        if ((PusherScale.x > 0 && getPushedScale.x > 0) || (PusherScale.x < 0 && getPushedScale.x < 0))
        {
            getPushedScale.x = -Mathf.Abs(getPushedScale.x);
        }
        if (visualRoot != null) visualRoot.localScale = getPushedScale;
        else transform.localScale = getPushedScale;
    }

    public void RefillStamina()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        Debug.Log("Stamina refilled!");
    }

    public void ActivateInfiniteStamina(float duration)
    {
        if (staminaBoostCoroutine != null) StopCoroutine(staminaBoostCoroutine);
        staminaBoostCoroutine = StartCoroutine(InfiniteStaminaRoutine(duration));
    }

    private IEnumerator InfiniteStaminaRoutine(float duration)
    {
        isStaminaInfinite = true;
        yield return new WaitForSeconds(duration);
        isStaminaInfinite = false;
        staminaBoostCoroutine = null;
    }
}