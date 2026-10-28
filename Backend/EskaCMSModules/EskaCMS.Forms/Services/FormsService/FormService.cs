using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Model;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Media.Models;
using EskaCMS.Media.Services;
using EskaCMS.Pages.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Forms;
using EskaCMS.Entities.Entity.Core;
using System.Diagnostics;
using EskaCMS.Forms.Services.FormsService.Interfaces;
using EskaCMS.Forms.Model.FormsViewModel;

namespace EskaCMS.Forms.Services.FormsService
{
    public class FormService : IFormService
    {

        private readonly IRepository<FormBuilder> _FormBuilderRepository;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<User> _userManager;
        private readonly IRepository<UsersSites> _UsersSitesRepository;
        private readonly SignInManager<User> _signInManager;
        private readonly ISubmittedFormService submittedFormService;
        private readonly ILogger _logger;
        private readonly ITestNotifier _testNotifier;
        private readonly IWorkContext _IWorkContext;
        private readonly IPages _IPages;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly string ALL_KEYS = "ALL_KEYS";
        private readonly Media.Services.Interfaces.IMediaService  _mediaService;
        private readonly IRepository<Entities.Entity.Forms.Forms> _FormsRepo;

        public FormService(IRepository<FormBuilder> FormBuilderRepository
            , IEmailSender emailSender
            , ILoggerFactory loggerFactory
            , IRepository<UsersSites> UsersSitesRepository
            , UserManager<User> userManager
            , SignInManager<User> signInManager
             , RoleManager<Role> roleManager
             , ISubmittedFormService submittedFormService
            , ITestNotifier testNotifier
            , IWorkContext IWorkContext
            , IPages IPages,
           Media.Services.Interfaces.IMediaService MediaService,
            IRepository<Entities.Entity.Forms.Forms> FormsRepo
            , IEmailTemplateService emailTemplateService
            , ISiteSettingsService siteSettingsService)
        {
            _FormsRepo = FormsRepo;
            _FormBuilderRepository = FormBuilderRepository;
            _emailSender = emailSender;
            _userManager = userManager;
            _UsersSitesRepository = UsersSitesRepository;
            _signInManager = signInManager;
            this.submittedFormService = submittedFormService;
            _logger = loggerFactory.CreateLogger<FormService>();
            _testNotifier = testNotifier;
            _IWorkContext = IWorkContext;
            _IPages = IPages;
            _mediaService = MediaService;
            _emailTemplateService = emailTemplateService;
            _siteSettingsService = siteSettingsService;
            //_BundleDestination = Configuration.GetSection("CMSConfig").GetSection("BuilderDestination").Value;
            //  _WebDestination = Configuration.GetSection("CMSConfig").GetSection("WebDestination").Value;
        }


        #region commented Code 
        //public async Task SaveForm(long PageId, string ComponentSettingsId, string Culture, string FormData, IFormFileCollection files)
        //{
        //    try
        //    {
        //        var SiteId = _IWorkContext.GetCurrentSiteId();
        //        var Settings = await _IPages.GetComponentsSettings(PageId, ComponentSettingsId, Culture);
        //        if (Settings.Count() > 0)
        //        {


        //            object EmailsTo = null;
        //            object FormName = string.Empty;
        //            object FormEmailKey = string.Empty;
        //            object EmailFrom = string.Empty;
        //            object SenderModule=string.Empty;
        //            object CustomerEmailTemplateId = string.Empty;
        //            object AdminEmailTemplateId = string.Empty;
        //            object AttacheFolder = string.Empty;
        //            object blocksList;

        //            Settings.TryGetValue("emailFrom", out EmailFrom);
        //            Settings.TryGetValue("emailsTo", out EmailsTo);
        //            Settings.TryGetValue("formName", out FormName);
        //            Settings.TryGetValue("formEmailKey", out FormEmailKey);

