using System.Threading.Tasks;
using EskaCMS.Infrastructure.Web;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.EmailSender.SMTP.Services;
using System.Collections.Generic;

namespace EskaCommerce.Orders.Services
{
    public class OrderEmailService : IOrderEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly IRazorViewRenderer _viewRender;
        public OrderEmailService(IEmailSender emailSender, IRazorViewRenderer viewRender)
        {
            _emailSender = emailSender;
            _viewRender = viewRender;
        }

        //public async Task SendEmailToUser(User user, Order order)
        //{

        //    var emailBody = await _viewRender.RenderViewToStringAsync("/Areas/Orders/Views/EmailTemplates/OrderEmailToCustomer.cshtml", order);
        //    var emailSubject = $"Order information #{order.Id}";
        //    var lst = new List<string>();
        //    lst.Add(user.Email);
        //    await _emailSender.SendEmailAsync(lst, emailSubject, emailBody, 1, true);
        //}
    }
}
