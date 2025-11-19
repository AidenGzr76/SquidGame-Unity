using UnityEngine;

public class CameraFollowXY : MonoBehaviour
{
    public Transform player;      // بازیکن
    public float smoothSpeed = 0.125f;
    public Vector3 offset;        // فاصله دوربین از بازیکن

    [Header("Camera Limits")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    void LateUpdate()
    {
        Vector3 desiredPosition = player.position + offset;

        // محدود کردن حرکت روی X و Y
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        desiredPosition.z = transform.position.z; // حفظ موقعیت Z دوربین

        // حرکت نرم
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
