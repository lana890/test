//using EskaCMS.Core.Configurations;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.EmailSender.Data
{
    public class EmailSenderCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<SiteSettings>().HasSeedData(
            //    new SiteSettings("SmtpServer") { Module = "EmailSender", IsVisibleInCommonSettingPage = false, Value = "mail.eskadenia.com" },
            //    new SiteSettings("SmtpPort") { Module = "EmailSender", IsVisibleInCommonSettingPage = false, Value = "25" },
            //    new SiteSettings("SmtpUsername") { Module = "EmailSender", IsVisibleInCommonSettingPage = false, Value = "RADTime@Eskadenia.com" },
            //    new SiteSettings("SmtpPassword") { Module = "EmailSender", IsVisibleInCommonSettingPage = false, Value = "R@DT!me#123" }
            //);
        }
    }
}

