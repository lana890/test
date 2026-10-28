using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Enums
{
    public enum EProvider
    {
        MySql=1,
        PostgreSQL=2,
        SqlServer=3,
        Oracle=4
    }
    public enum EEntity
    {
        Product = 1,
        Post = 2,
        Category = 3,
        Page = 4,
        Brand = 5,
        None = 6
    }
}
