using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Achievements.Enums
{
    public class AchievmentEnums
    {

        public enum AchievmentType
        {
            Logins = 1,
            Messages = 2,
            [Description("Chat Rooms")]
            ChatRooms = 3,
            [Description("Submit Data")]
            SubmitData = 4,
            [Description("Complete Profile Info")]
            CompleteProfileInfo = 5,
            [Description("Post Comments")]
            PostComments = 6,
            [Description("Product Comments")]
            ProductComments = 7,
        }

        public enum AchievementObtainType
        {
            Overall = 1,
            Daily = 2,
            Weekly = 3,
            
        }
    }
}
