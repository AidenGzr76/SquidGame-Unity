using UnityEngine;

public class CameraFollowX : MonoBehaviour
{
    public Transform player;      // بازیکن
    public float smoothSpeed = 0.125f; // نرمی حرکت
    public Vector3 offset;        // فاصله دوربین از بازیکن

    public float minX; // حداقل حرکت افقی
    public float maxX; // حداکثر حرکت افقی

    void LateUpdate()
    {
        // <<< --- این خط حیاتی را اضافه کنید --- >>>
        if (player == null)
        {
            return; // اگر بازیکنی هنوز تنظیم نشده، کاری نکن
        }
        

        // موقعیت هدف با احتساب offset
        Vector3 desiredPosition = player.position + offset;

        // فقط محور X رو محدود کن
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

        // Y و Z رو از موقعیت فعلی دوربین بگیر که ثابت بمونه
        desiredPosition.y = transform.position.y;
        desiredPosition.z = transform.position.z;

        // حرکت نرم به سمت موقعیت هدف
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
