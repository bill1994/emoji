using System;
using Unity.Notifications.iOS;
using UnityEngine;
using UnityEngine.Assertions;

namespace Kyub.LocalNotification.Generic
{
    /// <summary>
    /// Generic implementation of <see cref="ICrossLocalNotification"/>.
    /// </summary>
    public class GenericCrossLocalNotification : ICrossLocalNotification
    {
        /// <inheritdoc />
        public int? Id
        {
            get;
            set;
        }

        /// <inheritdoc />
        public string Title { get; set; }

        /// <inheritdoc />
        public string Body { get; set; }

        /// <inheritdoc />
        public string Subtitle { get; set; }

        /// <inheritdoc />
        public string Data { get; set; }

        /// <inheritdoc />
        public string Group { get => CategoryIdentifier; set => CategoryIdentifier = value; }

        /// <inheritdoc />
        public int? BadgeNumber
        {
            get;
            set;
        }

        /// <inheritdoc />
        public bool ShouldAutoCancel { get; set; }

        /// <inheritdoc />
        public bool Scheduled { get; private set; }

        /// <inheritdoc />
        public DateTime? DeliveryTime
        {
            get;
            set;
        }

        /// <summary>
        /// The category identifier for this notification.
        /// </summary>
        public string CategoryIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Does nothing on iOS.
        /// </summary>
        public string SmallIcon { get => null; set {} }

        /// <summary>
        /// Does nothing on iOS.
        /// </summary>
        public string LargeIcon { get => null; set {} }

        /// <summary>
        /// Instantiate a new instance of <see cref="iOSCrossLocalNotification"/>.
        /// </summary>
        public GenericCrossLocalNotification()
        {
        }

        /// <summary>
        /// Mark this notifications scheduled flag.
        /// </summary>
        internal void OnScheduled()
        {
            Assert.IsFalse(Scheduled);
            Scheduled = true;
        }
    }
}
