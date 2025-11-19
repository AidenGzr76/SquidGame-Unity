using UnityEngine;

public class ShapeEnterAnimation : MonoBehaviour
{
    public float delay = 1f;
    public float moveTime = 1f;
    public Vector3 targetPosition;

    void Start()
    {
        Vector3 startPos = new Vector3(targetPosition.x, -Screen.height, targetPosition.z);
        transform.position = startPos;

        // بعد از تاخیر، با EaseInOut به وسط حرکت کن
        LeanTween.move(gameObject, targetPosition, moveTime)
                 .setEase(LeanTweenType.easeInOutQuad)
                 .setDelay(delay);
    }
}
