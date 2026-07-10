using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using Unity.Notifications;
#endif
using CosmicRumble.Localization;

namespace CosmicRumble.Notifications
{
    /// <summary>
    /// Cihaz üzerinde zamanlanan hatırlatma bildirimleri — "seri bozulacak" ve "bugünün sandıkları
    /// bekliyor". Gerçek sunucu tetiklemeli UGS Push Notifications DEĞİL: bu oyunun ekonomisi
    /// client-authoritative (bkz. TODO.md madde 22) ve hatırlatmaların ihtiyaç duyduğu tek zamanlayıcı
    /// veri (login streak, günlük sandık hakkı) zaten cihazda — sunucu tarafı bir tetikleyici
    /// gerektirmiyor, standart mobil oyun deseni budur.
    ///
    /// Yalnızca Android/iOS derlemesinde etkin — `com.unity.mobile.notifications`'ın birleşik API
    /// assembly'si (`Unity.Notifications.Unified`) yalnızca Android/iOS/Editor için derleniyor, Windows
    /// Standalone (online test için kullanılan DevClient build'i) dışarıda kalıyor; bu yüzden tüm SDK
    /// çağrıları #if ile korunuyor (STEAMWORKS_INSTALLED/GPGS_INSTALLED ile aynı desen).
    /// </summary>
    public class LocalNotificationManager : MonoBehaviour
    {
        public static LocalNotificationManager Instance { get; private set; }

#if UNITY_ANDROID || UNITY_IOS
        const string ChannelId = "cosmic_reminders";
        const int StreakNotificationId = 1001;
        const int ChestNotificationId  = 1002;
        bool _initialized;
#endif

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID || UNITY_IOS
            NotificationCenter.Initialize(new NotificationCenterArgs
            {
                PresentationOptions = NotificationPresentation.Alert | NotificationPresentation.Badge | NotificationPresentation.Sound,
                AndroidChannelId          = ChannelId,
                AndroidChannelName        = "Reminders",
                AndroidChannelDescription = "Streak and chest reminders",
            });
            _initialized = true;
            _ = NotificationCenter.RequestPermission(); // Android 13+ runtime izni; öncesi no-op
#endif
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Uygulama arka plana atıldığında (ana menüde, oyuncu ayrılırken) çağrılır.</summary>
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) ScheduleReminders();
            else CancelReminders();
        }

        void ScheduleReminders()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!_initialized) return;
            CancelReminders();

            if (Economy.LoginStreakManager.Instance != null && Economy.LoginStreakManager.Instance.GetCurrentStreak() > 0)
            {
                var n = new Notification
                {
                    Identifier = StreakNotificationId,
                    Title      = Loc.T("Don't lose your streak!"),
                    Text       = Loc.T("Play a match today to keep your login streak alive."),
                };
                // ~20 saat sonra — UTC gün değişiminden (streak'in gerçek kırılma anı) önce bir
                // hatırlatma payı bırakır, saniyeye kadar kesin hesap gerekmiyor (diğer mobil
                // oyunlardaki "yaklaşık bir gün sonra" hatırlatma deseniyle aynı).
                NotificationCenter.ScheduleNotification(n, new NotificationIntervalSchedule(System.TimeSpan.FromHours(20)));
            }

            if (Economy.ChestManager.Instance != null && Economy.ChestManager.Instance.GetRemainingChests() > 0)
            {
                var n = new Notification
                {
                    Identifier = ChestNotificationId,
                    Title      = Loc.T("Chests are waiting!"),
                    Text       = Loc.T("You still have chests to earn today — jump into a match."),
                };
                NotificationCenter.ScheduleNotification(n, new NotificationIntervalSchedule(System.TimeSpan.FromHours(4)));
            }
#endif
        }

        void CancelReminders()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!_initialized) return;
            NotificationCenter.CancelScheduledNotification(StreakNotificationId);
            NotificationCenter.CancelScheduledNotification(ChestNotificationId);
#endif
        }
    }
}
