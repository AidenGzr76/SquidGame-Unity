using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;      // بازیکن (این توسط Spawner تنظیم می‌شود)
    public float smoothSpeed = 0.125f;
    public Vector3 offset;        

    public float minY; 
    public float maxY; 

    void LateUpdate()
    {
        // <<< --- خط حیاتی و جدید --- >>>
        // اگر هنوز بازیکنی تنظیم نشده (چون هنوز اسپاون نشده)، هیچ کاری نکن
        if (player == null)
        {
            return; 
        }
        // <<< --- پایان خط جدید --- >>>

        Vector3 desiredPosition = player.position + offset;

        // محدود کردن حرکت عمودی
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

        // برای حرکت نرم
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}









// using UnityEngine;

// public class CameraFollow : MonoBehaviour
// {
//     public Transform player;      // بازیکن
//     public float smoothSpeed = 0.125f;
//     public Vector3 offset;        // فاصله دوربین از بازیکن

//     public float minY; // حداقل ارتفاع دوربین (برای جایی که عروسک هست)
//     public float maxY; // حداکثر ارتفاع دوربین

//     void LateUpdate()
//     {
//         Vector3 desiredPosition = player.position + offset;

//         // محدود کردن حرکت عمودی
//         desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

//         // برای حرکت نرم
//         Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
//         transform.position = smoothedPosition;
//     }
    
// }
