using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace CosmicRumble.Networking
{
    /// <summary>
    /// Unity Multiplayer Services (Session API, Relay üzerinden) + Netcode for GameObjects
    /// köprüsü. UGS init/sign-in'i CloudSaveManager'ın kurduğu aynı oturumu kullanır, tekrar
    /// başlatmaz. Gerçek satın alma/host/join akışı OnlineLobbyPanelUI'dan çağrılır.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        public static NetworkBootstrap Instance { get; private set; }

        public string LastJoinCode { get; private set; }
        public bool IsBusy { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        async Task EnsureUgsReadyAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>Bir Relay oturumu oluşturur, host olarak başlar. Başarılıysa katılım kodunu döner.</summary>
        public async Task<string> HostSessionAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureUgsReadyAsync();

                var options = new SessionOptions { MaxPlayers = 2 }.WithRelayNetwork();
                var session = await MultiplayerService.Instance.CreateSessionAsync(options);

                LastJoinCode = session.Code;
                Debug.Log($"[NET] Hosted session, code={LastJoinCode}, IsHost={NetworkManager.Singleton.IsHost}");
                return LastJoinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NET] HostSessionAsync failed: {e}");
                return null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Verilen katılım koduyla mevcut bir oturuma bağlanır (client olarak).</summary>
        public async Task<bool> JoinSessionAsync(string code)
        {
            IsBusy = true;
            try
            {
                await EnsureUgsReadyAsync();

                var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, new JoinSessionOptions());
                Debug.Log($"[NET] Joined session code={code}, IsClient={NetworkManager.Singleton.IsClient}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NET] JoinSessionAsync failed: {e}");
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
