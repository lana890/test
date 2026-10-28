using EskaCommerce.UserCatalog.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.UserCatalog.Helpers
{
    public class InvoiceComparer : IEqualityComparer<UserInvoicesOutputVM>
    {
        public bool Equals(UserInvoicesOutputVM x, UserInvoicesOutputVM y)
        {
            return x.OrderId == y.OrderId;
        }
        public int GetHashCode(UserInvoicesOutputVM obj)
        {
            return obj.OrderId.GetHashCode() ^ (obj.OrderId == 0 ? 0 : obj.OrderId.GetHashCode());
        }
    } 
    public class UserPlanComparer : IEqualityComparer<SubscriberListVM>
    {
        public bool Equals(SubscriberListVM x, SubscriberListVM y)
        {
            return x.UserId == y.UserId;
        }
        public int GetHashCode(SubscriberListVM obj)
        {
            return obj.UserId.GetHashCode() ^ (obj.UserId == 0 ? 0 : obj.UserId.GetHashCode());
        }
    }
}
