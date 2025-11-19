using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PlayerUnit : MonoBehaviour
{
    public int power;
    public Animator animator;
    public Gender gender;
    public bool isFalling = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        ResetState();
    }

    public void FallIntoPit()
    {
        if (isFalling) return;
        isFalling = true;
        power = 0;
        transform.SetParent(null);
        StartCoroutine(FallAnimationRoutine());
        AudioClip screamSound = TugOfWarSoundManager.Instance.GetRandomScream(gender);
        if (screamSound != null) audioSource.PlayOneShot(screamSound);
    }

    private IEnumerator FallAnimationRoutine()
    {
        if (animator != null) animator.SetTrigger("Fall");
        float duration = 1.5f;
        Vector3 startScale = transform.localScale;
        float timer = 0f;
        while (timer < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        StopAllCoroutines();

        isFalling = false;
        gameObject.SetActive(true);

        if (animator != null)
        {
            animator.ResetTrigger("Fall");
            animator.SetBool("Pulling", true);

            // <<< --- این خط جدید اضافه شده است --- >>>
            // انیماتور را مجبور کن به حالت Idle (یا هر حالت پایه‌ای که داری) برگردد
            // animator.Play("Idle", 0, 0f); // (اگر اسم حالت Idle شما فرق دارد، آن را جایگزین کنید)
            // <<< --- پایان بخش جدید --- >>>
        }

        StartCoroutine(UpdatePowerLoop());
        StartCoroutine(PlayStrainingSoundLoop());

        Debug.Log(gameObject.name + " has been reset.");
    }

    IEnumerator UpdatePowerLoop()
    {
        while (!isFalling)
        {
            power = Random.Range(50, 101);
            if (animator != null) animator.SetBool("Pulling", true);
            yield return new WaitForSeconds(Random.Range(0.5f, 1f));
        }
    }

    IEnumerator PlayStrainingSoundLoop()
    {
        AudioClip mySound = TugOfWarSoundManager.Instance.GetRandomStrainingSound();
        if (mySound == null) yield break;
        while (!isFalling)
        {
            float randomDelay = Random.Range(1f, 6f);
            yield return new WaitForSeconds(randomDelay);
            if (!isFalling)
            {
                audioSource.PlayOneShot(mySound);
            }
        }
    }
}
