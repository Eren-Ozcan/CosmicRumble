using UnityEngine;

namespace CosmicRumble.Gravity
{
    /// <summary>
    /// Tek gezegen için sabit kuvvetli yerçekimi stratejisi.
    /// GravitySource.OnTriggerStay2D ile aynı formül (forceMag = gravityForce).
    /// </summary>
    public sealed class SinglePlanetGravity : IGravityStrategy
    {
        private readonly GravitySource _planet;

        public SinglePlanetGravity(GravitySource planet)
        {
            _planet = planet;
        }

        public Vector2 CalculateAcceleration(Vector2 fromPosition)
        {
            if (_planet == null) return Vector2.zero;

            Vector2 to = (Vector2)_planet.transform.position - fromPosition;
            if (to.sqrMagnitude < 1e-4f) return Vector2.zero;

            return to.normalized * _planet.gravityForce;
        }
    }
}
