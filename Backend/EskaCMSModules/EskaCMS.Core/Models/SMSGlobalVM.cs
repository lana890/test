using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Models
{
  public  class SMSGlobalVM
    {

        public SMSGlobalVM()
        {
        //    messages = new List<string>();
        }
        public string destination { get; set; }
       // public List<string> destinations { get; set; }
        public string message { get; set; }
        public string origin { get; set; }
      //  public List<string> messages { get; set; }
        
        //  public DateTimeOffset scheduledDateTime { get; set; }

    }
}
