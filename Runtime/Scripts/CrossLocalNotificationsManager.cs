using Kyub.LocalNotification.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
using Kyub.LocalNotification.Android;
#elif UNITY_IOS && !UNITY_EDITOR
using Kyub.LocalNotification.iOS;
#endif
using UnityEngine;

namespace Kyub.LocalNotification
{
    /// <summary>
    /// Global notifications manager that serves as a wrapper for multiple platforms' notification systems.
    /// </summary>
    public class CrossLocalNotificationsManager : Singleton<CrossLocalNotificationsManager>
    {
        #region Helper Classes

        [System.Flags]
        public enum GenericPlatformModeEnum { Editor = 1, Build = 2 }

        [Flags]
        public enum OperatingMode
        {
            /// <summary>
            /// Do not perform any queueing at all. All notifications are scheduled with the operating system
            /// immediately.
            /// </summary>
            NoQueue = 0x00,

            /// <summary>
            /// <para>
            /// Queue messages that are scheduled with this manager.
            /// No messages will be sent to the operating system until the application is backgrounded.
            /// </para>
            /// <para>
            /// If badge numbers are not set, will automatically increment them. This will only happen if NO badge numbers
            /// for pending notifications are ever set.
            /// </para>
            /// </summary>
            Queue = 0x01,

            /// <summary>
            /// When the application is foregrounded, clear all pending notifications.
            /// </summary>
            ClearOnForegrounding = 0x02,

            /// <summary>
            /// After clearing events, will put future ones back into the queue if they are marked with <see cref="PendingNotification.Reschedule"/>.
            /// </summary>
            /// <remarks>
            /// Only valid if <see cref="ClearOnForegrounding"/> is also set.
            /// </remarks>
            RescheduleAfterClearing = 0x04,

            /// <summary>
            /// Combines the behaviour of <see cref="Queue"/> and <see cref="ClearOnForegrounding"/>.
            /// </summary>
            QueueAndClear = Queue | ClearOnForegrounding,

            /// <summary>
            /// <para>
            /// Combines the behaviour of <see cref="Queue"/>, <see cref="ClearOnForegrounding"/> and
            /// <see cref="RescheduleAfterClearing"/>.
            /// </para>
            /// <para>
            /// Ensures that messages will never be displayed while the application is in the foreground.
            /// </para>
            /// </summary>
            QueueClearAndReschedule = Queue | ClearOnForegrounding | RescheduleAfterClearing,
        }

        #endregion

        #region Consts

        // Default filename for notifications serializer
        private const string DefaultFilename = "notifications.bin";

        // Minimum amount of time that a notification should be into the future before it's queued when we background.
        private static readonly TimeSpan MinimumNotificationTime = new TimeSpan(0, 0, 2);

        #endregion

        #region Private Variables
        [SerializeField, Tooltip("Activate Generic Platform on Unsupported Runtime Platforms")]
        protected GenericPlatformModeEnum m_genericPlatformMode = GenericPlatformModeEnum.Editor | GenericPlatformModeEnum.Build;

        [SerializeField, Tooltip("Auto Initialize Notification Center OnStart")]
        protected bool m_autoInitialize = true;

        [SerializeField, Tooltip("The operating mode for the notifications manager.")]
        protected OperatingMode m_mode = OperatingMode.QueueClearAndReschedule;

        [SerializeField, Tooltip(
            "Check to make the notifications manager automatically set badge numbers so that they increment.\n" +
            "Schedule notifications with no numbers manually set to make use of this feature.")]
        protected bool m_autoBadging = true;

        // Flag set when we're in the foreground
        protected bool _inForeground = true;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a scheduled local notification is delivered while the app is in the foreground.
        /// </summary>
        public static event Action<PendingNotification> LocalNotificationDelivered;

        /// <summary>
        /// Event fired when a queued local notification is cancelled because the application is in the foreground
        /// when it was meant to be displayed.
        /// </summary>
        /// <seealso cref="OperatingMode.Queue"/>
        public static event Action<PendingNotification> LocalNotificationExpired;

        #endregion

        #region Properties

        /// <summary>
        /// Auto-Initilize Platform Service OnStart
        /// </summary>
        public bool AutoInitialize { get { return m_autoInitialize; } set { m_autoInitialize = value; } }

        /// <summary>
        /// Gets the implementation of the notifications for the current platform;
        /// </summary>
        public ICrossLocalNotificationsPlatform Platform { get; private set; }

        /// <summary>
        /// Gets a collection of notifications that are scheduled or queued.
        /// </summary>
        public List<PendingNotification> PendingNotifications { get; private set; } = new List<PendingNotification>();

