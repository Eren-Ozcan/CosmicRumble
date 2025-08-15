using UnityEngine;

/// <summary>
/// Calculates net gravity on a point from all active GravitySources.
/// </summary>
public static class GravitySolver
{
    /// <summary>
    /// Returns the net gravity vector acting on a position.
    /// </summary>
    public static Vector2 Calculate(Vector2 position, GravityConfig config)
    {
        GravityConfig.GravityMode mode = (config != null) ? config.mode : GravityConfig.GravityMode.InverseSquared; // null-safe
        float globalScale = (config != null) ? config.globalScale : 1f;
        float minDist = (config != null) ? config.minDistance : 0.1f;
        bool useSum = (config != null) ? config.useVectorSum : true;

        Vector2 net = Vector2.zero;
        float strongest = float.MinValue;
        Vector2 strongestVec = Vector2.zero;

        foreach (var source in GravitySource.AllSources)
        {
            Vector2 dir = (Vector2)source.transform.position - position;
            float dist = dir.magnitude;
            if (dist > source.scaledRadius)
                continue;

            float clamped = Mathf.Max(dist, minDist);
            float force = source.scaledGravityForce;
            switch (mode)
            {
                case GravityConfig.GravityMode.Linear:
                    force /= clamped;
                    break;
                case GravityConfig.GravityMode.InverseSquared:
                    force /= clamped * clamped;
                    break;
                case GravityConfig.GravityMode.Constant:
                    // force remains constant
                    break;
            }

            Vector2 contribution = dir.normalized * force;

            if (useSum)
            {
                net += contribution;
            }
            else
            {
                float sqMag = contribution.sqrMagnitude;
                if (sqMag > strongest)
                {
                    strongest = sqMag;
                    strongestVec = contribution;
                }
            }
        }

        if (!useSum)
            net = strongestVec;

        return net * globalScale;
    }
}

