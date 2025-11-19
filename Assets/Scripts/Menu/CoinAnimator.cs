// CoinAnimator.cs
using UnityEngine;
using System.Collections;

public class CoinAnimator : MonoBehaviour
{
    public IEnumerator MoveToTarget(Vector3 startPos, Vector3 targetPos, float duration)
    {
        float elapsedTime = 0f;
        transform.position = startPos;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // حرکت نرم (EaseOut)
            transform.position = Vector3.Lerp(startPos, targetPos, 1 - Mathf.Pow(1 - progress, 3));

            yield return null;
        }

        // در انتها، خود را غیرفعال کن تا به Pool برگردد
        gameObject.SetActive(false);
    }
}