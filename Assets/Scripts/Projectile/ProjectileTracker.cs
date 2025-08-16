using UnityEngine;

public class ProjectileTracker : MonoBehaviour
{
    [HideInInspector] public string registryKey;

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(registryKey))
        {
            ProjectileRegistry.OnDespawned(registryKey);
        }
    }
}
