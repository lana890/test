using System;

namespace EskaCommerce.Notifications.Models.NotificationModels
{
    /// <summary>
    /// Can be used to store a simple message as notification data.
    /// </summary>
    [Serializable]
    public class MessageNotificationData : NotificationData
    {
        /// <summary>
        /// The message.
        /// </summary>
        public string Message
        {
            get => _message ?? this[nameof(Message)] as string;
            set
            {
                this[nameof(Message)] = value;
                _message = value;
            }
        }
        public string Title
        {
            get => _title ?? this[nameof(Title)] as string;
            set
            {
                this[nameof(Title)] = value;
                _title = value;
            }
        }
        public string AdditionalData
        {
            get => _additionalData ?? this[nameof(AdditionalData)] as string;
            set
            {
                this[nameof(AdditionalData)] = value;
                _additionalData = value;
            }
        }

        public string PushNotificationData
        {
            get => _pushnotificationdata ?? this[nameof(PushNotificationData)] as string;
            set
            {
                this[nameof(PushNotificationData)] = value;
                _pushnotificationdata = value;
            }
        }

        private string _title;
        private string _message;
        private string _additionalData;
        private string _pushnotificationdata;


        /// <summary>
        /// Needed for serialization.
        /// </summary>
        private MessageNotificationData()
        {

        }

        public MessageNotificationData(string message)
        {
            Message = message;
        }

        public MessageNotificationData(string message, string title)
        {
            Message = message;
            Title = title;
        }
        public MessageNotificationData(string message, string title, string additionalData)
        {
            Message = message;
            Title = title;
            AdditionalData = additionalData;
        }
        public MessageNotificationData(string message, string title, string additionalData,string pushNotificationData)
        {
            Message = message;
            Title = title;
            AdditionalData = additionalData;
            PushNotificationData = pushNotificationData;
        }
    }
}