        /// <summary>
        /// Gets or sets the serializer to use to save pending notifications to disk if we're in
        /// <see cref="OperatingMode.RescheduleAfterClearing"/> mode.
        /// </summary>
        public IPendingNotificationsSerializer Serializer { get; set; }

        /// <summary>
        /// Gets the operating mode for this manager.
        /// </summary>
        /// <seealso cref="OperatingMode"/>
        public OperatingMode Mode => m_mode;

        /// <summary>
        /// Gets whether this manager automatically increments badge numbers.
        /// </summary>
        public bool AutoBadging => m_autoBadging;

        /// <summary>
        /// Gets whether this manager has been initialized.
        /// </summary>
        public bool Initialized { get; private set; }

        #endregion

        #region Unity Functions

        protected virtual void Start()
        {
            if (s_instance == this)
            {
                if (!Initialized && m_autoInitialize)
                {
                    var channel = new CrossLocalNotificationChannel("DefaultChannelId", "Default Channel", "Channel Notifications");
                    Initialize(channel);
                }
            }
        }

        /// <summary>
        /// Clean up platform object if necessary
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Platform == null)
            {
                return;
            }

            Platform.NotificationReceived -= OnNotificationReceived;
            if (Platform is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _inForeground = false;
        }

        /// <summary>
        /// Check pending list for expired notifications, when in queue mode.
        /// </summary>
        protected virtual void Update()
        {
            if (s_instance == this)
            {
                if (PendingNotifications == null || !PendingNotifications.Any()
                    || (m_mode & OperatingMode.Queue) != OperatingMode.Queue)
                {
                    return;
                }

                // Check each pending notification for expiry, then remove it
                for (int i = PendingNotifications.Count - 1; i >= 0; --i)
                {
                    PendingNotification queuedNotification = PendingNotifications[i];
                    DateTime? time = queuedNotification.Notification.DeliveryTime;
                    if (time != null && time < DateTime.Now)
                    {
                        PendingNotifications.RemoveAt(i);
                        LocalNotificationExpired?.Invoke(queuedNotification);
                    }
                }
            }
        }

        /// <summary>
        /// Respond to application foreground/background events.
        /// </summary>
        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (s_instance != this)
                return;

            if (Platform == null || !Initialized)
            {
                return;
            }

            _inForeground = hasFocus;

            if (hasFocus)
            {
                OnForegrounding();

                return;
            }

            Platform?.OnBackground();

            // Backgrounding
            // Queue future dated notifications
            if ((m_mode & OperatingMode.Queue) == OperatingMode.Queue)
            {
                // Filter out past events
                for (var i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    PendingNotification pendingNotification = PendingNotifications[i];
                    // Ignore already scheduled ones
                    if (pendingNotification != null && pendingNotification.Notification.Scheduled)
                    {
                        continue;
                    }

                    // If a non-scheduled notification is in the past (or not within our threshold)
                    // just remove it immediately
                    if (pendingNotification != null && pendingNotification.Notification?.DeliveryTime != null &&
                        pendingNotification.Notification?.DeliveryTime - DateTime.Now < MinimumNotificationTime)
                    {
                        PendingNotifications.RemoveAt(i);
                    }
                }

                // Sort notifications by delivery time, if no notifications have a badge number set
                bool noBadgeNumbersSet =
                    PendingNotifications.All(notification => notification == null || notification.Notification == null || notification.Notification.BadgeNumber == null);

                if (noBadgeNumbersSet && AutoBadging)
                {
                    PendingNotifications.Sort((a, b) =>
                    {
                        if (a == null || a.Notification == null || a.Notification.DeliveryTime == null || !a.Notification.DeliveryTime.HasValue)
                        {
                            return -1;
                        }

                        if (b == null || b.Notification == null || b.Notification.DeliveryTime == null || !b.Notification.DeliveryTime.HasValue)
                        {
                            return -1;
                        }

                        return a.Notification.DeliveryTime.Value.CompareTo(b.Notification.DeliveryTime.Value);
                    });

                    // Set badge numbers incrementally
                    var badgeNum = 1;
                    foreach (PendingNotification pendingNotification in PendingNotifications)
                    {
                        if (pendingNotification.Notification.DeliveryTime.HasValue &&
                            !pendingNotification.Notification.Scheduled)
                        {
                            pendingNotification.Notification.BadgeNumber = badgeNum++;
                        }
                    }
                }

                for (int i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    PendingNotification pendingNotification = PendingNotifications[i];
                    // Ignore already scheduled ones
                    if (pendingNotification == null || 
                        pendingNotification.Notification == null || 
                        pendingNotification.Notification.Scheduled)
                    {
                        continue;
                    }

                    // Schedule it now
                    Platform.ScheduleNotification(pendingNotification.Notification);
                }

                // Clear badge numbers again (for saving)
                if (noBadgeNumbersSet && AutoBadging)
                {
                    foreach (PendingNotification pendingNotification in PendingNotifications)
                    {
                        if (pendingNotification != null && 
                            pendingNotification.Notification != null && 
                            pendingNotification.Notification.DeliveryTime != null && 
                            pendingNotification.Notification.DeliveryTime.HasValue)
                        {
                            pendingNotification.Notification.BadgeNumber = null;
                        }
                    }
                }
            }

