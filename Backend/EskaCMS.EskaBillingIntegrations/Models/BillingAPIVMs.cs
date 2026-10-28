using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models
{
    public class BillingAPIPayload<T>
    {
        public BillingAPIPayload(string Action, List<T> parameters)
        {
        //var TType = typeof(T);
        //if(TType.GetGenericTypeDefinition() == typeof(List<>))
        //{
        //        throw new Exception("Cannot create BillingAPIPayload object due Invalid Geniric T.");
        //}

            this.data = new PayloadDataVM<T>(Action, parameters);
        }

        public string source { get; set; } = "DCMS-ENGINE";
        public Guid requestId { get; set; } = Guid.NewGuid();
        public DateTimeOffset createDate { get; set; } = DateTimeOffset.Now;
        public PayloadDataVM<T> data { get; set; } 
  
    }

    public class PayloadDataVM<T>
    {

        public PayloadDataVM(string Action,List<T> parameters)
        {
            if (!SIMsActions.IsValidAction(Action))
            {
                throw new Exception("Cannot create ChangeStatusDataVM object due Invalid Action.");
            }
            this.eventCode = Action;
            this.parameters= parameters;
        }


        public long workFlowID { get; set; }
        public long bccContractNumber { get; set; }
        public long bccSubProductsId { get; set; }
        public long bccCustomerNumber { get; set; }
        public int priority { get; set; } = 1;
        public bool isBatch { get; set; } = false;
        public string eventCode { get; set; }
        public List<T> parameters { get; set; }


    }




    public class ChangeSimStatusParameter
    {
        public ChangeSimStatusParameter(string Identifier, List<string> SimIds, string ServiceProfile)
        {
            if (!SIMsIdentifiers.IsValidIdentifier(Identifier))
            {
                throw new Exception("Cannot create ChangeSimStatusParameter object due Invalid Identifier.");
            }
            this.Identifier = Identifier;
            this.SimIds = SimIds;
            this.ServiceProfile = ServiceProfile;

        }

        public List<string> SimIds { get; set; }
        public string Identifier { get; set; }
        public string ServiceProfile { get; set; }

    }

    public class ChangeSimPlanParameter
    {
        public ChangeSimPlanParameter(string Identifier, List<string> SimIds, long ServiceProfileId)
        {
            if (!SIMsIdentifiers.IsValidIdentifier(Identifier))
            {
                throw new Exception("Cannot create ChangeSimPlanParameter object due Invalid Identifier.");
            }
            this.Identifier = Identifier;
            this.SimIds = SimIds;
            this.ServiceProfileId = ServiceProfileId;

        }

        public List<string> SimIds { get; set; }
        public string Identifier { get; set; }
        public long ServiceProfileId { get; set; }

    }


}
