using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Services
{
    public class SmartVisionSMSResponse
    {

        public enum SmartVisionReposneCodes
        {
            sender_id_not_found = 1002,
            API_Key_not_found = 1003,
            smap_word_detected = 1004,
            internal_error = 1005,
            internal_error2 = 1006,
            low_balance = 1007,
            msg_not_set = 1008,
            sms_type_not_set = 1009,
            invalid_user_and_password = 1010,
            invalid_user_id = 1011,
            Success=200
        }
    }
}