            // Calculate notifications to save
            var notificationsToSave = new List<PendingNotification>(PendingNotifications.Count);
            foreach (PendingNotification pendingNotification in PendingNotifications)
            {
                // If we're in clear mode, add nothing unless we're in rescheduling mode
                // Otherwise add everything
                if ((m_mode & OperatingMode.ClearOnForegrounding) == OperatingMode.ClearOnForegrounding)
                {
                    if ((m_mode & OperatingMode.RescheduleAfterClearing) != OperatingMode.RescheduleAfterClearing)
                    {
                        continue;
                    }

                    // In reschedule mode, add ones that have been scheduled, are marked for
                    // rescheduling, and that have a time
                    if (pendingNotification != null &&
                        pendingNotification.Notification != null &&
                        pendingNotification.Reschedule &&
                        pendingNotification.Notification.Scheduled &&
                        pendingNotification.Notification.DeliveryTime.HasValue)
                    {
                        notificationsToSave.Add(pendingNotification);
                    }
                }
                else
                {
                    // In non-clear mode, just add all scheduled notifications
                    if (pendingNotification != null && 
                        pendingNotification.Notification != null && 
                        pendingNotification.Notification.Scheduled)
                    {
                        notificationsToSave.Add(pendingNotification);
                    }
                }
            }

            // Save to disk
            if (Serializer != null)
            {
                Serializer.Serialize(notificationsToSave);
            }
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Initialize the notifications manager.
        /// </summary>
        /// <param name="channels">An optional collection of channels to register, for Android</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has already been called.</exception>
        public virtual void Initialize(params CrossLocalNotificationChannel[] channels)
        {
            if (Initialized)
            {
                throw new InvalidOperationException("NotificationsManager already initialized.");
            }

            Initialized = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            Platform = new AndroidNotificationsPlatform();

            // Register the notification channels
            var doneDefault = false;
            foreach (CrossLocalNotificationChannel notificationChannel in channels)
            {
                if (!doneDefault)
                {
                    doneDefault = true;
                    ((AndroidNotificationsPlatform)Platform).DefaultChannelId = notificationChannel.Id;
                }

                long[] vibrationPattern = new long[0];
                if (notificationChannel.VibrationPattern != null)
                {
                    vibrationPattern = notificationChannel.VibrationPattern.Select(v => (long)v).ToArray();
                }

                // Wrap channel in Android object
                var androidChannel = new AndroidNotificationChannel(notificationChannel.Id, notificationChannel.Name,
                    notificationChannel.Description,
                    (Importance)notificationChannel.Style)
                {
                    CanBypassDnd = notificationChannel.HighPriority,
                    CanShowBadge = notificationChannel.ShowsBadge,
                    EnableLights = notificationChannel.ShowLights,
                    EnableVibration = notificationChannel.Vibrates,
                    LockScreenVisibility = (LockScreenVisibility)notificationChannel.Privacy,
                    VibrationPattern = vibrationPattern
                };

                AndroidNotificationCenter.RegisterNotificationChannel(androidChannel);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Platform = new iOSNotificationsPlatform();
#else
            if ((m_genericPlatformMode.HasFlag(GenericPlatformModeEnum.Editor) && Application.isEditor) ||
                (m_genericPlatformMode.HasFlag(GenericPlatformModeEnum.Build) && Application.isPlaying && !Application.isEditor))
            {
                Platform = new GenericNotificationsPlatform();
            }
#endif
            if (Platform == null)
            {
                return;
            }

            PendingNotifications = new List<PendingNotification>();
            Platform.NotificationReceived += OnNotificationReceived;

            // Check serializer
            if (Serializer == null)
            {
                Serializer = new DefaultSerializer(Path.Combine(Application.persistentDataPath, DefaultFilename));
            }

            OnForegrounding();
        }

        /// <summary>
        /// Creates a new notification object for the current platform.
        /// </summary>
        /// <returns>The new notification, ready to be scheduled, or null if there's no valid platform.</returns>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public ICrossLocalNotification CreateNotification()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            return Platform?.CreateNotification();
        }

