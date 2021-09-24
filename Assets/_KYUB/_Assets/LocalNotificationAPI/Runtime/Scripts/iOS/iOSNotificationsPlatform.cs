#if UNITY_IOS
using System;
using Unity.Notifications.iOS;

namespace Kyub.LocalNotification.iOS
{
    /// <summary>
    /// iOS implementation of <see cref="ICrossLocalNotificationsPlatform"/>.
    /// </summary>
    public class iOSNotificationsPlatform : ICrossLocalNotificationsPlatform<iOSCrossLocalNotification>,
        IDisposable
    {
        /// <inheritdoc />
        public event Action<ICrossLocalNotification> NotificationReceived;

        /// <summary>
        /// Instantiate a new instance of <see cref="iOSNotificationsPlatform"/>.
        /// </summary>
        public iOSNotificationsPlatform()
        {
            iOSNotificationCenter.OnNotificationReceived += OnLocalNotificationReceived;
        }

        /// <inheritdoc />
        public void ScheduleNotification(ICrossLocalNotification gameNotification)
        {
            if (gameNotification == null)
            {
                throw new ArgumentNullException(nameof(gameNotification));
            }

            if (!(gameNotification is iOSCrossLocalNotification notification))
            {
                throw new InvalidOperationException(
                    "Notification provided to ScheduleNotification isn't an iOSCrossLocalNotification.");
            }

            ScheduleNotification(notification);
        }

        /// <inheritdoc />
        public void ScheduleNotification(iOSCrossLocalNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            iOSNotificationCenter.ScheduleNotification(notification.InternalNotification);
            notification.OnScheduled();
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:Kyub.LocalNotification.Android.AndroidNotification" />.
        /// </summary>
        ICrossLocalNotification ICrossLocalNotificationsPlatform.CreateNotification()
        {
            return CreateNotification();
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:Kyub.LocalNotification.Android.AndroidNotification" />.
        /// </summary>
        public iOSCrossLocalNotification CreateNotification()
        {
            return new iOSCrossLocalNotification();
        }

        /// <inheritdoc />
        public void CancelNotification(int notificationId)
        {
            iOSNotificationCenter.RemoveScheduledNotification(notificationId.ToString());
        }

        /// <inheritdoc />
        public void DismissNotification(int notificationId)
        {
            iOSNotificationCenter.RemoveDeliveredNotification(notificationId.ToString());
        }

        /// <inheritdoc />
        public void CancelAllScheduledNotifications()
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();
        }

        /// <inheritdoc />
        public void DismissAllDisplayedNotifications()
        {
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }

        /// <inheritdoc />
        ICrossLocalNotification ICrossLocalNotificationsPlatform.GetLastNotification()
        {
            return GetLastNotification();
        }

        /// <inheritdoc />
        public iOSCrossLocalNotification GetLastNotification()
        {
            var notification = iOSNotificationCenter.GetLastRespondedNotification();

            if (notification != null)
            {
                return new iOSCrossLocalNotification(notification);
            }

            return null;
        }

        /// <summary>
        /// Clears badge count.
        /// </summary>
        public void OnForeground()
        {
            iOSNotificationCenter.ApplicationBadge = 0;
        }

        /// <summary>
        /// Does nothing on iOS.
        /// </summary>
        public void OnBackground() {}

        /// <summary>
        /// Unregister delegates.
        /// </summary>
        public void Dispose()
        {
            iOSNotificationCenter.OnNotificationReceived -= OnLocalNotificationReceived;
        }

        // Event handler for receiving local notifications.
        private void OnLocalNotificationReceived(iOSNotification notification)
        {
            // Create a new AndroidCrossLocalNotification out of the delivered notification, but only
            // if the event is registered
            NotificationReceived?.Invoke(new iOSCrossLocalNotification(notification));
        }
    }
}
#endif