        //            Settings.TryGetValue("formReciveSubmiterMessage", out CustomerEmailTemplateId);
        //            Settings.TryGetValue("formReciveMessage", out AdminEmailTemplateId);
        //            Settings.TryGetValue("folderAttach", out AttacheFolder);
        //            Settings.TryGetValue("senderModule", out SenderModule);
        //            Settings.TryGetValue("blocks", out blocksList);
        //            var blocks = JArray.Parse(blocksList.ToString());
        //            //Upload Files and size Validations  
        //            // Filter on Upload List
        //            var uploadList = new List<object>();
        //            var fileSizes = new List<int>();
        //            foreach (var block in blocks)
        //            {
        //                var element = block["element"];
        //                var type = element["type"];

        //                if (element != null && type != null && type.ToString() == "Upload")
        //                {
        //                    var acceptedFiles = block["acceptedFiles"];
        //                    if (acceptedFiles != null)
        //                    {
        //                        foreach (var ext in acceptedFiles)
        //                        {
        //                            if (!uploadList.Contains(ext))
        //                                uploadList.Add(ext.ToString());

        //                        }
        //                    }

        //                    var fileSize = block["fileSize"];

        //                    if (fileSize != null)
        //                    {
        //                        if (!fileSizes.Contains(Convert.ToInt16(fileSize)))
        //                            fileSizes.Add(Convert.ToInt16(fileSize));
        //                    }
        //                }
        //            }

        //            foreach (var file in files)
        //            {
        //                string extension = Path.GetExtension(file.FileName);
        //                if (!uploadList.Contains(extension))
        //                    throw new ValidationException("Filetype not supported.");

        //                long fileSizeinMBytes = (file.Length / 1024) / 1024;
        //                if (fileSizes.Any(x => fileSizeinMBytes > x))
        //                    throw new ValidationException("File exeeded allowed size");
        //            }





        //            //



        //            //string[] arr = EmailsTo.Cast<int>().ToArray();
        //            var AdminEmails = JArray.Parse(EmailsTo.ToString());
        //            var ObjFormBuilder = new FormBuilder();
        //            // var EmailTemplateId = "Request a Phone Call";// await _IPages.GetComponentsSettings(PageId, ComponentSettingsId, Culture, "TemplateId");

        //            string FormattedAdminEmailMessage = string.Empty;
        //            string FormattedCustomerEmailMessage = string.Empty;
        //            var formFiles = new List<FileViewModel>();
        //            AttacheFolder = null ?? "";
        //            if (files.Count > 0)
        //            {
        //                var FolderName = "wwwroot\\Resources\\site_" + SiteId + "\\" + AttacheFolder;//data.AttacheFolder;
        //                if (!Directory.Exists(FolderName))
        //                {
        //                    Directory.CreateDirectory(FolderName);
        //                }

        //                var pathToSave = Path.Combine(FolderName.Trim());

        //                for (int i = 0; i < files.Count; i++)
        //                {
        //                    FileViewModel fileToSave = new FileViewModel();

        //                    var file = files[i];
        //                    var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
        //                    var path = pathToSave;
        //                    if (files.Count > 1)
        //                    {
        //                        path = pathToSave + "\\" + file.Name;
        //                    }
        //                    if (!Directory.Exists(path))
        //                    {
        //                        Directory.CreateDirectory(path);
        //                    }

        //                    var fullPath = Path.Combine(path.Trim(), fileName.Trim());
        //                    var dbPath = Path.Combine(FolderName, fileName);
        //                    fileToSave.fileName = fileName.Trim();
        //                    fileToSave.publicUrl = path.Trim();

        //                    using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        //                    {
        //                        file.CopyTo(stream);
        //                        formFiles.Add(fileToSave);
        //                    }

        //                }
        //            }
        //            //formFiles = files.Where(x => new FileViewModel()
        //            //{
        //            //    contentType = x.ContentType,
        //            //    fileName = x.FileName,
        //            //}).ToList();

        //            var myJsonObject = JsonConvert.DeserializeObject<dynamic>(FormData);

        //            EmailTemplateVM objCustomerEmailTemplate = new EmailTemplateVM();
        //            if (CustomerEmailTemplateId != null)
        //            {
        //                objCustomerEmailTemplate = await _emailTemplateService.GetEmailTemplateById(CustomerEmailTemplateId.ToString());

