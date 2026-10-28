using Microsoft.EntityFrameworkCore;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using System;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.Module.EmailSenderSmtp.Data
{
    public class EmailSenderSmptCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
     //       modelBuilder.Entity<SiteSettings>().HasSeedData(
     //      new SiteSettings("SmtpServer") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "mail.eskadenia.com", SiteId = 1L },
     //        new SiteSettings("SmtpServerAdmin") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "mail.eskadenia.com", SiteId = 1L },
     //         new SiteSettings("SmtpSenderEmail") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "mail.eskadenia.com", SiteId = 1L },
     //         new SiteSettings("SmtpSenderEmailAdmin") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "mail.eskadenia.com", SiteId = 1L },
     //        new SiteSettings("SmtpPort") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "25", SiteId = 1L },
     //        new SiteSettings("SmtpUsername") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "RADTime@eskadenia.com", SiteId = 1L },
     //        new SiteSettings("SmtpPassword") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "R@DT!me#123", SiteId = 1L },
     //        new SiteSettings("SmtpRequiresAuthentication") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "false", SiteId = 1L },
     //        new SiteSettings("LoginType") { Module = "CORE", IsVisibleInCommonSettingPage = false, Value = LoginType.DirectLogin.ToString(), SiteId = 1L }

     ////new SiteSettings("SmtpPortAdmin") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "25", SiteId = 1L },
     ////new SiteSettings("SmtpUsernameAdmin") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "RADTime@eskadenia.com", SiteId = 1L },
     ////new SiteSettings("SmtpPasswordAdmin") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "R@DT!me#123", SiteId = 1L },
     ////new SiteSettings("SmtpRequiresAuthenticationAdmin") { Module = "EmailSenderSmpt", IsVisibleInCommonSettingPage = false, Value = "false", SiteId = 1L }
     //);


     //       modelBuilder.Entity<EmailTemplateType>().HasSeedData(
     //     new EmailTemplateType { Id = 1L, Name = "Payment", CreatedById = 1, CreationDate = DateTimeOffset.Now },
     //                   new EmailTemplateType { Id = 2L, Name = "FormBuilder", CreatedById = 1, CreationDate = DateTimeOffset.Now },
     //                   new EmailTemplateType { Id = 3L, Name = "UserEmails", CreatedById = 1, CreationDate = DateTimeOffset.Now },
     //                   new EmailTemplateType { Id = 4L, Name = "OrderInvoice", CreatedById = 1, CreationDate = DateTimeOffset.Now },
     //                   new EmailTemplateType { Id = 5L, Name = "PricingQuotation", CreatedById = 1, CreationDate = DateTimeOffset.Now }
     //  );


     //       modelBuilder.Entity<EmailTemplates>().HasSeedData(
     //                               new EmailTemplates("CODPayment")
     //                               {

     //                                   EmailBody = " < center >   < table width =\"800\" style=\"border-color: #249469; border-width: 5px; border-style: solid; font-family: arial;font-size: 16px;color:#666; \" border=\"0\" cellspacing=\"10\" cellpadding=\"10\">     <tbody><tr>       <td align=\"center\"><img src=\"https://www.tolpagorni.com/hs-fs/hubfs/Tolpagorni2015ThinFRAME.png?width=218&amp;name=Tolpagorni2015ThinFRAME.png\" alt=\"logo\"></td>     </tr>     <tr>       <td><p>Dear [Name],</p><p></p><p align=\"justify\">Thank you for signing up to our training program.</p>       <p></p></td>     </tr>     <tr>       <td><br>         <table width=\"100%\" border=\"1\" style=\" border:#d7d7d7;font-family: arial;font-size: 14px;color:#666;text-align: center\" cellspacing=\"0\" cellpadding=\"5\">           <thead>             <tr style=\"background: #249469; color:#fff;\">                <th width=\"3%\" style=\"font-weight: 100;\">#</th>               <th width=\"35%\" style=\"font-weight: 100;\">Item Name</th>               <th width=\"5%\" style=\"font-weight: 100;\">Item Price</th>               <th width=\"3%\" style=\"font-weight: 100;\">Quantity</th>                            </tr>           </thead>           <tbody style=\"text-align: center\">             <tr><td>[Items]</td></tr>           </tbody>         </table>       </td>     </tr>     <tr>       <td style=\"color:#666;font-size: 15px;\">         <p> Regards,<br>           The Tolpa Team</p>                  </td>     </tr>     </tbody>   </table> </center>     ",
     //                                   SiteId = 1L,
     //                                   CreatedById = 1,
     //                                   CreationDate = DateTimeOffset.Now,
     //                                   CultureId = "en-US",
     //                                   Subject = "Payment",
     //                                   EmailTemplateTypeId = 1L,
     //                                   IsHTML = true,
     //                                   Status = EStatus.Active,
     //                                   StatusDate = DateTimeOffset.Now
     //                               },

     //                         new EmailTemplates("Invite")
     //                         {

     //                             EmailBody = "<center><table width=\"800\" style=\"border-color: #249469; border-width: 5px; border-style: solid; font-family: arial;font-size: 16px;color:#666; \" border=\"0\" cellspacing=\"10\" cellpadding=\"10\">     <tbody><tr>       <td align=\"center\"><img src=\"https://www.tolpagorni.com/hs-fs/hubfs/Tolpagorni2015ThinFRAME.png?width=218&amp;name=Tolpagorni2015ThinFRAME.png\" alt=\"logo\"></td>     </tr>     <tr>       <td>  <p>Dear [Name],</p><p>You have been invited by [purchaserName] to attend : </p><ul> [courseName] </ul>  <p></p>  </td>     </tr>         <tr>       <td style=\"color:#666;font-size: 15px;\">         <p> Regards,<br>           The Tolpa Team</p>                  </td>     </tr>     </tbody>   </table> </center>      ",
     //                             SiteId = 1L,
     //                             CreatedById = 1,
     //                             CreationDate = DateTimeOffset.Now,
     //                             CultureId = "en-US",
     //                             Subject = "Invite",
     //                             EmailTemplateTypeId = 2L,
     //                             IsHTML = true,
     //                             Status = EStatus.Active,
     //                             StatusDate = DateTimeOffset.Now
     //                         },
     //                          new EmailTemplates("ForgetPassword")
     //                          {

     //                              EmailBody = "<body bgcolor=\"#F8F8F8\" topmargin=\"0\" leftmargin=\"0\">    <center> <br><br>        <table cellpadding=\"0\" cellspacing=\"0\" bgcolor=\"#ffffff\" width=\"750\"            style=\"font-family: Roboto,sans-serif;font-size:10pt;font-weight:normal;line-height:16pt; border:solid 1px #CDCDCD;\">            <tr>                <td colspan=\"2\" style=\"padding-top:30px;padding-left:40px;padding-bottom:20px;text-align:justify;\">                    <p> <a href=\"https://newrepublic.se/\" title=\"New Republic\" target=\"_blank\"> <img                                src=\"https://newrepublic.se/Portals/Portal1/Upload/Block/Image/logo-newrepublic-black.png\"                                border=\"0\" alt=\"ESKADENIA\"> </a> <br><br> Greetings, <br><br>                    <p>You can change your password by ', </br><a href=\"[Link]\">click on the following link to                            continue:</a></p>                    </p>                </td>            </tr>            <tr>                <td style=\"padding-left:40px;padding-bottom:20px\"> Best Regards, <br> <strong>RAD Team </strong> <br> <a                        href=\"http://40.113.81.13/TimeTracking/sign-in\" title=\"Rad.com\" target=\"_blank\"> <img                            src=\"http://40.113.81.13/TimeTracking/assets/images/Rad-time-login-logo.png\" width=\"auto\"                            height=\"40\" alt=\"\" /> </a> </td>                <td style=\"padding-right:40px;padding-bottom:20px;padding-top: 20px;\" align=\"right\"> <a                        href=\"http://eskadenia.com/\" title=\"ESKADENIA.com\" target=\"_blank\"> <img alt=\"\"                            src=\"http://eskadenia.com/Portals/Portal1/Upload/Block/Image/eskadenia-email.png\" /> </a>                </td>            </tr>        </table>    </center></body>",
     //                              SiteId = 1L,
     //                              CreatedById = 1,
     //                              CreationDate = DateTimeOffset.Now,
     //                              CultureId = "en-US",
     //                              Subject = "DCMS Forgot Password",
     //                              EmailTemplateTypeId = 3L,
     //                              IsHTML = true,
     //                              Status = EStatus.Active,
     //                              StatusDate = DateTimeOffset.Now
     //                          }
     //);
        }
    }
}

