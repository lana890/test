using Microsoft.AspNetCore.Mvc;
using EskaCMS.Infrastructure.Web;

namespace EskaCommerce.Payments.Components.PaymentCoD
{
    public class CoDLandingViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View(this.GetViewPath());
        }
    }
}
