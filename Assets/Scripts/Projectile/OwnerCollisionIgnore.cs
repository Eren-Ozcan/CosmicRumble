using UnityEngine;
using System.Collections;

/// <summary>
/// Temporarily ignores collisions between a projectile and its owner,
/// restoring them after a real-time delay or when destroyed.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OwnerCollisionIgnore : MonoBehaviour
{
    Collider2D col;
    Collider2D[] ownerColliders;
    Coroutine restoreRoutine;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Ignore collisions with all colliders on the owner for the given duration.
    /// </summary>
    public void Ignore(GameObject owner, float duration)
    {
        if (owner == null || col == null) return;
        ownerColliders = owner.GetComponentsInChildren<Collider2D>();
        foreach (var oc in ownerColliders)
            Physics2D.IgnoreCollision(col, oc, true);
        if (restoreRoutine != null)
            StopCoroutine(restoreRoutine);
        restoreRoutine = StartCoroutine(RestoreAfter(duration));
    }

    IEnumerator RestoreAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Restore();
    }

    /// <summary>
    /// Re-enable collisions with previously ignored owner colliders.
    /// </summary>
    public void Restore()
    {
        if (ownerColliders == null || col == null) return;
        foreach (var oc in ownerColliders)
            if (oc != null)
                Physics2D.IgnoreCollision(col, oc, false);
        ownerColliders = null;
        restoreRoutine = null;
    }

    void OnDestroy()
    {
        Restore();
    }
}
