using UnityEngine;
using CosmicRumble.Gravity;

// Awake()'ın tüm diğer Awake'lerden önce çalışması için
[DefaultExecutionOrder(-100)]
public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance { get; private set; }

    /// <summary>
    /// Aktif yerçekimi stratejisi. GravityBody ve TrajectoryDots bu property'yi kullanır.
    /// RefreshStrategy() ile sahne gezegenlerine göre yeniden seçilir.
    /// </summary>
    public IGravityStrategy Strategy { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        RefreshStrategy();
    }

    /// <summary>
    /// GravitySource.AllSources sayısına göre stratejiyi (yeniden) seçer.
    /// Gezegen sayısı değiştiğinde (spawn/destroy) çağrılabilir.
    /// </summary>
    public void RefreshStrategy()
    {
        var sources = GravitySource.AllSources;
        if (sources.Count == 1)
            Strategy = new SinglePlanetGravity(sources[0]);
        else
            Strategy = new MultiPlanetGravity();
    }
}
