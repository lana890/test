using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.Integrations.Services
{
    ///
    /// Each concrete subclass (GeneralApiClient, MedicalApiClient, FinancialApiClient)
    /// is registered as a singleton via AddHttpClient<T>() in ModuleInitializer.
    ///
    /// Thread-safety guarantee:
    ///   - HttpClient itself is thread-safe for concurrent calls.
    ///   - PostAsync / PostSafeAsync create only local state (CTS, StringContent) —
    ///     no shared mutable fields are touched per-call.
    ///   - DefaultRequestHeaders are written ONCE in each subclass constructor,
    ///     never mutated at call-time, so there is no race condition.
    /// </summary>
    public abstract class HttpServiceBase
    {
        protected readonly HttpClient Client;

        // Soft per-call ceiling — sits just under the 60 s HttpClient.Timeout hard wall.
        // PostAsync enforces this internally; WithStepTimeout in IntegrationService adds
        // a second layer at the orchestration level (belt-and-suspenders).
        protected static readonly TimeSpan DefaultCallTimeout = TimeSpan.FromMinutes(2);

        protected HttpServiceBase(HttpClient client)
        {
            Client = client;
        }

        // ─── THROWING VARIANTS ────────────────────────────────────────────────────
        // Use when a failure MUST stop the current flow.

        /// <summary>POST, deserialize, and return. Throws on HTTP error or timeout.</summary>
        public async Task<T> PostAsync<T>(
            string url,
            object payload,
            CancellationToken cancellationToken = default, 
            TimeSpan? timeout = null)
        {
            using var cts = BuildLinkedCts(cancellationToken, timeout ?? DefaultCallTimeout);

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(url, content, cts.Token);

            // Read the body ONCE — used for both error reporting and deserialization.
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
				//return default;
				 throw new Exception($"[POST {url}] HTTP {(int)response.StatusCode}: {body}");

            return JsonConvert.DeserializeObject<T>(body);
        }


		public async Task<T> GetAsync<T>(string url, object payload, string? token)
		{
			var json = JsonConvert.SerializeObject(payload);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Content = content;

			if (!string.IsNullOrEmpty(token))
			{
				request.Headers.Add("Authorization", token);
			}

			var response = await Client.SendAsync(request);

			var body = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
				throw new Exception($"[GET {url}] HTTP {(int)response.StatusCode}: {body}");

			return JsonConvert.DeserializeObject<T>(body)!;
		}




		/// <summary>POST with no response body needed. Throws on HTTP error or timeout.</summary>
		public async Task PostAsync(
            string url,
            object payload,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null)
        {
            using var cts = BuildLinkedCts(cancellationToken, timeout ?? DefaultCallTimeout);

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(url, content, cts.Token);

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"[POST {url}] HTTP {(int)response.StatusCode}: {body}");
        }

        // ─── SAFE (NON-THROWING) VARIANTS ─────────────────────────────────────────
        // Use for non-critical steps where the flow must continue regardless.

        /// <summary>POST and deserialize. Returns default(T) on any failure — never throws.</summary>
        public async Task<T> PostSafeAsync<T>(
            string url,
            object payload,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null)
        {
            try { return await PostAsync<T>(url, payload, cancellationToken, timeout); }
            catch { return default; }
        }

        /// <summary>POST with no response body. Returns false on any failure — never throws.</summary>
        public async Task<bool> PostSafeAsync(
            string url,
            object payload,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null)
        {
            try { await PostAsync(url, payload, cancellationToken, timeout); return true; }
            catch { return false; }
        }

        // ─── HELPER ───────────────────────────────────────────────────────────────

        private static CancellationTokenSource BuildLinkedCts(
            CancellationToken callerToken, TimeSpan timeout)
        {
            if (callerToken == CancellationToken.None || callerToken == default)
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(timeout);
                return cts;
            }

            var linked = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
            linked.CancelAfter(timeout);
            return linked;
        }
    }
}