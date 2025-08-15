using UnityEngine;

[CreateAssetMenu(menuName = "Gravity/Gravity Config")]
public class GravityConfig : ScriptableObject
{
    public enum GravityMode
    {
        InverseSquared,
        Linear,
        Constant
    }

    [Header("Force Calculation")]
    public GravityMode mode = GravityMode.InverseSquared;
    [Tooltip("Global multiplier applied to all gravity forces.")]
    public float globalScale = 1f;
    [Tooltip("Minimum distance used when calculating gravity to avoid singularities.")]
    public float minDistance = 0.1f;
    [Tooltip("Sum forces from all sources instead of using only the strongest source.")]
    public bool useVectorSum = true;
}
