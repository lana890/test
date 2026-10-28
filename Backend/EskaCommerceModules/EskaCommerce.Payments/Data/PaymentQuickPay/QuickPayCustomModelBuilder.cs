using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCommerce.Payments.Data.PaymentQuickPay
{
    public class QuickPayCustomModelBuilder : ICustomModelBuilder
    {

        public void Build(ModelBuilder modelBuilder)
        {
            ////QP Quickpay    Quickpay Quickpay
//            modelBuilder.Entity<PaymentProvider>().HasSeedData(
//                                 new PaymentProvider("Quickpay") { Name = "Quick pay Payment", IsEnabled = true, SiteId = 1, AdditionalSettings = "{\"logo\": \"/Resources/qp-logo.svg\",\"apiKey\": \"6467fef9cb7101e367207059a4e2c310ab60090ab34269bd3e4bff843752276f\",\"privateKey\": \"9aca4aad12f9207810495583c0c532977f3a91958f01b743b63e8f65b4cb1002\",\"userKey\": \"c676a8eae1e856d5240b46139339de10f9feb66fb5dacf0f3acecee2c7306dd2\",\"apiSecret\": \"WdnlkIida6Fs9jIoReZPkx20ION5TIEA\",\"merchantId\": 119123,\"agreementId\": 432030}" }

//);


        }
    }
}

