using UnityEngine;

namespace CosmicRumble.Gravity
{
    /// <summary>
    /// Yerçekimi ivmesi hesaplama stratejisi.
    /// GravityManager, sahne gezegenlerine göre doğru stratejiyi seçer.
    /// </summary>
    public interface IGravityStrategy
    {
        /// <summary>
        /// Verilen dünya konumunda net yerçekimi ivmesini döner (m/s²).
        /// GravityBody.FixedUpdate ve TrajectoryDots.Show bu değeri kullanır.
        /// </summary>
        Vector2 CalculateAcceleration(Vector2 fromPosition);
    }
}