        /// <summary>
        /// Schedules a notification to be delivered.
        /// </summary>
        /// <param name="notification">The notification to deliver.</param>
        public PendingNotification ScheduleNotification(ICrossLocalNotification notification)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (notification == null || Platform == null)
            {
                return null;
            }

            // If we queue, don't schedule immediately.
            // Also immediately schedule non-time based deliveries (for iOS)
            if ((m_mode & OperatingMode.Queue) != OperatingMode.Queue ||
                notification.DeliveryTime == null)
            {
                Platform.ScheduleNotification(notification);
            }
            else if (!notification.Id.HasValue)
            {
                // Generate an ID for items that don't have one (just so they can be identified later)
                int id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
                notification.Id = id;
            }

            // Register pending notification
            var result = new PendingNotification(notification);
            PendingNotifications.Add(result);

            return result;
        }

        /// <summary>
        /// Cancels a scheduled notification.
        /// </summary>
        /// <param name="notificationId">The ID of the notification to cancel.</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void CancelNotification(int notificationId)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (Platform == null)
            {
                return;
            }

            Platform.CancelNotification(notificationId);

            // Remove the cancelled notification from scheduled list
            int index = PendingNotifications.FindIndex(scheduledNotification =>
                scheduledNotification.Notification.Id == notificationId);

            if (index >= 0)
            {
                PendingNotifications.RemoveAt(index);
            }
        }

        /// <summary>
        /// Cancels all scheduled notifications.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void CancelAllNotifications()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (Platform == null)
            {
                return;
            }

            Platform.CancelAllScheduledNotifications();

            PendingNotifications.Clear();
        }

        /// <summary>
        /// Dismisses a displayed notification.
        /// </summary>
        /// <param name="notificationId">The ID of the notification to dismiss.</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void DismissNotification(int notificationId)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            Platform?.DismissNotification(notificationId);
        }

        /// <summary>
        /// Dismisses all displayed notifications.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
        public void DismissAllNotifications()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            Platform?.DismissAllDisplayedNotifications();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ICrossLocalNotification GetLastNotification()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            return Platform?.GetLastNotification();
        }

        #endregion

        #region Receivers

        /// <summary>
        /// Event fired by <see cref="Platform"/> when a notification is received.
        /// </summary>
        protected virtual void OnNotificationReceived(ICrossLocalNotification deliveredNotification)
        {
            // Ignore for background messages (this happens on Android sometimes)
            if (!_inForeground)
            {
                return;
            }

            // Find in pending list
            int deliveredIndex =
                PendingNotifications.FindIndex(scheduledNotification =>
                    scheduledNotification.Notification.Id == deliveredNotification.Id);
            if (deliveredIndex >= 0)
            {
                LocalNotificationDelivered?.Invoke(PendingNotifications[deliveredIndex]);

                PendingNotifications.RemoveAt(deliveredIndex);
            }
        }

        // Clear foreground notifications and reschedule stuff from a file
        protected virtual void OnForegrounding()
        {
            PendingNotifications.Clear();

            Platform.OnForeground();

            // Deserialize saved items
            IList<ICrossLocalNotification> loaded = Serializer?.Deserialize(Platform);

            // Foregrounding
            if ((m_mode & OperatingMode.ClearOnForegrounding) == OperatingMode.ClearOnForegrounding)
            {
                // Clear on foregrounding
                Platform.CancelAllScheduledNotifications();

                // Only reschedule in reschedule mode, and if we loaded any items
                if (loaded == null ||
                    (m_mode & OperatingMode.RescheduleAfterClearing) != OperatingMode.RescheduleAfterClearing)
                {
                    return;
                }

                // Reschedule notifications from deserialization
                foreach (ICrossLocalNotification savedNotification in loaded)
                {
                    if (savedNotification.DeliveryTime > DateTime.Now)
                    {
                        PendingNotification pendingNotification = ScheduleNotification(savedNotification);
                        pendingNotification.Reschedule = true;
                    }
                }
            }
            else
            {
                // Just create PendingNotification wrappers for all deserialized items.
                // We're not rescheduling them because they were not cleared
                if (loaded == null)
                {
                    return;
                }

                foreach (ICrossLocalNotification savedNotification in loaded)
                {
                    if (savedNotification.DeliveryTime > DateTime.Now)
                    {
                        PendingNotifications.Add(new PendingNotification(savedNotification));
                    }
                }
            }
        }

        #endregion
    }
}
