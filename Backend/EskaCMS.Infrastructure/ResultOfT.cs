using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Infrastructure
{
    public class Result<TValue> : Result
    {
        public TValue Value { get; set; }

        protected internal Result(TValue value, bool success, string error)
            : base(success, error)
        {
            Value = value;
        }
    }

}
