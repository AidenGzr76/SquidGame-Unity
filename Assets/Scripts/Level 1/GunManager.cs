using UnityEngine;
using System.Collections.Generic;

public class GunManager : MonoBehaviour
{
    public static GunManager Instance;

    public List<GunController> allGuns = new List<GunController>();
    private List<GunController> availableGuns = new List<GunController>();
    public AudioClip[] shootSounds;

    private void Awake()
    {
        Instance = this;
        availableGuns.AddRange(allGuns); // کپی اولیه
    }

    public void ShootAtTarget(Transform target, System.Action onHit)
    {
        if (availableGuns.Count == 0) return;

        int index = Random.Range(0, availableGuns.Count);
        GunController gun = availableGuns[index];

        availableGuns.RemoveAt(index);

        gun.AimAndShoot(target, shootSounds,
            () => availableGuns.Add(gun), // onComplete
            onHit // onHit -> حذف بازیکن
        );
    }
}
