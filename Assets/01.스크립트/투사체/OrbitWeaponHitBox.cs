using System.Collections.Generic;
using UnityEngine;

public class OrbitWeaponHitBox : MonoBehaviour
{
    private readonly HashSet<IDamageable> damagedTargets = new();

    private OrbitWeapon orbitWeapon;

    public void Init(OrbitWeapon orbitWeapon)
    {
        this.orbitWeapon = orbitWeapon;
        damagedTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (orbitWeapon == null)
            return;

        IDamageable target = orbitWeapon.GetDamageTarget(other);

        if (target == null)
            return;

        if (damagedTargets.Contains(target))
            return;

        if (!orbitWeapon.TryDamageTarget(target))
            return;

        damagedTargets.Add(target);
    }

    private void OnTriggerExit(Collider other)
    {
        if (orbitWeapon == null)
            return;

        IDamageable target = orbitWeapon.GetDamageTarget(other);

        if (target == null)
            return;

        damagedTargets.Remove(target);
    }

    private void OnDisable()
    {
        damagedTargets.Clear();
    }
}