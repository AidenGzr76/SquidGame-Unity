using UnityEngine;
using System.Collections;

public class GunController : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform gunTip;
    public AudioSource audioSource;

    public void AimAndShoot(Transform target, AudioClip[] shootSounds, System.Action onComplete, System.Action onHit)
    {
        StartCoroutine(ShootRoutine(target, shootSounds, onComplete, onHit));
    }

    private IEnumerator ShootRoutine(Transform target, AudioClip[] shootSounds, System.Action onComplete, System.Action onHit)
    {

        // Debug.Log($"اسلحه در حال شلیک به {target.name}");
    
        // فعال کردن لیزر و قفل روی بازیکن
        lineRenderer.enabled = true;

        float timer = 0f;
        while (timer < 0.5f) // 1 ثانیه دنبال کردن
        {
            if (target != null)
            {
                lineRenderer.SetPosition(0, gunTip.position);
                // Debug.Log(gunTip.parent.name);
                lineRenderer.SetPosition(1, target.position);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // شلیک
        if (shootSounds.Length > 0)
        {
            int index = Random.Range(0, shootSounds.Length);
            audioSource.PlayOneShot(shootSounds[index]);
        }

        yield return new WaitForSeconds(0.1f); // نیم ثانیه بعد از شلیک
        if (target != null)
        {
            // جهت از اسلحه به بازیکن
            Vector3 dir = (target.position - gunTip.position).normalized;

            // اگه PlayerController روی بازیکن باشه، این جهت رو ذخیره کن
            PlayerController pc = target.GetComponent<PlayerController>();
            if (pc != null)
                pc.SetLastShotDirection(dir);
        }

        // برخورد -> بازیکن حذف می‌شود
        onHit?.Invoke();

        // خاموش کردن لیزر
        lineRenderer.enabled = false;

        // اسلحه به لیست آزاد برمی‌گردد
        onComplete?.Invoke();
    }
}
