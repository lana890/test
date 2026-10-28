using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.OrderCancelation.Scripts
{
    public static class MakeProductSerialAvaliable
    {
        public static string MakeSerialsAvaliable = @"update catalog_productserial set Status = 1,OrderId = null where OrderId={0};";

    }
}
