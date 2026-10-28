using Newtonsoft.Json;
namespace EskaCommerce.Notifications.Models
{
    [Serializable]
    public class NotificationData
    {

        public const string MainSystemMessageNotificationDataTypeName =
            "EskaCommerce.Notifications.Models.MessageNotificationData, EskaCommerce.Notifications, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public virtual string Type => GetType().FullName;

        public object this[string key]
        {
            get => Properties.TryGetValue(key, out var obj) ? obj : default;
            set => Properties[key] = value;
        }

        public Dictionary<string, object> Properties
        {
            get => _properties;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                foreach (var keyValue in value)
                {
                    if (!_properties.ContainsKey(keyValue.Key))
                    {
                        _properties[keyValue.Key] = keyValue.Value;
                    }
                }
            }
        }
        private readonly Dictionary<string, object> _properties;

        public NotificationData()
        {
            _properties = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings());
        }
    }

    [Serializable]
    public class MessageNotificationData : NotificationData
    {
        public string Message
        {
            get => _message ?? (this[nameof(Message)] as string);
            set
            {
                this[nameof(Message)] = value;
                _message = value;
            }
        }
        public string Title
        {
            get => _title ?? (this[nameof(Title)] as string);
            set
            {
                this[nameof(Title)] = value;
                _title = value;
            }
        }
        public string AdditionalData
        {
            get => _additionalData ?? (this[nameof(AdditionalData)] as string);
            set
            {
                this[nameof(AdditionalData)] = value;
                _additionalData = value;
            }
        }

        public string PushNotificationData
        {
            get => _pushnotificationdata ?? (this[nameof(PushNotificationData)] as string);
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
        public MessageNotificationData(string message, string title, string additionalData, string pushNotificationData)
        {
            Message = message;
            Title = title;
            AdditionalData = additionalData;
            PushNotificationData = pushNotificationData;
        }
    }
}
