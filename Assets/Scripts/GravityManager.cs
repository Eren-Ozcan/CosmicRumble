using UnityEngine;
using System.Collections.Generic;

// Awake()’ın tüm diğer Awake'lerden önce çalışması için
[DefaultExecutionOrder(-100)]
public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance { get; private set; }
    public List<GravitySource> sources = new List<GravitySource>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Eğer sahneler arası kalmasını istersen:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterSource(GravitySource src)
    {
        if (!sources.Contains(src))
            sources.Add(src);
    }

    public void UnregisterSource(GravitySource src)
    {
        sources.Remove(src);
    }
}
