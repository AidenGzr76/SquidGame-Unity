// File: CharacterMapping.cs
using UnityEngine;

/// <summary>
/// این کلاس برای Inspector استفاده می‌شود.
/// این کلاس به اسکریپت Spawner هر مرحله کمک می‌کند
/// تا ID را به Prefab مربوط به "همان" مرحله مرتبط کند.
/// </summary>
[System.Serializable]
public class CharacterMapping
{
    [Tooltip("ID کاراکتر (مثلاً: 1 برای بازیکن، 2 تا 10 برای دشمنان)")]
    public int characterID;
    
    [Tooltip("Prefab این کاراکتر، *مخصوص این مرحله*")]
    public GameObject characterPrefab;
}