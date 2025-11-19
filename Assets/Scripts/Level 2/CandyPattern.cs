using UnityEngine;

// این اتریبیوت به یونیتی اجازه می‌دهد تا این کلاس را در Inspector نمایش دهد
[System.Serializable]
public class CandyPattern
{
    public string patternName; // برای شناسایی راحت‌تر در Inspector (مثلا: "Circle")

    [Header("Game Play Sprites")]
    public Sprite fullSprite;       // شکل کامل شیرینی که کاربر روی آن می‌کشد
    public Texture2D maskTexture;   // ماسک برای تشخیص مسیر درست

    [Header("Win State Sprites")]
    public Sprite hollowSprite;     // شکل توخالی شیرینی (برای زمان برد)
    public Sprite shapeSprite;      // خودِ شکل جدا شده (برای زمان برد)

    [Header("Lose State Sprites")]
    public Sprite crackedSprite;    // شکل ترک خورده (برای زمان باخت)
}