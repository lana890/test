using System.Collections.Concurrent;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// One call to a webhook flow's address, as the Admin shows it under "Recent calls": when, from
    /// which address (and whether through Cloudflare), what it asked (start a run, or read one), what
    /// the server answered and why. Nothing the caller sent is kept: no key, signature or body.
    /// </summary>
    /// <param name="Outcome">accepted, signature, time, key, keyRetired, address, input, tooMany.</param>
    /// <param name="Proof">How the call proved it was the other system's: "signature: {key name}" or "key"; null when it did not.</param>
    public sealed record WebhookCallRecord(DateTimeOffset At, string? Address, bool ViaCloudflare, string Action, string Outcome, int Status,
        long? RunId, string? Proof, string? Detail);

    /// <summary>
    /// The last calls of each webhook flow (only flows that exist, so strangers cannot fill it), in
    /// this process's memory: what an admin needs while another system starts calling (which address
    /// the server sees, whether the signature or the clock is wrong). Cleared when the server restarts.
    /// </summary>
    public sealed class ServerFlowWebhookCalls
    {
        public const int Keep = 30;

        private readonly ConcurrentDictionary<string, LinkedList<WebhookCallRecord>> _calls = new(StringComparer.Ordinal);

        private static string KeyOf(long siteId, string flowName) => $"{siteId}:{flowName}";

        public void Record(long siteId, string flowName, WebhookCallRecord call)
        {
            var list = _calls.GetOrAdd(KeyOf(siteId, flowName), _ => new LinkedList<WebhookCallRecord>());
            lock (list)
            {
                list.AddFirst(call);
                while (list.Count > Keep)
                    list.RemoveLast();
            }
        }

        /// <summary>The flow's last calls, newest first.</summary>
        public IReadOnlyList<WebhookCallRecord> For(long siteId, string flowName)
        {
            if (!_calls.TryGetValue(KeyOf(siteId, (flowName ?? "").Trim()), out var list))
                return Array.Empty<WebhookCallRecord>();
            lock (list)
                return list.ToList();
        }
    }
}
