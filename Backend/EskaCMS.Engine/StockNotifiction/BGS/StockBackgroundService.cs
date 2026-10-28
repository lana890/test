
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Engine;
using EskaCMS.Engine.PagePreviewCode.BGS;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCommerce.Module.Notifications.Notifiers;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCMS.Entities.Entity.Inventory;
using EskaCommerce.Notifications.Notifiers;

namespace EskaCMS.Engine.StockNotifiction.BGS
{
    public class StockBackgroundService : BaseBackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(3000));
        private readonly IConfiguration _configuration;
        public readonly ILogger<StockBackgroundService> _logger;
        public StockBackgroundService(IServiceProvider serviceProvider
            , ILogger<StockBackgroundService> logger
            , IConfiguration configuration)
            : base(nameof(StockBackgroundService), logger, serviceProvider, configuration)
        {
            _logger = logger;

            this.serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public override async Task<bool> Execute()
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var OrderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Order>>();
                var productRepository = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
                List<Product> products = await GetProductsHaveStocks(productRepository);
                if (products.Count > 0)
                {
                    var Query = await OrderRepository.Query().Include(x => x.OrderItems)
                                                             .ThenInclude(x => x.Product)
                                                             .Where(x => (x.OrderStatus == OrderStatus.PaymentReceived)).ToListAsync();


                    var Orders = Query.Where(x => x.OrderItems.Any(y => products.Any(t => t.Id == y.ProductId))).ToList();

                    if (Orders.Count > 0)
                    {
                        try
                        {
                            var siteSettingsRepository = scope.ServiceProvider.GetRequiredService<IRepositoryWithTypedId<SiteSettings, string>>();
                            var testNotifier = scope.ServiceProvider.GetRequiredService<ITestNotifier>();
                            var emailTemplateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
                            var stockRepo = scope.ServiceProvider.GetRequiredService<IRepository<Stock>>();

                            await SendNotification(Orders, products, siteSettingsRepository, testNotifier, emailTemplateService, stockRepo);

                            _logger.LogInformation($"Successfully Send");



                        }
                        catch (Exception)
                        {
                            _logger.LogInformation($"Failed Send");
                        }



                    }
                }
                else
                {
                    _logger.LogInformation($"No Product Have Stock");


                }
            }
            catch (Exception ex)
            {

                using var scope = serviceProvider.CreateScope();
                var dcmsLogService = scope.ServiceProvider.GetRequiredService<IdcmsLogService>();
                await dcmsLogService.AddError("Stock BackgroundService", "", ex.Message, "", null);
            }
            return true;
        }
        #region Helpers
        public async Task<bool> SendNotification(
            List<Order> orders,
            List<Product> products,
            IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
            ITestNotifier testNotifier,
            IEmailTemplateService emailTemplateService,
            IRepository<Stock> stockRepo)
        {
            try
            {
                var Settings = await siteSettingsRepository.Query().Where(x => x.Module == "StockNotification").ToListAsync();

                var PhoneNumber = Settings.Find(x => x.Id == "StockEmail").Value.Split(",").ToList();
                var EmailTemplate = Settings.Find(x => x.Id == "StockEmailTemplate").Value;

                var Notification = await CreateNotificationObject(orders, products, PhoneNumber, EmailTemplate, emailTemplateService, stockRepo);


                await testNotifier.SendMessageAsync(NotificationType.SMS, null, Notification, orders.Select(x => x.SiteId).FirstOrDefault());

                return true;


            }
            catch (Exception ex)
            {

                throw;
            }

        }
        public async Task<NotificationObject> CreateNotificationObject(
            List<Order> orders,
            List<Product> products,
            List<string> PhoneNumber,
            string Template,
            IEmailTemplateService emailTemplateService,
            IRepository<Stock> stockRepo)
        {
            NotificationObject Notificationobj = new NotificationObject();

            var ObjTemplate = await emailTemplateService.GetEmailTemplateById(Template.ToString().Trim());


            Notificationobj.MessageTitle = ObjTemplate.Subject;

            StringBuilder TotalConsumedAndAvalible = new StringBuilder("");

            var yesterday = DateTimeOffset.Now.AddDays(-1);
            var At9Am = new DateTimeOffset(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day, 9, 0, 0, new TimeSpan());
            var YesterdayMorning = new DateTimeOffset(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0, yesterday.Offset);
            var TodayMorning = new DateTimeOffset(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day, 0, 0, 0, DateTimeOffset.Now.Offset);


            StringBuilder SellToday = new StringBuilder();
            products = products.OrderBy(x => x.Price).ToList();
            foreach (var product in products)
            {
                var Avaliable = 0;

                var Consumers = 0;

                var SellProductToday = 0;
                foreach (var ordersItems in orders.Select(x => x.OrderItems).Where(x => x.Any(y => y.ProductId == product.Id)).ToList())
                {
                    var ComulativeItems = ordersItems.Where(x => x.Order.CreatedOn < TodayMorning.ToUniversalTime()).ToList();

                    Consumers += ordersItems.Select(x => x.Quantity).Sum();

                    var Items = ordersItems.Where(x => x.Order.CreatedOn < TodayMorning.ToUniversalTime() && x.Order.CreatedOn >= YesterdayMorning.ToUniversalTime()).ToList();

                    SellProductToday += Items.Select(x => x.Quantity).Sum();

                }

                var StockQuentity = await stockRepo.Query().Where(s => s.ProductId == product.Id).Select(x => x.Quantity).SumAsync();
                Avaliable = StockQuentity - Consumers;

                SellToday.Append($"{product.ProductDetails.Select(x => x.Name).FirstOrDefault()} = " + SellProductToday + System.Environment.NewLine);

                TotalConsumedAndAvalible.Append($"{product.ProductDetails.Select(x => x.Name).FirstOrDefault()} = " + Consumers + "/" + Avaliable + System.Environment.NewLine);
            }

            var Date = DateTimeOffset.Now.Date.AddDays(-1).ToShortDateString();

            Notificationobj.MessageBody = ObjTemplate.EmailBody;

            Notificationobj.SendValue = PhoneNumber;

            Notificationobj.Severity = 0;
            List<EmailParamsVM> Params = new();

            Params.Add(new EmailParamsVM { Param = "items", Value = TotalConsumedAndAvalible.ToString() });
            Params.Add(new EmailParamsVM { Param = "selltoday", Value = SellToday.ToString() });
            Params.Add(new EmailParamsVM { Param = "time", Value = Date.ToString() });

            Notificationobj.SMSProvider = SMSProviders.SMSUniFonic;
            Notificationobj.EmailOptions.Params = Params;
            Notificationobj.EmailOptions.TemplateId = Template.ToString().Trim();

            return Notificationobj;


        }

        public async Task<List<Product>> GetProductsHaveStocks(IRepository<Product> productRepository)
        {
            try
            {
                var Products = await productRepository.Query().Include(x => x.ProductDetails).ThenInclude(x => x.Culture).Where(x => x.StockTrackingIsEnabled).ToListAsync();

                return Products;

            }
            catch (Exception)
            {

                return new List<Product>();
            }
        }
        #endregion
    }

}



