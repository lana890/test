using EskaCMS.Core.Model;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Notifications.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Notifications.Areas.Notifications.ViewModels
{
    //the model is taking the object data based on the firebase notification document
    //the ur for the documentation is https://firebase.google.com/docs/cloud-messaging/http-server-ref#notification-payload-support
    // and https://firebase.google.com/docs/cloud-messaging/http-server-ref#send-downstream

    public class FirebaseModel
    {
        public string topic { get; set; }
        public string Token { get; set; }
        public bool validateOnly { get; set; }
        public Messageobject message { get; set; }


    }
    public enum SendBy
    {
        MobileNumber = 1,
        Email = 2,
        Username = 3,
        UserId = 4
    }
    public class Messageobject
    {
        public NotificationObject notification { get; set; }
    }

   


public class PushNotification

    {
        public PushNotification()
        {
            DesitinationType = DesitinationType.Personal;
            Topics = new List<string>();
        }
        public List<string> Topics { get; set; }
        public DesitinationType DesitinationType { get; set; }
        public Dictionary<string, string>? Data { get; set; }

        
    }
     
    public class NotificationObject 
    {
        public NotificationObject()
        {
            MessageTitle = string.Empty;
            //Severities = EnumHelper.ToDictionary(typeof(NotificationSeverity)).Select(s => new SelectListItem(s.Key.ToString(), s.Value)).ToList();
            Severity = NotificationSeverity.Success;
            //SMSProvider = SMSProviders.SMSGlobal;
            PushNotificationOptions = new PushNotification();
            EmailOptions = new EmailSenderVM();
            SendValue = new List<string>();
            SendVia = new SendBy();
            Attachment = new List<FileViewModel>();
        }
        public string MessageBody { get; set; }
        public string MessageTitle { get; set; }
        public PushNotification PushNotificationOptions { get; set; }
        public SendBy SendVia { get; set; }
        public List<string> SendValue { get; set; }
        public List<string> CC { get; set; }=new List<string>();
       public NotificationType NotificationType { get; set; }

        public NotificationSeverity Severity { get; set; }

        public long[] UserId { get; set; }

        public List<SelectListItem> OnlineUsers { get; set; }

        public List<SelectListItem> Severities { get; set; }

        public SMSProviders? SMSProvider { get; set; }
        public WhatsAppProviders? WhatsAppProvider { get; set; }

        public EmailSenderVM EmailOptions { get; set; }
        public List<FileViewModel> Attachment { get; set; }
        public List<FileMemoryViewModel>? Files { get; set; }
        public string EmailTemplateId { get; set; }

        public bool isHTML { get; set; }

        public Dictionary<string, List<EmailParamsVM>>? UserInfoWithParams { get; set; }
        public bool IsPdf { get; set; }
        public string PdfLink { get; set; }
        public string UserName { get; set; }

    }
   



    public enum NotificationType
    {
        PushNotification = 1,
        WebNotification = 2, // Signal R
        Email = 3,
        SMS = 4,
        Integeration=5,
        BothSMSEmail=6,
        SignalR=7,
        WhatsApp=8



    }
    public enum FireBaseTopics
    {
        NoTopic = 0,
        SilverMembers = 1,
        GoldenMembers = 2,
        BronzeMembers = 3,
        GuestUsers = 4,
        Marketing = 5,

    }
    public enum DesitinationType
    {
        Android,
        IOS,
        Personal
    }

    //public class PayloadModel
    //{
    //    public dataObject? data { get; set; }
    //    public NotificationObject? notification { get; set; }

    //}


    //public class TargetsObject
    //{
    //    public string? to { get; set; }
    //    public string? registration_ids { get; set; }
    //    public string? condition { get; set; }
    //}

    //public class OptionsObject
    //{
    //    public string? collapse_key { get; set; }
    //    public string? priority { get; set; }
    //    public string? content_available { get; set; }
    //    public int? time_to_live { get; set; }
    //    public string? restricted_package_name  { get; set; }//android  only
    //    public bool? dry_run { get; set; }
    //}

    //public class dataObject
    //{
    //    public string?  key { get; set; }
    //    public string? value { get; set; }
    //}

    //public class NotificationObject
    //{
    //    public string? title { get; set; }//general use
    //    public string? body { get; set; }//general use
    //    public string? sound { get; set; }//general use
    //    public  string? badge { get; set; }//for apple use
    //    public string? icon { get; set; }//for android use
    //    public string? android_channel_id { get; set; }//for android use
    //    public string? click_action { get; set; }//general use
    //    public string? subtitle { get; set; }//for apple use
    //    public string? body_loc_key { get; set; }//general use
    //    public string? body_loc_args { get; set; }//general use
    //    public string? title_loc_key { get; set; }//general use
    //    public string? title_loc_args { get; set; }//general use
    //    public string? tag { get; set; }//for android use
    //    public string? color { get; set; }//for android use
    //}

}
