using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Enum
{
    public enum ComparisonOperators
    {
        Equal=1,
        NotEqual=2,
        GreaterThan=3,
        LessThan=4,
        GreaterThanOrEqual=5,
        LessThanOrEqual=6

    }
    public enum TimeIntervals
    {
        Daily=1,
        Monthly=2,
        Weekly=3,
        Yearly=4,
        None=5
    }

    public enum GroupByEnum
    {
        Key = 1,
        TimeIntervals=2
    }
    public enum TypeMathematics
    {
        Sum=1,
        Count=2,
        None =3
    }
}
