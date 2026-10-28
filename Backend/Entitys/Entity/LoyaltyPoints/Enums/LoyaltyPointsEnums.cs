using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.LoyaltyPoints.Enums
{
    public class LoyaltyPointsEnums
    {
        public enum ELoyalityWalletStatus
        {
            Active = 1,
            Suspended = 2,
        }

        public enum EWalletOperationType
        {
            Gain = 1,
            Redeem = 2,
            Hold = 3,
            Refund = 4,
            Canceld = 5,
            GainByAdmin = 6,
            ReversedGain=7

        }

        public enum ELoyalitySource
        {
            Referrer = 1,
            ReferrerAndReferee = 2,
            NewUser = 3,
            ConsumeRange = 4,
            ConsumeRate = 5,
            SpecificProduct = 6,
        }
    }
}
