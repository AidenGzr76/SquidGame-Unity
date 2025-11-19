using UnityEngine;
using System.Collections;

public class QueueWalker : MonoBehaviour
{
    [Header("Queue Movement")]
    [Tooltip("سرعت حرکت کاراکتر در صف")]
    public float moveSpeed = 3f;

    [Header("Components")]
    [Tooltip("انیماتور کاراکتر برای پخش انیمیشن راه رفتن")]
    public Animator animator;
    [Tooltip("نام پارامتر بولین برای انیمیشن راه رفتن")]
    public string walkingBoolName = "isWalking";

    private Coroutine moveCoroutine;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    // این تابع توسط QueueManager صدا زده می‌شود
    public void MoveToSlot(Transform targetSlot)
    {
        // اگر در حال حرکت به سمت جایگاه قبلی بود، آن را متوقف کن
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        // حرکت به سمت جایگاه جدید را شروع کن
        moveCoroutine = StartCoroutine(MoveCoroutine(targetSlot.position));
    }

    private IEnumerator MoveCoroutine(Vector3 targetPosition)
    {
        if (animator != null) animator.SetBool(walkingBoolName, true);

        // تا زمانی که به مقصد نرسیده‌ای، به حرکت ادامه بده
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // برای اطمینان، دقیقاً در نقطه مقصد قرار بگیر
        transform.position = targetPosition;
        
        if (animator != null) animator.SetBool(walkingBoolName, false);
    }
}