using Newtonsoft.Json.Linq;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    /// <summary>
    /// The signed-in user a run reads (the visitor of a page flow, an order's customer), the way the
    /// pages read the signed-in user:
    ///  - Logged In User / User info: id, username, fullName, email, mobilephone, userType;
    ///  - User Option / User options: an option by its name, in any case;
    ///  - Current Username: username.
    /// Runs created before kept the user under other names (userName, name, phone, isGuest); both
    /// sets are readable, so an older run and an older flow read the same values.
    /// </summary>
    public static class FlowUser
    {
        /// <summary>The pages' name and the older server name of the same value.</summary>
        private static readonly (string Page, string Older)[] Names =
        {
            ("username", "userName"),
            ("fullName", "name"),
            ("mobilephone", "phone"),
        };

        /// <summary>DCMS's user type of a visitor who is not signed in (EUsersTypes.GuestClient).</summary>
        public const string GuestType = "GuestClient";

        /// <summary>The user with both sets of names; null when the run has no user.</summary>
        public static JObject? View(JToken? stored)
        {
            if (stored is not JObject user)
                return null;
            var view = (JObject)user.DeepClone();
            foreach (var (page, older) in Names)
            {
                if (view[page] == null && view[older] != null)
                    view[page] = view[older]!.DeepClone();
                else if (view[older] == null && view[page] != null)
                    view[older] = view[page]!.DeepClone();
            }
            if (view["isGuest"] == null && view["userType"]?.Type == JTokenType.String)
                view["isGuest"] = string.Equals((string?)view["userType"], GuestType, StringComparison.OrdinalIgnoreCase);
            else if (view["userType"] == null && view["isGuest"]?.Type == JTokenType.Boolean && (bool)view["isGuest"]!)
                view["userType"] = GuestType;
            return view;
        }

        /// <summary>One of the user's options by its name, in any case (null when the user has none by that name).</summary>
        public static JToken? Option(JToken? stored, string? name)
        {
            if (stored?["options"] is not JObject options || string.IsNullOrWhiteSpace(name))
                return null;
            var wanted = name.Trim();
            return options.Properties().FirstOrDefault(p => string.Equals(p.Name, wanted, StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}
