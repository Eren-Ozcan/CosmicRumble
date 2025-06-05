// Assets/Scripts/Interfaces/IDamageable.cs

/// <summary>
/// Hasar alabilen (damageable) nesneler bu interface'i implement etmeli.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Nesneye belirli miktarda hasar uygular.
    /// </summary>
    /// <param name="amount">Uygulanacak hasar miktarı</param>
    void TakeDamage(float amount);
}
