using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Enum
{
    public enum CustomerActionEnum
    {
        Export,
        Create_Group,
        Add_points,
        Assign_Coupon,
        Send_Email,
        Send_Greeting
    }
    public enum EmailType
    {
        Templete,
        Custome
    }
}