        //                ObjFormBuilder.SubmitterMailSubject = objCustomerEmailTemplate.Subject;
        //                ObjFormBuilder.SubmitterMailMessage = setDataKeys(FormData, objCustomerEmailTemplate.EmailBody);

        //                var emailsubmitter = (string)myJsonObject[FormEmailKey];
        //                if (!string.IsNullOrEmpty(emailsubmitter))
        //                {
        //                    ObjFormBuilder.SubmitterMailReceiver = emailsubmitter.ToString();

        //                    NotificationObject Notificationobj = new NotificationObject();
        //                    Notificationobj.MessageTitle = objCustomerEmailTemplate.Subject;
        //                    FormattedCustomerEmailMessage = setDataKeys(FormData, objCustomerEmailTemplate.EmailBody);
        //                    Notificationobj.MessageBody = FormattedCustomerEmailMessage;
        //                    Notificationobj.SendValue = new List<string>();
        //                    Notificationobj.SendValue.Add(emailsubmitter);
        //                    Notificationobj.Attachment = formFiles;





        //                    Notificationobj.EmailOptions.Attachment = formFiles;
        //                    Notificationobj.Severity = 0;
        //                    Notificationobj.EmailOptions.TemplateId = objCustomerEmailTemplate.Id;// data.EmailSenderVM;
        //                    await _testNotifier.SendMessageAsync(NotificationType.Email, Notificationobj, SiteId);
        //                }

        //            }
        //            EmailTemplateVM objAdminEmailTemplate = new EmailTemplateVM();
        //            if (AdminEmailTemplateId != null)
        //            {
        //                objAdminEmailTemplate = await _emailTemplateService.GetEmailTemplateById(AdminEmailTemplateId.ToString());
        //                FormName = null ?? objAdminEmailTemplate.Id;
        //                ObjFormBuilder.FormName = FormName.ToString();

        //                ObjFormBuilder.MailSender = EmailFrom.ToString();//appsettings

        //                ObjFormBuilder.MailReceiver = string.Join(",", AdminEmails);

        //                FormattedAdminEmailMessage = setDataKeys(FormData, objAdminEmailTemplate.EmailBody);

        //                ObjFormBuilder.MailSubject = objAdminEmailTemplate.Subject;
        //                ObjFormBuilder.MailMessage = FormattedAdminEmailMessage;

        //            }

        //            EmailFrom = null ?? _siteSettingsService.GetEmailSmtpSettings(SiteId, SenderModule == null ? string.Empty:SenderModule.ToString()  ).Result.SenderEmail;

        //            if (files.Count > 0)
        //            {
        //                var FolderName = "wwwroot\\Resources\\site_" + SiteId + "\\" + AttacheFolder;//data.AttacheFolder;
        //                if (!Directory.Exists(FolderName))
        //                {
        //                    Directory.CreateDirectory(FolderName);
        //                }

        //                var pathToSave = Path.Combine(FolderName.Trim());

        //                for (int i = 0; i < files.Count; i++)
        //                {
        //                    FileViewModel fileToSave = new FileViewModel();

        //                    var file = files[i];
        //                    var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
        //                    var path = pathToSave;
        //                    if (files.Count > 1)
        //                    {
        //                        path = pathToSave + "\\" + file.Name;
        //                    }
        //                    if (!Directory.Exists(path))
        //                    {
        //                        Directory.CreateDirectory(path);
        //                    }

        //                    var fullPath = Path.Combine(path.Trim(), fileName.Trim());
        //                    var dbPath = Path.Combine(FolderName, fileName);
        //                    fileToSave.fileName = fileName.Trim();
        //                    fileToSave.publicUrl = path.Trim();

        //                    using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        //                    {
        //                        file.CopyTo(stream);
        //                        formFiles.Add(fileToSave);
        //                    }

        //                }
        //            }
        //            ObjFormBuilder.FormData = FormData;

        //            ObjFormBuilder.AttacheFolder = AttacheFolder.ToString();





