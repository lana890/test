using System.Collections.Concurrent;
using System.Globalization;
using EskaCMS.Core.Services;
using EskaCMS.Core.ViewModels;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Execution
{
    public enum FlowCallStatus
    {
        /// <summary>The call answered (it may still report a business error inside the body).</summary>
        Ok = 1,

        /// <summary>Not sent, or refused before anything could change: a setting is wrong.</summary>
        NotSent = 2,

        /// <summary>Sent, but it ended without an answer we can trust (error, timeout, crash).</summary>
        Unclear = 3,
    }

    public sealed class FlowCallResult
    {
        public FlowCallStatus Status { get; init; }

        public JToken? Response { get; init; }

        public string? Error { get; init; }

        /// <summary>For a call that did not answer 2xx: the other side's body, when the error carried one.</summary>
        public JToken? ErrorBody { get; init; }

        public static FlowCallResult Ok(JToken? response) => new() { Status = FlowCallStatus.Ok, Response = response };

        public static FlowCallResult NotSent(string error) => new() { Status = FlowCallStatus.NotSent, Error = error };

        public static FlowCallResult Unclear(string error, JToken? errorBody = null) => new() { Status = FlowCallStatus.Unclear, Error = error, ErrorBody = errorBody };
    }

    /// <summary>Who a call runs as: the order's site, customer, culture and cart.</summary>
    public sealed record FlowIdentity(long SiteId, long UserId, long CultureId, long? CartId);

    /// <summary>
    /// Sends one request. Every call runs in its own DI scope, marked as a server scope (so the
    /// server-only data sources accept it) and given a request with the order's site, customer,
    /// culture and cart headers - what the data source invoker and the notifier read.
    ///
    /// The scope lives exactly as long as the call: when the runner stops waiting (timeout) the call
    /// finishes on its own scope and database connection, never on the runner's.
    /// </summary>
    public sealed class FlowCaller
    {
        public const string TokensSection = "ServerFlows:Tokens";

        private const int MaxErrorLength = 1000;

        /// <summary>DCMS's customer group of guests (GuestController puts every guest in it).</summary>
        private const long GuestCustomerGroupId = 2;

        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FlowCaller> _logger;

        /// <summary>Each site's "Server flows" account, once found or made (see <see cref="SiteAccountAsync"/>).</summary>
        private readonly ConcurrentDictionary<long, long> _siteAccounts = new();
        private readonly SemaphoreSlim _findingAccount = new(1, 1);

        public FlowCaller(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<FlowCaller> logger)
        {
            _scopes = scopes;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<FlowCallResult> CallDataSourceAsync(long dataSourceId, JToken payload, string? tokenName, int? timeoutSec, FlowIdentity who)
        {
            var token = "";
            if (tokenName != null)
            {
                token = _configuration[$"{TokensSection}:{tokenName}"] ?? "";
                if (token.Length == 0)
                    return FlowCallResult.NotSent($"The token \"{tokenName}\" is not set ({TokensSection}:{tokenName} in the server configuration).");
            }

            try
            {
                who = await WithAccountAsync(who);
            }
            catch (Exception ex)
            {
                return FlowCallResult.NotSent(NoAccount(who.SiteId, ex));
            }

            var call = RunIsolated(who, async sp =>
            {
                var mapper = sp.GetRequiredService<IApiMapper>();
                object? result = await mapper.HandleWebAPIRequests(dataSourceId, payload, who.UserId, token);
                return ToToken(result);
            });

            try
            {
                var response = timeoutSec.HasValue
                    ? await call.WaitAsync(TimeSpan.FromSeconds(timeoutSec.Value))
                    : await call;
                return FlowCallResult.Ok(response);
            }
            catch (TimeoutException)
            {
                // The call goes on in its own scope; keep its failure from going unobserved.
                _ = call.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return FlowCallResult.Unclear($"No answer within {timeoutSec} s.");
            }
            catch (UnauthorizedAccessException ex)
            {
                // Raised by the invoker before sending (e.g. the data source is server-only and the
                // scope flag is missing): a setting, not the remote side.
                return FlowCallResult.NotSent(Trim(ex.Message));
            }
            catch (FileResponseException ex)
            {
                return FlowCallResult.NotSent(ex.Message);
            }
            catch (Exception ex)
            {
                var message = ex.GetBaseException().Message;
                // A setting, not the remote side: asking again cannot help, so the run stops at once
                // with the reason instead of retrying for minutes while the page hears "still working".
                if (IsMissingDataSource(message))
                    return FlowCallResult.NotSent($"Data source {dataSourceId} does not exist on this site, or is switched off. Use an existing data source in the flow.");
                // The other side's answer stays whole in the message, so the runner can mask it
                // (StepPayload.SafeError) before the text is kept anywhere; it cuts it after that.
                var body = StepPayload.FromRemoteError(message);
                return FlowCallResult.Unclear(body == null ? Trim(message) : message, body);
            }
        }

        /// <summary>What the data-source invoker throws when the id is not one of the site's active data sources.</summary>
        internal static bool IsMissingDataSource(string? message) =>
            message != null && message.Trim().StartsWith("Data Source Not configuered", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// A local request: one of the DCMS operations in <see cref="ServerOperations"/>, run in-process
        /// and always waited for to the end (the parser says so about a Timeout on one).
        /// </summary>
        public async Task<FlowCallResult> RunLocalAsync(string operation, JToken payload, FlowIdentity who)
        {
            if (operation == ServerOperations.Notify)
                return await SendNotificationAsync(payload, who);
            if (operation != ServerOperations.EnsureUser && operation != ServerOperations.SetUserOptions && operation != ServerOperations.ChangeOrderStatus)
                return FlowCallResult.NotSent($"Unknown server operation \"{operation}\".");

            try
            {
                who = await WithAccountAsync(who);
            }
            catch (Exception ex)
            {
                return FlowCallResult.NotSent(NoAccount(who.SiteId, ex));
            }

            try
            {
                var answer = await RunIsolated(who, sp =>
                {
                    if (operation == ServerOperations.ChangeOrderStatus)
                        return sp.GetRequiredService<ServerFlowOrderOperations>().ChangeStatusAsync(payload, who.SiteId);
                    var accounts = sp.GetRequiredService<ServerFlowUserOperations>();
                    return operation == ServerOperations.EnsureUser
                        ? accounts.EnsureUserAsync(payload, who.SiteId)
                        : accounts.SetUserOptionsAsync(payload, who.SiteId);
                });
                return FlowCallResult.Ok(answer);
            }
            catch (ServerFlowOperationException ex)
            {
                // DCMS said no (bad input, e-mail taken ...): repeating gets the same answer.
                return FlowCallResult.NotSent(Trim(ex.Message));
            }
            catch (Exception ex)
            {
                return FlowCallResult.Unclear(Trim(ex.GetBaseException().Message));
            }
        }

        public async Task<FlowCallResult> SendNotificationAsync(JToken payload, FlowIdentity who)
        {
            NotificationObject? data;
            try
            {
                data = payload.ToObject<NotificationObject>();
            }
            catch (JsonException ex)
            {
                return FlowCallResult.NotSent($"The notification body does not fit /Notification/send: {Trim(ex.Message)}");
            }
            if (data == null)
                return FlowCallResult.NotSent("The notification body is empty.");

            try
            {
                var sent = await RunIsolated(who, async sp =>
                {
                    var notifier = sp.GetRequiredService<ITestNotifier>();
                    var result = await notifier.SendMessageAsync(data.NotificationType, null, data, who.SiteId);
                    return result == null ? JValue.CreateNull() : JToken.FromObject(result);
                });
                return FlowCallResult.Ok(sent);
            }
            catch (Exception ex)
            {
                return FlowCallResult.Unclear(Trim(ex.GetBaseException().Message));
            }
        }

        /// <summary>The user name of a site's "Server flows" account.</summary>
        public static string SiteAccountUserName(long siteId) =>
            string.Create(CultureInfo.InvariantCulture, $"server-flows.site{siteId}@guest.eskacms.com");

        /// <summary>
        /// Who a call runs as. A run that has no user (another system's call, a set time, an alert)
        /// calls as the site's "Server flows" account: DCMS writes the calling user on rows such as
        /// the data source log, a foreign key to Core_User, so user 0 cannot call at all.
        /// </summary>
        private async Task<FlowIdentity> WithAccountAsync(FlowIdentity who) =>
            who.UserId > 0 ? who : who with { UserId = await SiteAccountAsync(who.SiteId) };

        private static string NoAccount(long siteId, Exception ex) =>
            Trim($"This call has no user, and the site's \"Server flows\" account ({SiteAccountUserName(siteId)}) could not be found or made: {ex.GetBaseException().Message}");

        /// <summary>
        /// The site's "Server flows" account: a guest account, made the first time a call without a
        /// user needs it, the way DCMS makes one for a visitor who is not signed in (GuestController):
        /// type guest, role Guest, the guests' customer group, this site. It has a guest's rights only,
        /// so nothing of an admin or a customer can be read through it, and it never signs in (its
        /// password is random and kept nowhere). Found by its user name, so every server process and
        /// every restart uses the same account.
        /// </summary>
        private async Task<long> SiteAccountAsync(long siteId)
        {
            if (_siteAccounts.TryGetValue(siteId, out var id))
                return id;
            await _findingAccount.WaitAsync();
            try
            {
                if (_siteAccounts.TryGetValue(siteId, out id))
                    return id;
                id = await RunIsolated(new FlowIdentity(siteId, 0, 0, null), sp => FindOrMakeSiteAccountAsync(sp, siteId));
                _siteAccounts[siteId] = id;
                return id;
            }
            finally
            {
                _findingAccount.Release();
            }
        }

        private async Task<long> FindOrMakeSiteAccountAsync(IServiceProvider sp, long siteId)
        {
            var name = SiteAccountUserName(siteId);
            var users = sp.GetRequiredService<IRepository<User>>();
            Task<long?> Find() => users.Query().AsNoTracking().Where(u => u.UserName == name).Select(u => (long?)u.Id).FirstOrDefaultAsync();
            if (await Find() is long existing)
                return existing;

            var now = DateTimeOffset.Now;
            var account = new User
            {
                FullName = "Server flows",
                Email = name,
                UserName = name,
                Type = EUsersTypes.GuestClient,
                CreationDate = now,
                CreatedById = 1,
                ClientIP = "127.0.0.1",
                Status = EStatus.Active,
                StatusDate = now,
            };
            account.UserSites.Add(new UsersSites { User = account, SiteId = siteId, IsDefault = true });
            account.CustomerGroups.Add(new CustomerGroupUser { CustomerGroupId = GuestCustomerGroupId, User = account });

            var accounts = sp.GetRequiredService<UserManager<User>>();
            var made = await accounts.CreateAsync(account, ServerFlowUserOperations.NewPassword(32));
            if (!made.Succeeded)
            {
                // Another server process made it at the same moment (the user name is unique).
                if (await Find() is long other)
                    return other;
                throw new InvalidOperationException(string.Join(" ", made.Errors.Select(e => e.Description)));
            }
            var role = await accounts.AddToRoleAsync(account, ERoleTypes.Guest.ToString());
            if (!role.Succeeded)
                _logger.LogWarning("Server flows: the account {UserName} ({UserId}) was made but not given the Guest role: {Errors}",
                    name, account.Id, string.Join(" ", role.Errors.Select(e => e.Description)));
            _logger.LogInformation("Server flows: made the account {UserName} ({UserId}) that calls without a user run as on site {SiteId}.", name, account.Id, siteId);
            return account.Id;
        }

        /// <summary>
        /// Runs <paramref name="work"/> on a fresh scope, on a thread pool task that does not inherit
        /// the caller's execution context: the synthetic request set here stays private to the call
        /// (IHttpContextAccessor is AsyncLocal; setting it in a shared flow would clear another
        /// call's request).
        /// </summary>
        internal Task<T> RunIsolated<T>(FlowIdentity who, Func<IServiceProvider, Task<T>> work)
        {
            // SuppressFlow throws when the flow is suppressed already (then there is nothing to inherit).
            using (ExecutionContext.IsFlowSuppressed() ? default(AsyncFlowControl?) : ExecutionContext.SuppressFlow())
            {
                return Task.Run(async () =>
                {
                    using var scope = _scopes.CreateScope();
                    var sp = scope.ServiceProvider;
                    sp.GetRequiredService<ServerExecutionContext>().EnterServerScope();

                    // WorkContext reads the request when it is created, so the request must be in
                    // place before anything resolves it.
                    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                    var request = new DefaultHttpContext { RequestServices = sp };
                    var headers = request.Request.Headers;
                    headers["siteId"] = who.SiteId.ToString(CultureInfo.InvariantCulture);
                    headers["userId"] = who.UserId.ToString(CultureInfo.InvariantCulture);
                    if (who.CultureId > 0)
                        headers["cultureId"] = who.CultureId.ToString(CultureInfo.InvariantCulture);
                    if (who.CartId.HasValue)
                        headers["CartId"] = who.CartId.Value.ToString(CultureInfo.InvariantCulture);
                    accessor.HttpContext = request;

                    try
                    {
                        return await work(sp);
                    }
                    finally
                    {
                        accessor.HttpContext = null;
                    }
                });
            }
        }

        /// <summary>The invoker's answer as JSON, with dates back as the text the API sent (as in the browser).</summary>
        private static JToken? ToToken(object? result)
        {
            JToken? token = result switch
            {
                null => JValue.CreateNull(),
                JToken t => t,
                string s => new JValue(s),
                InvokerFileResult => throw new FileResponseException("The data source answered a file; the server cannot use file responses."),
                _ => JToken.FromObject(result),
            };

            IEnumerable<JToken> all = token is JContainer container ? container.DescendantsAndSelf() : new[] { token };
            foreach (var date in all.OfType<JValue>().Where(v => v.Type == JTokenType.Date).ToList())
                date.Value = JsonConvert.SerializeObject(date.Value).Trim('"');

            return token;
        }

        private static string Trim(string message) => message.Length <= MaxErrorLength ? message : message[..MaxErrorLength] + "...";

        private sealed class FileResponseException : Exception
        {
            public FileResponseException(string message) : base(message) { }
        }
    }
}
