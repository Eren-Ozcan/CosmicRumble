#if CR_AUTOTEST
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using CosmicRumble.Data;
using CosmicRumble.Networking;
using Debug = UnityEngine.Debug;

namespace CosmicRumble.AutoTest
{
    /// <summary>
    /// CR_AUTOTEST-only headless bot: command-line driven host/join/crash, for host migration
    /// load tests without human clicking. N processes launched from Bash with -logFile, each one
    /// self-drives login/lobby/match-start and writes PASS/FAIL lines tagged [AUTOTEST] — a
    /// harness reads the logs, no screenshots/clicks needed.
    ///
    /// Command line:
    ///   -autohost N   host a session, wait for N total players, start the match.
    ///   -autojoin CODE  join an existing session by code.
    ///   -autokill S   after S seconds, hard-kill this process (Process.Kill) to simulate a
    ///                 crash/force-close for reconnect/host-migration testing.
    ///
    /// Never compiled into real builds — CR_AUTOTEST is only ever passed via BuildDevClientTool's
    /// extraScriptingDefines, never persisted in ProjectSettings.
    /// </summary>
    public class AutoTestBot : MonoBehaviour
    {
        const string Tag = "[AUTOTEST]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (UnityEngine.Object.FindFirstObjectByType<AutoTestBot>() != null) return;
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-autohost") && !args.Contains("-autojoin")) return;

            var go = new GameObject("AutoTestBot");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<AutoTestBot>();
        }

        int    _requiredPlayers = 2;
        string _joinCode;
        bool   _isHost;
        float  _killAt  = -1f;
        float  _leaveAt = -1f;

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-autohost":
                        _isHost = true;
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int n))
                            _requiredPlayers = Mathf.Max(2, n);
                        break;
                    case "-autojoin":
                        if (i + 1 < args.Length) _joinCode = args[i + 1];
                        break;
                    case "-autokill":
                        if (i + 1 < args.Length && float.TryParse(args[i + 1], out float s)) _killAt = s;
                        break;
                    case "-autoleave":
                        if (i + 1 < args.Length && float.TryParse(args[i + 1], out float l)) _leaveAt = l;
                        break;
                }
            }

            Debug.Log($"{Tag} boot isHost={_isHost} requiredPlayers={_requiredPlayers} joinCode={_joinCode} " +
                      $"killAt={_killAt} leaveAt={_leaveAt}");

            if (_killAt  >= 0f) StartCoroutine(KillTimer());
            if (_leaveAt >= 0f) StartCoroutine(LeaveTimer());
            StartCoroutine(Run());
        }

        IEnumerator KillTimer()
        {
            yield return new WaitForSecondsRealtime(_killAt);
            Debug.Log($"{Tag} autokill firing at {_killAt}s, force-closing process");
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// -autoleave: process'i öldürmek yerine oturumdan DÜZGÜN ayrılır. Çöken host ile
        /// bilerek çıkan host, Lobby açısından farklı iki yol: çöken host'un üyeliği ancak
        /// hareketsizlik zaman aşımıyla düşerken (ölçüldü: 108 s, bkz. docs/HOST_MIGRATION_PLAN.md)
        /// düzgün ayrılma üyeliği anında kaldırır, yani yeni host seçimi de anında olmalı.
        /// Bu ikinci yolun gerçekten migration ürettiğini ölçmek için gerekli.
        /// </summary>
        IEnumerator LeaveTimer()
        {
            yield return new WaitForSecondsRealtime(_leaveAt);
            Debug.Log($"{Tag} autoleave firing at {_leaveAt}s, leaving the session gracefully");
            var task = NetworkBootstrap.Instance.LeaveSessionAsync();
            while (!task.IsCompleted) yield return null;
            Debug.Log($"{Tag} autoleave done");
        }

        IEnumerator Run()
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            while (AuthManager.Instance == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            while ((AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn) &&
                   Time.realtimeSinceStartup < deadline)
            {
                LoginScreenUI.Instance?.AutoContinueAsGuest();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
            {
                Debug.LogError($"{Tag} FAIL login timeout");
                yield break;
            }
            Debug.Log($"{Tag} login ok");

            if (_isHost) yield return HostFlow();
            else if (!string.IsNullOrEmpty(_joinCode)) yield return JoinFlow();
        }

        IEnumerator HostFlow()
        {
            LobbyData.SelectedMode   = _requiredPlayers == 2 ? GameModeType.Duel1v1 : GameModeType.Ffa;
            LobbyData.FfaPlayerCount = Mathf.Clamp(_requiredPlayers, GameModeCatalog.MinFfaPlayers, 8);

            var hostTask = NetworkBootstrap.Instance.HostSessionAsync();
            while (!hostTask.IsCompleted) yield return null;

            string code = hostTask.Result;
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError($"{Tag} FAIL host session setup failed");
                yield break;
            }
            Debug.Log($"{Tag} HOST_CODE={code}");

            float deadline = Time.realtimeSinceStartup + 120f;
            while (NetworkManager.Singleton.ConnectedClientsIds.Count < _requiredPlayers &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            int connected = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (connected < _requiredPlayers)
            {
                Debug.LogError($"{Tag} FAIL only {connected}/{_requiredPlayers} joined in time");
                yield break;
            }

            Debug.Log($"{Tag} all {_requiredPlayers} players joined, starting match");
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
            Debug.Log($"{Tag} PASS match started");
        }

        IEnumerator JoinFlow()
        {
            var joinTask = NetworkBootstrap.Instance.JoinSessionAsync(_joinCode);
            while (!joinTask.IsCompleted) yield return null;

            if (!joinTask.Result)
            {
                Debug.LogError($"{Tag} FAIL join failed code={_joinCode}");
                yield break;
            }
            Debug.Log($"{Tag} PASS joined code={_joinCode}");
        }
    }
}
#endif
