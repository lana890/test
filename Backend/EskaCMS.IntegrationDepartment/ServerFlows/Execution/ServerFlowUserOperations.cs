using System.Globalization;
using System.Security.Cryptography;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Services.Interfaces;
using EskaCMS.EmailSender.SMTP.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    /// <summary>A refusal of an account operation (bad input, or DCMS said no): not retried.</summary>
    public sealed class ServerFlowOperationException : Exception
    {
        public ServerFlowOperationException(string message) : base(message) { }
    }

    /// <summary>
    /// The DCMS account operations of a flow (ServerOperations.EnsureUser / SetUserOptions), the
    /// server's version of what the CustomerPortal does after a payment:
    ///  - EnsureUser: the customer's account if they have one, else a new one made by DCMS's own
    ///    Register (same role, site, user options and wallet as the registration page), and, when a
    ///    template is given, the CP e-mail with the user name and a generated password;
    ///  - SetUserOptions: AddUpdateUserOptions for one user (e.g. customerId after the CRM converts the lead).
    /// Runs on the call's own scope (FlowCaller.RunIsolated), so the site header is the order's site.
    /// </summary>
    public sealed class ServerFlowUserOperations
    {
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnopqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Symbols = "!@#$%^&*?_-";

        private readonly IRepository<User> _users;
        private readonly IRepository<UsersSites> _userSites;
        private readonly IRepository<UserOptions> _options;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<Role> _roles;
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _templates;
        private readonly IdcmsLogService _log;

        public ServerFlowUserOperations(
            IRepository<User> users,
            IRepository<UserOptions> options,
            IRepository<UsersSites> userSites,
            Microsoft.AspNetCore.Identity.RoleManager<Role> roles,
            IUserService userService,
            IEmailSender emailSender,
            IEmailTemplateService templates,
            IdcmsLogService log)
        {
            _users = users;
            _userSites = userSites;
            _options = options;
            _roles = roles;
            _userService = userService;
            _emailSender = emailSender;
            _templates = templates;
            _log = log;
        }

        /// <summary>
        /// Body: { preferUserId, userName, lookupOption, lookupValue, email, phoneNumber, fullName,
        /// dateOfBirth (dd/MM/yyyy or ISO), roleId, groupId, status, userOptions: [{ PropertyName,
        /// Value, IsVisible, IsEditable }], welcomeTemplateId, passwordLength }.
        /// The account is found by the PERSON's identity only (the national id for ASIC): the order's own
        /// user (preferUserId) only when its user name or its lookupOption equals this identity, then
        /// the user name, then the user option lookupOption = lookupValue on the site. Never by e-mail
        /// (families share one) and never the buyer's account for someone else's policy. Guests and
        /// deleted users never match, and neither do accounts that are not customers' (integrations,
        /// accounts without the Customer role; see CustomerAccounts): a flow never touches
        /// those. Only when nothing matches is an account created, never with an admin role.
        /// reportOptions: ["customerId", ...] answers those options of the account as "options", so the
        /// flow can decide with a condition (e.g. set customerId only when it is empty).
        /// </summary>
        public async Task<JToken> EnsureUserAsync(JToken body, long siteId)
        {
            var started = DateTimeOffset.Now;
            var b = body as JObject ?? throw new ServerFlowOperationException("EnsureUser needs an object body.");
            var userName = Text(b, "userName");
            if (userName.Length == 0)
                throw new ServerFlowOperationException("EnsureUser needs userName.");
            var normalizedUserName = userName.ToUpperInvariant();
            var lookupOption = Text(b, "lookupOption").ToUpperInvariant();
            var lookupValue = Text(b, "lookupValue").ToUpperInvariant();
            var report = ReadNames(b["reportOptions"]);

            // 1. the account that placed the order, when it is a real one AND the same person
            if (b["preferUserId"] is { } prefer && long.TryParse(prefer.ToString(), out var preferId) && preferId > 0)
            {
                var own = await CustomerOfSite(siteId)
                    .Where(u => u.Id == preferId && (u.NormalizedUserName == normalizedUserName
                        || (lookupOption.Length > 0 && lookupValue.Length > 0
                            && u.UserOptions.Any(o => o.NormalizedPropertyName == lookupOption && o.NormalizedValue == lookupValue))))
                    .Select(u => new { u.Id, u.UserName }).FirstOrDefaultAsync();
                if (own != null)
                    return await FoundAsync(own.Id, own.UserName, "orderUser", report);
            }

            // 2. the same user name (the national id for ASIC)
            if (await ByUserNameAsync(normalizedUserName, siteId, report) is { } byName)
                return byName;

            // 3. the same user option (e.g. nationalId), on this site
            if (lookupOption.Length > 0 && lookupValue.Length > 0)
            {
                var byOption = await CustomerOfSite(siteId)
                    .Where(u => u.UserOptions.Any(o => o.NormalizedPropertyName == lookupOption && o.NormalizedValue == lookupValue))
                    .OrderBy(u => u.Id)
                    .Select(u => new { u.Id, u.UserName }).FirstOrDefaultAsync();
                if (byOption != null)
                    return await FoundAsync(byOption.Id, byOption.UserName, "option", report);
            }

            // 4. nobody: create the account the way the registration page does
            var email = Text(b, "email");
            if (email.Length == 0)
                throw new ServerFlowOperationException("EnsureUser needs an e-mail to create an account: without one the customer could never receive the login details or reset the password.");
            if (!long.TryParse(Text(b, "roleId"), out var roleId) || roleId <= 0)
                throw new ServerFlowOperationException("EnsureUser needs roleId to create an account.");
            var role = await _roles.Roles.AsNoTracking().Where(r => r.Id == roleId).Select(r => new { r.IsSystemAdmin, r.Type }).FirstOrDefaultAsync();
            if (role == null)
                throw new ServerFlowOperationException($"EnsureUser's roleId {roleId} is not a role of DCMS.");
            // Role.Type does not tell customers from admins: DCMS seeds every role with Type Admin, its
            // Customer role (id 6, held by the customers of all sites) included, and gives new customers
            // that role by id (ERoleTypes.Customer). A site's own back-office roles carry Type Admin too.
            // So: the Customer role, or a role typed Customer, and never a system-admin role.
            var customersRole = roleId == CustomerRoleId || role.Type == ERoleTypes.Customer;
            if (role.IsSystemAdmin || !customersRole)
                throw new ServerFlowOperationException($"Role {roleId} is not the customers' role; a flow only creates customers' accounts. Give EnsureUser roleId {CustomerRoleId} (Customer) or a role of type Customer.");

            // User names are unique across ALL sites and all states, and Register hides the reason
            // ("Something went wrong"): say it plainly instead.
            var taken = await _users.Query().AsNoTracking()
                .Where(u => u.NormalizedUserName == normalizedUserName)
                .Select(u => new { u.Id, u.Status, u.Type }).FirstOrDefaultAsync();
            if (taken != null)
                throw new ServerFlowOperationException(
                    $"User name \"{userName}\" is taken by user {taken.Id} ({taken.Type switch { EUsersTypes.GuestClient => "a guest", EUsersTypes.Customer => taken.Status.ToString(), EUsersTypes.Integration => "an integration account", _ => "an account without the Customer role" }}), which cannot be used for this customer. Fix that account, then try again.");

            var passwordLength = int.TryParse(Text(b, "passwordLength"), out var len) ? Math.Clamp(len, 10, 64) : 14;
            var password = NewPassword(passwordLength);
            var fullName = Text(b, "fullName");
            var model = new RegisterViewModel
            {
                UserName = userName,
                Email = email.Length > 0 ? email : null,
                FullName = fullName.Length > 0 ? fullName : userName,
                Password = password,
                PhoneNumber = Text(b, "phoneNumber") is { Length: > 0 } phone ? phone : null,
                RoleId = roleId,
                GroupId = long.TryParse(Text(b, "groupId"), out var groupId) && groupId > 0 ? groupId : null,
                DateOfBirth = ParseDate(Text(b, "dateOfBirth")),
                Status = int.TryParse(Text(b, "status"), out var status) && Enum.IsDefined(typeof(EStatus), status) ? (EStatus)status : EStatus.Active,
                SiteId = siteId,
                UserOptions = ReadOptions(b["userOptions"]),
            };

            long newId;
            try
            {
                newId = (await _userService.Register(model)).UserId;
            }
            catch (Exception ex)
            {
                // Register wraps everything in "Something Went Wrong !<reason>": keep the reason.
                var reason = ex.GetBaseException().Message;
                var made = await _users.Query().AsNoTracking()
                    .Where(u => u.NormalizedUserName == normalizedUserName)
                    .Select(u => new { u.Id, u.CreationDate }).FirstOrDefaultAsync();
                // Made since this call began: Register made it and then failed on the rest (or another
                // run made it at the same moment). Nobody can tell which, and its password was never
                // sent: a person looks.
                if (made != null && made.CreationDate >= started)
                    throw new ServerFlowOperationException(
                        $"The account \"{userName}\" (user {made.Id}) was made, but DCMS then refused the rest ({reason}); its login details were not e-mailed. Check the account, then record the result.");
                // Made by another run before this one began: that account is the one.
                if (await ByUserNameAsync(normalizedUserName, siteId, report) is { } madeBefore)
                    return madeBefore;
                throw new ServerFlowOperationException($"Creating the account was refused: {reason}");
            }

            var emailSent = false;
            var template = Text(b, "welcomeTemplateId");
            if (template.Length > 0)
                emailSent = await SendCredentialsAsync(template, email, userName, password, siteId);

            var answer = new JObject
            {
                ["userId"] = newId,
                ["userName"] = userName,
                ["created"] = true,
                ["matchedBy"] = "new",
                ["credentialsEmailed"] = emailSent,
            };
            if (report.Count > 0)
            {
                // what the new account now holds: the options it was created with
                var options = new JObject();
                foreach (var name in report)
                    options[name] = model.UserOptions.FirstOrDefault(o => string.Equals(o.PropertyName, name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";
                answer["options"] = options;
            }
            return answer;
        }

        /// <summary>
        /// Body: { userId, userOptions: [{ PropertyName, Value, IsVisible, IsEditable }] }.
        /// An option the user already has is updated when it is editable (as DCMS does) and left as
        /// it is otherwise; a new one is added. Answers what was added, updated and skipped.
        /// </summary>
        public async Task<JToken> SetUserOptionsAsync(JToken body, long siteId)
        {
            var b = body as JObject ?? throw new ServerFlowOperationException("AddUpdateUserOptions needs an object body.");
            if (!long.TryParse(Text(b, "userId"), out var userId) || userId <= 0)
                throw new ServerFlowOperationException("AddUpdateUserOptions needs userId.");
            if (!await CustomerOfSite(siteId).AnyAsync(u => u.Id == userId))
                throw new ServerFlowOperationException($"User {userId} is not a customer of site {siteId}; a flow only changes customers' accounts.");

            var wanted = ReadOptions(b["userOptions"]);
            if (wanted.Count == 0)
                throw new ServerFlowOperationException("AddUpdateUserOptions needs at least one user option.");

            var existing = await _options.Query().Where(o => o.UserId == userId).OrderBy(o => o.Id).ToListAsync();
            var added = new JArray();
            var updated = new JArray();
            var skipped = new JArray();
            var now = DateTimeOffset.Now;

            foreach (var option in wanted)
            {
                var current = existing.FirstOrDefault(o => string.Equals(o.PropertyName, option.PropertyName, StringComparison.OrdinalIgnoreCase));
                if (current == null)
                {
                    _options.Add(new UserOptions
                    {
                        UserId = userId,
                        PropertyName = option.PropertyName,
                        Value = option.Value,
                        IsEditable = option.IsEditable,
                        IsVisible = option.IsVisible,
                        ControlType = option.ControlType,
                        CreationDate = now,
                    });
                    added.Add(option.PropertyName);
                }
                else if (current.Value == option.Value)
                {
                    skipped.Add(option.PropertyName); // already right
                }
                else if (current.IsEditable)
                {
                    current.Value = option.Value;
                    current.IsVisible = option.IsVisible;
                    current.IsEditable = option.IsEditable;
                    current.ModificationDate = now;
                    updated.Add(option.PropertyName);
                }
                else
                {
                    skipped.Add(option.PropertyName);
                }
            }

            await _options.SaveChangesAsync();
            return new JObject { ["userId"] = userId, ["added"] = added, ["updated"] = updated, ["skipped"] = skipped };
        }

        /// <summary>
        /// Customers' accounts that are not deleted: accounts typed Customer, and accounts that hold the
        /// Customer role whatever their type says: older accounts carry no type at all (Type 0, e.g.
        /// user 10946369 on der3) and staff buy with their back-office account (Type Admin). Never
        /// guests or integrations, and never an account that is neither typed Customer nor holds the
        /// Customer role. A flow never sends an existing account its login details or changes its
        /// password; it only links to it.
        /// </summary>
        private IQueryable<User> CustomerAccounts() =>
            _users.Query().AsNoTracking()
                .Where(u => u.Status != EStatus.Deleted
                    && (u.Type == EUsersTypes.Customer
                        || (u.Type != EUsersTypes.GuestClient && u.Type != EUsersTypes.Integration
                            && u.UserRoles.Any(r => r.RoleId == CustomerRoleId))));

        /// <summary>The site's customers' accounts (see CustomerAccounts).</summary>
        private IQueryable<User> CustomerOfSite(long siteId) =>
            CustomerAccounts().Where(u => u.UserSites.Any(s => s.SiteId == siteId));

        private const long CustomerRoleId = (long)ERoleTypes.Customer;

        /// <summary>
        /// The customer with this user name, on any site (user names are unique across sites); linked
        /// to this site when it was not yet, as DCMS InsureUser does. Null when there is none.
        /// </summary>
        private async Task<JObject?> ByUserNameAsync(string normalizedUserName, long siteId, IReadOnlyList<string> report)
        {
            var byName = await CustomerAccounts()
                .Where(u => u.NormalizedUserName == normalizedUserName)
                .OrderBy(u => u.Id)
                .Select(u => new { u.Id, u.UserName, OnSite = u.UserSites.Any(s => s.SiteId == siteId) })
                .FirstOrDefaultAsync();
            if (byName == null)
                return null;
            if (!byName.OnSite)
            {
                _userSites.Add(new UsersSites { UserId = byName.Id, SiteId = siteId });
                await _userSites.SaveChangesAsync();
            }
            return await FoundAsync(byName.Id, byName.UserName, "userName", report);
        }

        /// <summary>An existing account (nothing is sent to it), with the options the flow asked about.</summary>
        private async Task<JObject> FoundAsync(long id, string? userName, string matchedBy, IReadOnlyList<string> report)
        {
            var answer = new JObject
            {
                ["userId"] = id,
                ["userName"] = userName,
                ["created"] = false,
                ["matchedBy"] = matchedBy,
                ["credentialsEmailed"] = false,
            };
            if (report.Count > 0)
            {
                var wanted = report.Select(n => n.ToUpperInvariant()).ToList();
                var rows = await _options.Query().AsNoTracking()
                    .Where(o => o.UserId == id && wanted.Contains(o.NormalizedPropertyName))
                    .OrderBy(o => o.Id)
                    .Select(o => new { o.PropertyName, o.Value }).ToListAsync();
                var options = new JObject();
                foreach (var name in report)
                    options[name] = rows.FirstOrDefault(r => string.Equals(r.PropertyName, name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";
                answer["options"] = options;
            }
            return answer;
        }

        private static List<string> ReadNames(JToken? token) =>
            token is JArray names
                ? names.Where(n => n.Type == JTokenType.String).Select(n => ((string)n!).Trim()).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();

        /// <summary>
        /// CP's AutoRegistration e-mail: template params "email" (the user name) and "password".
        /// Rendered here and handed straight to the mail sender, NOT through the notifier: the
        /// notifier writes every e-mail's params and body into the DCMS log, which would keep the
        /// customer's password there in plain text. A missing template is "not sent", never a blank mail.
        /// </summary>
        private async Task<bool> SendCredentialsAsync(string templateId, string email, string userName, string password, long siteId)
        {
            try
            {
                var template = await _templates.GetEmailTemplateById(templateId);
                if (template == null || string.IsNullOrWhiteSpace(template.EmailBody))
                {
                    await _log.AddError("ServerFlows.EnsureUser", $"E-mail template \"{templateId}\" not found", $"Account {userName} created, but the login e-mail was not sent: template \"{templateId}\" does not exist on site {siteId}.", null, siteId);
                    return false;
                }

                // The same bare-word replacement the notifier does for CP's template.
                var body = template.EmailBody.Replace("email", userName).Replace("password", password);
                await _emailSender.SendEmailAsync(new List<string> { email }, null, template.Subject, body, siteId, null, true);
                return true;
            }
            catch (Exception ex)
            {
                // The account exists; the customer can still reset the password. Never log the password.
                await _log.AddError("ServerFlows.EnsureUser", ex.GetBaseException().Message, $"Account {userName} created, but the login e-mail (template {templateId}) was not sent.", null, siteId);
                return false;
            }
        }

        private static List<UserOptionsVM> ReadOptions(JToken? token)
        {
            var list = new List<UserOptionsVM>();
            if (token is not JArray items)
                return list;
            foreach (var item in items.OfType<JObject>())
            {
                var name = (string?)(item["PropertyName"] ?? item["propertyName"] ?? item["Key"] ?? item["key"]);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var value = item["Value"] ?? item["value"];
                list.Add(new UserOptionsVM
                {
                    PropertyName = name.Trim(),
                    Value = value == null || value.Type == JTokenType.Null ? "" : value.Type == JTokenType.String ? (string)value! : value.ToString(Newtonsoft.Json.Formatting.None),
                    IsVisible = Flag(item["IsVisible"] ?? item["isVisible"], true),
                    IsEditable = Flag(item["IsEditable"] ?? item["isEditable"], true),
                    ControlType = (EControlTypes)1,
                });
            }
            return list;
        }

        private static bool Flag(JToken? token, bool fallback) => token?.Type switch
        {
            JTokenType.Boolean => (bool)token,
            JTokenType.String => bool.TryParse((string?)token, out var b) ? b : fallback,
            JTokenType.Integer => (long)token != 0,
            _ => fallback,
        };

        private static string Text(JObject body, string key)
        {
            var token = body[key];
            return token == null || token.Type == JTokenType.Null ? "" : (token.Type == JTokenType.String ? (string)token! : token.ToString()).Trim();
        }

        /// <summary>dd/MM/yyyy (the journeys' Gregorian birth date) or an ISO date.</summary>
        private static DateTimeOffset? ParseDate(string text)
        {
            if (text.Length == 0)
                return null;
            if (DateTime.TryParseExact(text, new[] { "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dmy))
                return new DateTimeOffset(dmy, TimeSpan.Zero);
            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var iso) ? iso : null;
        }

        /// <summary>Upper, lower, digit and symbol at least once each, from a cryptographic source; no look-alike characters.</summary>
        internal static string NewPassword(int length)
        {
            var all = Upper + Lower + Digits + Symbols;
            var chars = new char[length];
            chars[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
            chars[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
            chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            chars[3] = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];
            for (var i = 4; i < length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            RandomNumberGenerator.Shuffle(chars.AsSpan());
            return new string(chars);
        }
    }
}
