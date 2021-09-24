#if UNITY_ANDROID
using System;
using Unity.Notifications.Android;

namespace Kyub.LocalNotification.Android
{
    /// <summary>
    /// Android implementation of <see cref="ICrossLocalNotificationsPlatform"/>.
    /// </summary>
    public class AndroidNotificationsPlatform : ICrossLocalNotificationsPlatform<AndroidCrossLocalNotification>,
        IDisposable
    {
        /// <inheritdoc />
        public event Action<ICrossLocalNotification> NotificationReceived;

        /// <summary>
        /// Gets or sets the default channel ID for notifications.
        /// </summary>
        /// <value>The default channel ID for new notifications, or null.</value>
        public string DefaultChannelId { get; set; }

        /// <summary>
        /// Instantiate a new instance of <see cref="AndroidNotificationsPlatform"/>.
        /// </summary>
        public AndroidNotificationsPlatform()
        {
            AndroidNotificationCenter.OnNotificationReceived += OnLocalNotificationReceived;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Will set the <see cref="AndroidCrossLocalNotification.Id"/> field of <paramref name="gameNotification"/>.
        /// </remarks>
        public void ScheduleNotification(AndroidCrossLocalNotification gameNotification)
        {
            if (gameNotification == null)
            {
                throw new ArgumentNullException(nameof(gameNotification));
            }

            if (gameNotification.Id.HasValue)
            {
                AndroidNotificationCenter.SendNotificationWithExplicitID(gameNotification.InternalNotification,
                    gameNotification.DeliveredChannel,
                    gameNotification.Id.Value);
            }
            else
            {
                int notificationId = AndroidNotificationCenter.SendNotification(gameNotification.InternalNotification,
                    gameNotification.DeliveredChannel);
                gameNotification.Id = notificationId;
            }

            gameNotification.OnScheduled();
        }

        /// <inheritdoc />
        /// <remarks>
        /// Will set the <see cref="AndroidCrossLocalNotification.Id"/> field of <paramref name="gameNotification"/>.
        /// </remarks>
        public void ScheduleNotification(ICrossLocalNotification gameNotification)
        {
            if (gameNotification == null)
            {
                throw new ArgumentNullException(nameof(gameNotification));
            }

            if (!(gameNotification is AndroidCrossLocalNotification androidNotification))
            {
                throw new InvalidOperationException(
                    "Notification provided to ScheduleNotification isn't an AndroidCrossLocalNotification.");
            }

            ScheduleNotification(androidNotification);
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AndroidCrossLocalNotification" />.
        /// </summary>
        public AndroidCrossLocalNotification CreateNotification()
        {
            var notification = new AndroidCrossLocalNotification()
            {
                DeliveredChannel = DefaultChannelId
            };

            return notification;
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AndroidCrossLocalNotification" />.
        /// </summary>
        ICrossLocalNotification ICrossLocalNotificationsPlatform.CreateNotification()
        {
            return CreateNotification();
        }

        /// <inheritdoc />
        public void CancelNotification(int notificationId)
        {
            AndroidNotificationCenter.CancelScheduledNotification(notificationId);
        }

        /// <inheritdoc />
        /// <summary>
        /// Not currently implemented on Android
        /// </summary>
        public void DismissNotification(int notificationId)
        {
            AndroidNotificationCenter.CancelDisplayedNotification(notificationId);
        }

        /// <inheritdoc />
        public void CancelAllScheduledNotifications()
        {
            AndroidNotificationCenter.CancelAllScheduledNotifications();
        }

        /// <inheritdoc />
        public void DismissAllDisplayedNotifications()
        {
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
        }

        /// <inheritdoc />
        ICrossLocalNotification ICrossLocalNotificationsPlatform.GetLastNotification()
        {
            return GetLastNotification();
        }

        /// <inheritdoc />
        public AndroidCrossLocalNotification GetLastNotification()
        {
            var data = AndroidNotificationCenter.GetLastNotificationIntent();

            if (data != null)
            {
                return new AndroidCrossLocalNotification(data.Notification, data.Id, data.Channel);
            }

            return null;
        }

        /// <summary>
        /// Does nothing on Android.
        /// </summary>
        public void OnForeground() {}

        /// <summary>
        /// Does nothing on Android.
        /// </summary>
        public void OnBackground() {}

        /// <summary>
        /// Unregister delegates.
        /// </summary>
        public void Dispose()
        {
            AndroidNotificationCenter.OnNotificationReceived -= OnLocalNotificationReceived;
        }

        // Event handler for receiving local notifications.
        private void OnLocalNotificationReceived(AndroidNotificationIntentData data)
        {
            // Create a new AndroidCrossLocalNotification out of the delivered notification, but only
            // if the event is registered
            NotificationReceived?.Invoke(new AndroidCrossLocalNotification(data.Notification, data.Id, data.Channel));
        }
    }
}
#endif
