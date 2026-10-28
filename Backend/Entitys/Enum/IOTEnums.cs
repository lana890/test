using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Enums
{
    public class IOTEnums
    {
        public enum EIOTSIMStatus
        {
            Active = 1,
            Suspended = 2,
            Terminated = 3,
            WaitingForActivation = 4

        }

        public enum ChangePlanStatus
        {
            Completed = 1,
            Waiting = 2,
            Failed = 3,
            Deleted = 4,

        }

        public enum EInvoiceStatus
        {
            Paid = 1,
            UnPaid = 2,

        }
        public enum EDataUnit
        {
            KB = 0,
            MB = 1,
            GB = 2,
            TB = 3,

        }
        public enum ESIMsIdentifiers
        {
            ICCID = 1,
            IMSI = 2,
            ANY = 3

        }

        public enum ESIMsAction
        {
            ACTIVATE = 1,
            SUSPEND = 2,
            TERMINATE = 3,
            RESUME = 4,
            CHANGE_PLAN = 5,
            GET_SIM_USAGE = 6,
            UPDATE_SIM_DATA = 7,

        }


        public static class SIMsActions
        {
            public const string ACTIVATE = "STCIOTAS";
            public const string SUSPEND = "STCIOTSS";
            public const string TERMINATE = "";
            public const string RESUME = "STCIOTRS";
            public const string CHANGE_PLAN = "STCIOTCSS";
            public const string GET_SIM_USAGE = "STCIOTGSS";
            public const string UPDATE_SIM_DATA = "STCIOTUSD";
            public static string GetProperActionCode(ESIMsAction Action)
            {
                switch (Action)
                {
                    case ESIMsAction.ACTIVATE:
                        return ACTIVATE;
                    case ESIMsAction.SUSPEND:
                        return SUSPEND;
                    case ESIMsAction.TERMINATE:
                        return TERMINATE;
                    case ESIMsAction.RESUME:
                        return RESUME;
                    case ESIMsAction.CHANGE_PLAN:
                        return CHANGE_PLAN;
                    case ESIMsAction.GET_SIM_USAGE:
                        return GET_SIM_USAGE;
                    case ESIMsAction.UPDATE_SIM_DATA:
                        return UPDATE_SIM_DATA;
                    default:
                        return "";
                }
            }
            public static bool IsValidAction(string Action)
            {
                return (Action == ACTIVATE || Action == SUSPEND || Action == TERMINATE || Action == RESUME || Action == CHANGE_PLAN || Action == GET_SIM_USAGE || Action == UPDATE_SIM_DATA);
            }
            public static EIOTSIMStatus GetProperSIMStatus(ESIMsAction Action)
            {
                switch (Action)
                {
                    case ESIMsAction.ACTIVATE:
                        return EIOTSIMStatus.Active;
                    case ESIMsAction.TERMINATE:
                        return EIOTSIMStatus.Terminated;
                    case ESIMsAction.SUSPEND:
                        return EIOTSIMStatus.Suspended;
                    case ESIMsAction.RESUME:
                        return EIOTSIMStatus.Active;
                    default:
                        throw new Exception("Invalid Action.");
                }
            }


            public static bool CanMakeTheAction(ESIMsAction Action, EIOTSIMStatus SIMStatus)
            {
                switch (Action)
                {
                    case ESIMsAction.ACTIVATE:
                        return SIMStatus == EIOTSIMStatus.Terminated || SIMStatus == EIOTSIMStatus.WaitingForActivation;
                    case ESIMsAction.TERMINATE:
                        return SIMStatus == EIOTSIMStatus.Active || SIMStatus == EIOTSIMStatus.Suspended;

                    case ESIMsAction.SUSPEND:
                        return SIMStatus == EIOTSIMStatus.Active;
                    case ESIMsAction.RESUME:
                        return SIMStatus == EIOTSIMStatus.Suspended;
                    case ESIMsAction.CHANGE_PLAN:
                    case ESIMsAction.GET_SIM_USAGE:
                    case ESIMsAction.UPDATE_SIM_DATA:
                        return true;
                    default:
                        return false;
                }
            }
        }


        public static class SIMsIdentifiers
        {
            public const string IMSI = "IMSI";
            public const string ICCID = "ICCID";

            public static bool IsValidIdentifier(string Identifier)
            {
                return (Identifier == IMSI || Identifier == ICCID);
            }

        }

    }
}
