// File: CharacterIdentity.cs
using UnityEngine;

/// <summary>
/// این اسکریپت ساده به Prefab تمام بازیکنان و دشمنان متصل می‌شود
/// تا به گیم منیجر بگوید که این کاراکتر کدام ID را دارد.
/// </summary>
public class CharacterIdentity : MonoBehaviour
{
    // این ID توسط اسکریپت Spawner در هر مرحله تنظیم می‌شود
    public int playerID; 
}