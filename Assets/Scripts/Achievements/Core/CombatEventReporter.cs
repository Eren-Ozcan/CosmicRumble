using UnityEngine;

namespace CosmicRumble.Achievements
{
    /// <summary>
    /// Projectile/ability script'lerinin hasar+headshot event'lerini tek, tutarlı yerden
    /// raporlamasını sağlar — her çarpışma noktasında AchievementEvents'i ayrı ayrı çağırmak yerine.
    /// </summary>
    public static class CombatEventReporter
    {
        /// <summary>Ayrık bir isabeti (hasar + varsa headshot) raporlar. impactPoint dünya koordinatında olmalı.</summary>
        public static void ReportHit(IDamageable target, float amount, Vector2 impactPoint)
        {
            AchievementEvents.FireDamageDealt(Mathf.Max(0, Mathf.RoundToInt(amount)));

            if (target is CharacterHealth ch && IsHeadshot(ch.transform, impactPoint))
                AchievementEvents.FireHeadshotLanded();
        }

        /// <summary>Konum bilgisi olmayan sürekli hasar (DoT vb.) için — headshot kontrolü yapmaz.</summary>
        public static void ReportDamage(float amount)
        {
            AchievementEvents.FireDamageDealt(Mathf.Max(0, Mathf.RoundToInt(amount)));
        }

        // "Head" = karakterin transform.up ekseninde üst yarısı. GravityBody, transform.up'ı her zaman
        // gezegen yüzeyinden dışa dönük tutar (bkz. GravityBody.cs), bu yüzden karakter gezegenin
        // neresinde olursa olsun "yukarısı" doğru hesaplanır.
        private static bool IsHeadshot(Transform target, Vector2 worldImpactPoint)
        {
            var col = target.GetComponent<Collider2D>();
            if (col == null) return false;

            Vector2 local = target.InverseTransformPoint(worldImpactPoint);
            float halfHeight = (col is CapsuleCollider2D capsule) ? capsule.size.y * 0.5f : col.bounds.extents.y;
            if (halfHeight <= 0f) return false;

            return local.y > halfHeight * 0.5f;
        }
    }
}
