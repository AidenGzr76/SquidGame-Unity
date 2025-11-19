using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InputDebugger : MonoBehaviour
{
    void Update()
    {
        // با هر کلیک چپ موس یا لمس صفحه اجرا می‌شود
        if (Input.GetMouseButtonDown(0))
        {
            // --- بخش اول: بررسی برخورد با المان‌های UI ---

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();

            // یک اشعه به سمت تمام المان‌های UI در آن نقطه می‌فرستد
            EventSystem.current.RaycastAll(eventData, results);

            // اگر اشعه حداقل به یک المان UI برخورد کرده باشد
            if (results.Count > 0)
            {
                // نام اولین المانی که در بالاترین لایه قرار دارد را چاپ می‌کند
                Debug.Log("کلیک به یک المان UI برخورد کرد: " + results[0].gameObject.name, results[0].gameObject);
                
                // برای اطلاعات بیشتر، می‌توانید نام تمام المان‌های زیر آن را هم ببینید
                // for (int i = 1; i < results.Count; i++)
                // {
                //    Debug.Log("... و زیر آن قرار داشت: " + results[i].gameObject.name, results[i].gameObject);
                // }

                return; // چون به UI برخورد کرده، دیگر نیازی به بررسی دنیای بازی نیست
            }

            // --- بخش دوم: بررسی برخورد با آبجکت‌های دوبعدی در دنیای بازی ---
            
            RaycastHit2D hit2D = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit2D.collider != null)
            {
                Debug.Log("کلیک به یک آبجکت دوبعدی برخورد کرد: " + hit2D.collider.gameObject.name, hit2D.collider.gameObject);
                return;
            }

            Debug.Log("کلیک به هیچ چیز خاصی برخورد نکرد.");
        }
    }
}