        //            ObjFormBuilder.SubmitionDate = DateTimeOffset.Now;
        //            ObjFormBuilder.SiteId = SiteId;
        //            ObjFormBuilder.FormFiles = JsonConvert.SerializeObject(formFiles);
        //            ObjFormBuilder.MailSendStatus = false;
        //            ObjFormBuilder.SubmitterMailSendStatus = false;
        //            _FormBuilderRepository.Add(ObjFormBuilder);
        //            await _FormBuilderRepository.SaveChangesAsync();

        //            if (AdminEmails.Count() > 0)
        //            {
        //                NotificationObject Notificationobj = new NotificationObject();
        //                Notificationobj.MessageTitle = objAdminEmailTemplate.Subject;
        //                Notificationobj.MessageBody = FormattedAdminEmailMessage;
        //                Notificationobj.SendValue = AdminEmails.ToObject<List<string>>();
        //                Notificationobj.EmailOptions.Attachment = formFiles;
        //                Notificationobj.Severity = 0;
        //                Notificationobj.EmailOptions = new EmailSenderVM();// data.EmailSenderVM;
        //                await _testNotifier.SendMessageAsync(NotificationType.Email, Notificationobj, SiteId);
        //            }
        //            ObjFormBuilder.MailSendStatus = true;
        //            ObjFormBuilder.SubmitterMailSendStatus = true;

        //            _FormBuilderRepository.Update(ObjFormBuilder);
        //        }
        //        else
        //        {
        //            throw new Exception("No settings found");
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }

        //}

        //private string setDataKeys(string formData, string mailMessage)
        //{
        //    try
        //    {
        //        //JObject myJsonObjectddd = JObject.Parse(formData);
        //        var myJsonObject = JsonConvert.DeserializeObject<dynamic>(formData);




        //        while (mailMessage.IndexOf("${{") != -1)
        //        {
        //            try
        //            {
        //                // if key =="All_Params" 
        //                var start = mailMessage.IndexOf("${{");
        //                var end = mailMessage.IndexOf("}}", start) + 2;
        //                var key = mailMessage.Substring(start + 3, end - 2 - (start + 3));
        //                var replace = mailMessage.Substring(start, end - start);
        //                var value = myJsonObject[key];

        //                if (value != null)
        //                {
        //                    if (value.ToString().StartsWith("[") && value.ToString().EndsWith("]"))
        //                    {
        //                        var data = JArray.Parse(value.ToString());
        //                        List<string> replaceArray = new List<string>();
        //                        foreach (var item in data)
        //                        {
        //                            if (item.value == true)
        //                            {
        //                                replaceArray.Add(Regex.Replace(item.dataLabel, @"\s", " ").Trim().ToString());
        //                            }

        //                        }
        //                        value = string.Join(",", replaceArray);

        //                    }
        //                    mailMessage = mailMessage.Replace(replace, value.ToString());
        //                }

        //                else
        //                    mailMessage = mailMessage.Replace(replace, " ");

        //            }
        //            catch (Exception)
        //            {
        //                break;
        //            }

        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return mailMessage;
        //    }
        //    return mailMessage;

        //}

        #endregion


        public GridOutputVM<FormDataViewModel> GetSubmittedForms(long siteId, SearchTableParam<FormDataViewModel> param)
        {

            FormDataViewModel search = param.Search;
            var query = _FormBuilderRepository.Query().AsNoTracking().Where(x => x.SiteId == siteId
                     && (search.SubmitionDateFrom == null || x.SubmitionDate >= search.SubmitionDateFrom)
                     && (search.SubmitionDateTo == null || x.SubmitionDate <= search.SubmitionDateTo)
                     && (search.MailSendStatus == false || search.MailSendStatus == search.MailSendStatus)
                     && (search.SubmitterMailSendStatus == false || search.SubmitterMailSendStatus == search.SubmitterMailSendStatus))
                .OrderByDescending(d => d.SubmitionDate)
                .ToList(); ;

            GridOutputVM<FormDataViewModel> result = new GridOutputVM<FormDataViewModel>
            {
                Items = query.Select(c => new FormDataViewModel
                {
                    Id = c.Id,
                    FormName = c.FormName,
                    MailSender = c.MailSender,
                    MailSendStatus = c.MailSendStatus,
                    SubmitionDate = c.SubmitionDate,
                    SubmitterMailSendStatus = c.SubmitterMailSendStatus,
                    FormData = c.FormData,
                    MailSubject = c.MailSubject,
                    MailReceiverstr = c.MailReceiver,
                    SubmitterMailReceiverstr = c.SubmitterMailReceiver

                }).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).ToList(),
                TotalRecord = query.Count()
            };

