using UnityEngine;

namespace CosmicRumble.Gravity
{
    /// <summary>
    /// Çoklu gezegen için vektörel yerçekimi stratejisi.
    /// TrajectoryDots.Show() içindeki döngü ile aynı formül.
    /// GravitySource.AllSources canlı listesini kullanır — dinamik spawn/destroy desteklenir.
    /// </summary>
    public sealed class MultiPlanetGravity : IGravityStrategy
    {
        private const float EPS = 1e-4f;

        public Vector2 CalculateAcceleration(Vector2 fromPosition)
        {
            var sources = GravitySource.AllSources;
            Vector2 acc = Vector2.zero;

            for (int i = 0; i < sources.Count; i++)
            {
                var src = sources[i];
                if (!src) continue;

                Vector2 to = (Vector2)src.transform.position - fromPosition;
                if (to.sqrMagnitude < EPS) continue;

                acc += to.normalized * src.gravityForce;
            }

            return acc;
        }
    }
}
