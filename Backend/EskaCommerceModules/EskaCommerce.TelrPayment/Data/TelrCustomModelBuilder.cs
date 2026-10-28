using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.TelrPayment.Data
{
    public class TelrCustomModelBuilder : ICustomModelBuilder
    {

        public void Build(ModelBuilder modelBuilder)
        {
 //           modelBuilder.Entity<PaymentProvider>().HasSeedData(
 //new PaymentProvider("TELR") { Name = "Telr Payment", IsEnabled = true, SiteId = 1, AdditionalSettings = "{ \"BaseAddress\":https://secure.telr.com/, \"URLType\":gateway/order.json\"\", \"Authkey\":c5S9q@9Xr5W^xkcg\"\", \"return_auth\":test\"\", \"return_can\":testcan\"\", \"return_decl\":testdecl\"\", \"TestMode\":1\"\", \"DataIntegritySecretkey\":WcVJh#b2#\"\", \"StoreId\":24569\"\" }" }
 //);

 //           modelBuilder.Entity<SiteSettings>().HasSeedData(

 //                     new SiteSettings("TransactionSearchCartId") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "https://secure.telr.com/tools/api/xml/transaction/:ref/cart", SiteId = 1L },
 //              new SiteSettings("TransactionSearchEmail") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "https://secure.telr.com/tools/api/xml/transaction/:addr/cart", SiteId = 1L },
 //              new SiteSettings("TransactionDetails") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "https://secure.telr.com/tools/api/xml/transaction/:addr/cart", SiteId = 1L },
 //              new SiteSettings("AgreementList") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "https://secure.telr.com/tools/api/xml/agreement", SiteId = 1L },
 //              new SiteSettings("AgreementDetails") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "https://secure.telr.com/tools/api/xml/agreement/:agreement_id", SiteId = 1L },
 //              new SiteSettings("AgreementHistory") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "https://secure.telr.com/tools/api/xml/agreement/:agreement_id/history", SiteId = 1L },
 //              new SiteSettings("CancelAgreement") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "  https://secure.telr.com/tools/api/xml/agreement/:agreement_id", SiteId = 1L },
 //              new SiteSettings("AuthUsername") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "15156", SiteId = 1L },
 //              new SiteSettings("AuthPassword") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "107e0759M$LBM6T7gTk#9b2q", SiteId = 1L },
 //                new SiteSettings("RefundPayment") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "{ \"StoreId\":\"24569\", \"Key\":\"hsHp~2wz6B^ddwB5\", \"Type\":\"Refund\", \"class\":\"ecom\", \"Currency\":\"AED\" ,\"Test\":\"1\", \"URL\":\"https://secure.telr.com/gateway/remote.xml\"}", SiteId = 1L },
 //                new SiteSettings("PaymentHold") { Module = "TelrPayment", IsVisibleInCommonSettingPage = false, Value = "{ \"Key\":\"Email1\", \"Value\":\"o.alkhdour@hotmail.com\"}", SiteId = 1L }
 //          );

        }
    }
}