            return result;
        }

        public async Task<bool> NewFormSave(
                                long PageId,
                                string ComponentSettingsId,
                                string Culture,
                                string FormData,
                                IFormFileCollection files,
                                string HostName,
                                bool? NotFromPricingQuotation = true)
        {
            try
            {
                var SiteId = _IWorkContext.GetCurrentSiteId();
                var CultureId = _IWorkContext.GetCurrentCultureId();

                var myJson = JsonConvert.DeserializeObject<dynamic>(FormData);
                var IdentifierCode = (string)myJson["identifierCode"];
                var IsChecked = Convert.ToBoolean(myJson["isComponentShared"]);

                var Settings = await _IPages.GetComponentsSettings(
                    PageId, ComponentSettingsId, Culture, IdentifierCode, IsChecked);

                if (Settings == null)
                    throw new Exception("No settings found");

                #region ===== READ SETTINGS (SAME KEYS) =====

                object EmailFrom = string.Empty;
                object EmailsTo = null;
                object FormName = string.Empty;
                object FormEmailKey = string.Empty;
                object FolderId = string.Empty;
                object AttacheFolder = string.Empty;

                object CustomerEmailTemplateId = string.Empty;
                object AdminEmailTemplateId = string.Empty;

                object isCustomerChoiceSendEmail = string.Empty;
                object customerChoiceEmailMessage = string.Empty;
                object customerChoiceEmailSubject = string.Empty;
                object customerChoiceEmailKey = string.Empty;

                object cc = string.Empty;
                object enablecustomercc = string.Empty;
                object enableadmincc = string.Empty;

                Settings.TryGetValue("emailFrom", out EmailFrom);
                Settings.TryGetValue("emailsTo", out EmailsTo);
                Settings.TryGetValue("formName", out FormName);
                Settings.TryGetValue("formEmailKey", out FormEmailKey);
                Settings.TryGetValue("folderId", out FolderId);
                Settings.TryGetValue("folderAttach", out AttacheFolder);
                Settings.TryGetValue("formReciveSubmiterMessage", out CustomerEmailTemplateId);
                Settings.TryGetValue("formReciveMessage", out AdminEmailTemplateId);
                Settings.TryGetValue("isCustomerChoiceSendEmail", out isCustomerChoiceSendEmail);
                Settings.TryGetValue("customerChoiceEmailMessage", out customerChoiceEmailMessage);
                Settings.TryGetValue("customerChoiceEmailSubject", out customerChoiceEmailSubject);
                Settings.TryGetValue("customerChoiceEmailKey", out customerChoiceEmailKey);
                Settings.TryGetValue("cc", out cc);
                Settings.TryGetValue("enablecustomercc", out enablecustomercc);
                Settings.TryGetValue("enableadmincc", out enableadmincc);

                #endregion

                #region ===== PARSE SETTINGS =====

                bool EnableCustomerCc =
                    enablecustomercc != null &&
                    (enablecustomercc.ToString() == "1" ||
                     enablecustomercc.ToString().Equals("true", StringComparison.OrdinalIgnoreCase));

                bool EnableAdminCc =
                    enableadmincc != null &&
                    (enableadmincc.ToString() == "1" ||
                     enableadmincc.ToString().Equals("true", StringComparison.OrdinalIgnoreCase));

                var AdminEmails = EmailsTo != null
                    ? JArray.Parse(EmailsTo.ToString())
                    : new JArray();

                var CcEmails = cc != null && !string.IsNullOrWhiteSpace(cc.ToString())
                    ? JArray.Parse(cc.ToString())
                    : new JArray();

                var ccEmailList = new List<string>();
                foreach (var item in CcEmails)
                {
                    var email = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(email))
                        ccEmailList.Add(email);
                }

                #endregion

                #region ===== FILE UPLOAD =====

                var formFiles = new List<FileViewModel>();

                if (files != null && files.Count > 0)
                {
                    long? folderId =
                        FolderId == null || string.IsNullOrEmpty(FolderId.ToString())
                            ? null
                            : Convert.ToInt64(FolderId);

                    var folderPath = $"wwwroot\\Resources\\{SiteId}\\{AttacheFolder}";
                    Directory.CreateDirectory(folderPath);

                    foreach (var file in files)
                    {
                        var fileName = ContentDispositionHeaderValue
                            .Parse(file.ContentDisposition).FileName.Trim('"');

                        var fullPath = Path.Combine(folderPath, fileName);

                        using var stream = new FileStream(fullPath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        formFiles.Add(new FileViewModel
                        {
                            fileName = fileName,
                            publicUrl = folderPath
                        });
                    }

                    if (NotFromPricingQuotation == true)
                    {
                        var mediaUpload = new MediaUploadModel
                        {
                            Uploads = files.AsEnumerable(),
                            ParentId = folderId,
                            SiteId = SiteId,
                            CreatedBy = await _IWorkContext.GetCurrentUserId(),
                            CultureId = CultureId,
                            Force = true
                        };

                        await _mediaService.SaveMedia(mediaUpload);
                    }
                }

                #endregion

                var EmailSettings = await _siteSettingsService.GetEmailSetting(SiteId);
                var ObjFormBuilder = new FormBuilder();
                var myJsonObject = JsonConvert.DeserializeObject<dynamic>(FormData);

                #region ===== CUSTOMER EMAIL =====

                if (CustomerEmailTemplateId != null)
                {
                    var template = await _emailTemplateService
                        .GetEmailTemplateById(CustomerEmailTemplateId.ToString());

                    var receiver = (string)myJsonObject[FormEmailKey];

                    if (!string.IsNullOrWhiteSpace(receiver))
                    {
                        var notification = new NotificationObject
                        {
                            MessageTitle = template.Subject,
                            MessageBody = setDataKeys(formFiles, FormData, template.EmailBody, HostName),
                            SendValue = new List<string> { receiver },
                            Attachment = formFiles,
                            CC = EnableCustomerCc ? ccEmailList : new List<string>(),
                            Severity = 0
                        };

                        notification.EmailOptions.TemplateId = template.Id;

                        await _testNotifier.SendMessageAsync(
                            NotificationType.Email, EmailSettings, notification, SiteId);
                    }
                }

                #endregion

                #region ===== ADMIN EMAIL =====

                if (AdminEmailTemplateId != null && AdminEmails.Any())
                {
                    var template = await _emailTemplateService
                        .GetEmailTemplateById(AdminEmailTemplateId.ToString());

                    var notification = new NotificationObject
                    {
                        MessageTitle = template.Subject,
                        MessageBody = setDataKeys(formFiles, FormData, template.EmailBody, HostName),
                        SendValue = AdminEmails.ToObject<List<string>>(),
                        Attachment = formFiles,
                        CC = EnableAdminCc ? ccEmailList : new List<string>(),
                        Severity = 0,
                        EmailOptions = new EmailSenderVM()
                    };

                    await _testNotifier.SendMessageAsync(
                        NotificationType.Email, EmailSettings, notification, SiteId);
                }

                #endregion

                #region ===== SAVE FORM =====

                ObjFormBuilder.FormName = FormName?.ToString();
                ObjFormBuilder.MailSender = EmailFrom?.ToString() ?? EmailSettings.SmtpSenderEmail;
                ObjFormBuilder.FormData = FormData;
                ObjFormBuilder.AttacheFolder = AttacheFolder?.ToString();
                ObjFormBuilder.SubmitionDate = DateTimeOffset.Now;
                ObjFormBuilder.SiteId = SiteId;
                ObjFormBuilder.FormFiles = JsonConvert.SerializeObject(formFiles);

                _FormBuilderRepository.Add(ObjFormBuilder);
                await _FormBuilderRepository.SaveChangesAsync();

                #endregion

                return true;
            }
            catch
            {
                throw;
            }
        }

        private async Task<long?> GetFormIdByComponentSettingsId(string componentSettingsId)
        {
            try
            {
                var Form =await _FormsRepo.Query().Where(x => x.ComponentSettingsId == componentSettingsId).FirstOrDefaultAsync();

                if (Form == null)
                { 
                   return null;
                
                }
                return Form.Id;

            }
            catch (Exception ex)
            {

                throw ex;  
            }
        }

        private string setDataKeys(List<FileViewModel> FormFiles, string formData, string mailMessage, string HostName)
        {
            try
            {
                var myJsonObject = JsonConvert.DeserializeObject<dynamic>(formData);

                while (mailMessage.IndexOf("${{") != -1)
                {
                    try
                    {
                        var start = mailMessage.IndexOf("${{");
                        var end = mailMessage.IndexOf("}}", start) + 2;
                        var key = mailMessage.Substring(start + 3, end - 2 - (start + 3));
                        var replace = mailMessage.Substring(start, end - start);
                        var value = myJsonObject[key];
                        if (replace == "${{uploaded-files}}")
                        {
                            if (HostName != null)
                            {
                                value = EditUrlsForEmails(FormFiles, HostName);
                            }
                        }


                        if (value == null)
                        {
                            value = string.Empty;
                        }


                        #region comment
                        //if (value != null)
                        //{
                        //    if (value.ToString().StartsWith("[") && value.ToString().EndsWith("]"))
                        //    {
                        //        var data = JArray.Parse(value.ToString());
                        //        List<string> replaceArray = new List<string>();
                        //        foreach (var item in data)
                        //        {
                        //            if (item.value == true)
                        //            {
                        //                replaceArray.Add(Regex.Replace(item.dataLabel, @"\s", " ").Trim().ToString());
                        //            }

                        //        }
                        //        value = string.Join(",", replaceArray);

                        //    }
                        //    mailMessage = mailMessage.Replace(replace, value.ToString());
                        //}

                        //else
                        //    mailMessage = mailMessage.Replace(replace, " ");
                        #endregion

                        string FormData = string.Empty;

                        if (key == ALL_KEYS)
                        {
                            foreach (var item in myJsonObject)
                            {
                                FormData += " <b>" + item.Name + "</b>: " + GetReplacedEmailTemplateValue(mailMessage, item.Name, item.Value.ToString()) + "<br/>";

                            }
                            mailMessage = mailMessage.Replace(replace, FormData);

                        }
                        else
                        {
                            mailMessage = mailMessage.Replace(replace, GetReplacedEmailTemplateValue(mailMessage, key, value.ToString()));

                        }



                    }
                    catch (Exception)
                    {
                        break;
                    }

                }
            }
            catch (Exception)
            {
                return mailMessage;
            }
            return mailMessage;

        }

        private string GetReplacedEmailTemplateValue(string mailMessage, string Key, string Value )
        {

            if (Value != null)
            {
                if (Value.ToString().StartsWith("[") && Value.ToString().EndsWith("]"))
                {
                    var data = JArray.Parse(Value.ToString());
                    List<string> replaceArray = new List<string>();
                    foreach (dynamic item in data)
                    {
                        if (item.value == true)
                        {
                            replaceArray.Add(Regex.Replace(item.dataLabel, @"\s", " ").Trim().ToString());
                        }

                    }
                    Value = string.Join(",", replaceArray);

                }

                if (Value.Contains("fakepath"))
                {
                    Value = Path.GetFileName(Value);
                   
                }

               
                return Value.ToString();
            }

            else
                return " ";

        }
        private string EditUrlFormData(List<FileViewModel> formFiles, string FormData)
        {
            List<string> listUrls = new List<string>();

            foreach (var item in formFiles)
            {
                //For Database 
                var publicUrl = item.publicUrl.Replace("\\", "\\\\");
                // Remove wwwroot From Public Url
                listUrls.Add(publicUrl.Replace("wwwroot\\\\", "") + item.fileName);
            }
            string Result = null;


            if (formFiles.Count > 0)
            {

                Result = FormData.Replace("[formFiles]", string.Join(",", listUrls));
            }
            else
            {
                Result = FormData.Replace("[formFiles]", "");
            }
            return Result;
        }
        private string EditUrlsForEmails(List<FileViewModel> Formfiles, string HostName)
        {
            List<string> listUrls = new List<string>();

            string Result = null;
            foreach (var item in Formfiles)
            {


                //For Database 
                var Path = item.publicUrl.Replace("wwwroot", HostName) + item.fileName;
                // Remove wwwroot From Public Url

                var HtmlPath = $"<a href=\"{Path}\" download>" + item.fileName + "</a>";
                listUrls.Add(HtmlPath);
            }
            return string.Join(",", listUrls);
        }

        public async Task<bool> UpsertFormBuilderSettings(List<FormBuilderVm> formBuilder)
        {
            try
            {
                var CurrentUserId = await _IWorkContext.GetCurrentUserId();
                var CurrentSiteId = _IWorkContext.GetCurrentSiteId();
                Entities.Entity.Forms.Forms Form;
                foreach (var Forms in formBuilder)
                {



                    Form = await _FormsRepo.Query().Where(x => x.ComponentSettingsId.Trim() == Forms.ComponenetSettingsId.Trim()).FirstOrDefaultAsync();


                    bool IsEditMood = Form != null;

                    if (IsEditMood)
                    {
                        Form.ModificationDate = DateTime.Now;
                        Form.ModifiedById = CurrentUserId;

                    }
                    else
                    {
                        Form = new Entities.Entity.Forms.Forms();

                        Form.CreatedById = CurrentUserId;
                        Form.CreationDate = DateTime.Now;

                    }

                    Form.Name = string.IsNullOrEmpty(Forms.Name) ? "" : Forms.Name;
                    Form.SiteId = CurrentSiteId;
                    Form.AdditionalDate = Forms.AdditionalData;
                    Form.ComponentSettingsId = Forms.ComponenetSettingsId;
                    Form.Keys = Forms.Keys;
                    Form.Settings = Forms.Settings;
                    Form.Source = Forms.Source;
                    Form.PageId=Forms.PageId;
                    if (!IsEditMood)
                    {
                        _FormsRepo.Add(Form);

                    }
                    await _FormsRepo.SaveChangesAsync();

                }
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<FormResponseVm>> GetAllForms(long PageId)
        {
            try
            {
                var SiteId = _IWorkContext.GetCurrentSiteId();
                var Forms = await _FormsRepo.
                                      Query()
                                      .Where(x =>x.PageId == PageId && x.SiteId == SiteId)
                                      .Select(x => new FormResponseVm
                                      {
                                          Id = x.Id,
                                          FormName = x.Name
                                      })
                                      .AsNoTracking()
                                      .ToListAsync();

                return Forms;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<List<GetAllKeysVM>> GetAllKeys(long PageId)
        {
            try
            {
                var SiteId = _IWorkContext.GetCurrentSiteId();
                var Keys = await _FormsRepo
                                  .Query()
                                  .Where(x => x.SiteId == SiteId &&
                                            x.PageId == PageId)
                                  .Select(x => new GetAllKeysVM
                                  {
                                      Id = x.Id,
                                      Key = x.Keys,
                                      FormName=x.Name,
                                      ComponentSettingsId=x.ComponentSettingsId

                                  })
                                  .AsNoTracking()
                                  .ToListAsync();
                return Keys;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task<FormEditVM> GetFormEdit(long Id)
        {
            try
            {
                var Form = await _FormsRepo.Query().Where(x => x.Id == Id)
                                                 .Select(x => new FormEditVM
                                                 {
                                                     AdditionalDate = x.AdditionalDate,
                                                     Keys = x.Keys,
                                                     Settings = x.Settings,
                                                     ComponentSettingsId = x.ComponentSettingsId,
                                                     Name = x.Name,
                                                     Source = x.Source


                                                 }).FirstOrDefaultAsync();

                return Form;

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }
    }
}

