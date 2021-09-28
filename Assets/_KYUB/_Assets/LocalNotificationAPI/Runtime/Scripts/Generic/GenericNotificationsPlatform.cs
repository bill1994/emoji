using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Kyub.LocalNotification.Generic
{
    public class GenericNotificationsPlatform : ICrossLocalNotificationsPlatform<GenericCrossLocalNotification>,
        IDisposable
    {
        GenericCrossLocalNotification _lastReceivedNotification = null;

        List<GenericCrossLocalNotification> _scheduledNotifications = new List<GenericCrossLocalNotification>();

        public event Action<ICrossLocalNotification> NotificationReceived;

        public GenericNotificationsPlatform()
        {
            ApplicationContext.OnUpdate -= OnUpdate;
            ApplicationContext.OnUpdate += OnUpdate;
        }

        public void CancelAllScheduledNotifications()
        {
            _scheduledNotifications.Clear();
        }

        public void CancelNotification(int notificationId)
        {
            var index = _scheduledNotifications.FindIndex(a => a != null && a.Id == notificationId);
            if(index >= 0)
                _scheduledNotifications.RemoveAt(index);
        }

        public GenericCrossLocalNotification CreateNotification()
        {
            var notification = new GenericCrossLocalNotification();
            notification.Id = notification.GetHashCode();

            return notification;
        }

        public void DismissAllDisplayedNotifications()
        {
        }

        public void DismissNotification(int notificationId)
        {
        }

        public void Dispose()
        {
            Kyub.ApplicationContext.OnUpdate -= OnUpdate;
        }

        public GenericCrossLocalNotification GetLastNotification()
        {
            return _lastReceivedNotification as GenericCrossLocalNotification;
        }

        public void OnBackground()
        {
        }

        public void OnForeground()
        {
        }

        public void OnUpdate()
        {
            if (_scheduledNotifications != null && _scheduledNotifications.Count > 0)
            {
                var notification = _scheduledNotifications[0];

                if (notification == null || notification.DeliveryTime == null || notification.DeliveryTime >= DateTime.Now)
                {
                    _scheduledNotifications.RemoveAt(0);
                    if (notification != null)
                    {
                        NotificationReceived?.Invoke(notification);

                        Debug.Log($"NotificationId: '{notification.Id}'\nTitle: {notification.Title}\nBody: {notification.Body}");
                     }
                }
            }
        }

        public void ScheduleNotification(GenericCrossLocalNotification notification)
        {
            if (notification == null)
                return;

            if (notification.Id == null)
                notification.Id = notification.GetHashCode();

            var index = _scheduledNotifications.FindIndex(a => a != null && a.Id == notification.Id);
            if (index >= 0)
                _scheduledNotifications[index] = notification;
            else
            {
                if (notification.DeliveryTime == null)
                    _scheduledNotifications.Insert(0, notification);

                int addIndex = 0;
                for (int i = 0; i < _scheduledNotifications.Count; i++)
                {
                    if (_scheduledNotifications[i] != null && _scheduledNotifications[i].DeliveryTime != null && _scheduledNotifications[i].DeliveryTime.Value <= notification.DeliveryTime.Value)
                        addIndex++;
                    else
                    {
                        break;
                    }
                }
                if(addIndex >= _scheduledNotifications.Count)
                    _scheduledNotifications.Add(notification);
                else
                    _scheduledNotifications.Insert(addIndex, notification);

                notification?.OnScheduled();
            }
        }

        public void ScheduleNotification(ICrossLocalNotification gameNotification)
        {
            ScheduleNotification(gameNotification as GenericCrossLocalNotification);
        }

        ICrossLocalNotification ICrossLocalNotificationsPlatform.CreateNotification()
        {
            return CreateNotification();
        }

        ICrossLocalNotification ICrossLocalNotificationsPlatform.GetLastNotification()
        {
            return GetLastNotification();
        }
    }
}
