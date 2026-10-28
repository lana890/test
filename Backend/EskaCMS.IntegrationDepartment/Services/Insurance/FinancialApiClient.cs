using EskaCMS.Integrations.Services.Interfaces;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models;
using EskaCMS.Integrations.Models.Insurance;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EskaCMS.Integrations.Services;

namespace EskaCMS.Integrations.Services.InsuranceInterfaces
{
    public class FinancialApiClient : HttpServiceBase, IFinancialApiClient
    {
        private readonly IntegrationSettings _settings;

        public FinancialApiClient(HttpClient client) : base(client)
        {
            _settings = IntegrationSettingsLoader.Load<IntegrationSettings>(AppContext.BaseDirectory, "Integration");

            // Replicate exactly the same headers the original single HttpClient had.
            // The original IntegrationService set these once on its injected HttpClient
            // and ALL calls — including customer API calls — went out with these headers.
            // Removing them broke the customer API which expects Bearer + Environment + SessionId.
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.LongLifeToken);
            Client.DefaultRequestHeaders.Remove("Environment");
            Client.DefaultRequestHeaders.Add("Environment", "onpremise");
            Client.DefaultRequestHeaders.Remove("SessionId");
            Client.DefaultRequestHeaders.Add("SessionId", "");
        }


        // ─── RECEIPT ─────────────────────────────────────────────────────────────

        public async Task GenerateReceipt(
            string policyId, long? transactionId, string netPremium, string date)
        {
            var r = _settings.Receipt;
            var url = $"{_settings.FinancialBaseUrl}/GenerateReceipt"
                + $"?BankAccountID={r.BankAccountId}"
                + $"&BankAmount={netPremium}"
                + $"&CommissionAccountID={r.CommissionAccountId}"
                + $"&CommissionAmount={r.CommissionAmount}"
                + $"&ReceiptDate={Uri.EscapeDataString(date)}"
                + $"&PolicyID={policyId}"
                + $"&TransactionIDs={transactionId}"
                + $"&ApplicationID={r.ApplicationID}"
                + $"&CostCenterId={r.CostCenterId}"
                + $"&Note={r.Note}"
                + $"&ApprovalNo={r.ApprovalNo}"
                + $"&CreditCardNo={r.CreditCardNo}"
                + $"&ReferenceNo={r.ReferenceNo}"
                + $"&CreditCardCode={r.CreditCardCode}";

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var response = await Client.GetAsync(url, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception(
                    $"GenerateReceipt failed [{response.StatusCode}] policy={policyId}: {body}");
            }
        }


        // ─── CUSTOMER API CALLS ───────────────────────────────────────────────────
        // All three use FinancialBaseUrl2 which expects CoreToken only — no Bearer.
        // Token is set per HttpRequestMessage so concurrent calls never race.

        public async Task<long> GetCustomerByNationalId(string nationalId)
        {
            var payload = new
            {
                CompanyId = _settings.CompanyId,
                NationalId = nationalId,
                PageNumber = "1",
                RowsSizePerPage = "1000"
            };

            var form = new MultipartFormDataContent
            {
                {
                    new StringContent(
                        JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
                    "SearchCriteria"
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_settings.FinancialBaseUrl2}/Customers/GetCustomers");
            request.Headers.Add("CoreToken", _settings.LongLifeToken);
            request.Content = form;

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var response = await Client.SendAsync(request, cts.Token);
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<CustomerResponse>(json);
            return data?.Data?.FirstOrDefault()?.ID ?? 0;
        }

        public async Task<long> InsertCustomer(InsertCustomerRequest payload)
        {
            var form = new MultipartFormDataContent
            {
                {
                    new StringContent(
                        JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
                    "CustomerObjectValues"
                },
                { new StringContent("[1]", Encoding.UTF8, "application/json"), "CustomerTypesDataObj" }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_settings.FinancialBaseUrl2}/Customers/InsertCustomer");
            request.Headers.Add("CoreToken", _settings.LongLifeToken);
            request.Content = form;

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var response = await Client.SendAsync(request, cts.Token);
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<CustomerResponse>(json);
            return data?.Data?.FirstOrDefault()?.ID ?? 0;
        }

        public async Task UpdateCustomer(UpdateCustomerRequest payload)
        {
            using var form = new MultipartFormDataContent
    {
        {
            new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"),
            "CustomerObjectValues"
        },
        {
            new StringContent(
                "[1]",
                Encoding.UTF8,
                "application/json"),
            "CustomerTypesDataObj"
        }
    };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_settings.FinancialBaseUrl2}/Customers/UpdateCustomer");

            request.Headers.Add("CoreToken", _settings.LongLifeToken);
            request.Content = form;

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

            try
            {
                using var response = await Client.SendAsync(request, cts.Token);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"UpdateCustomer failed. " +
                        $"Status: {(int)response.StatusCode} {response.ReasonPhrase}. " +
                        $"Response: {responseContent}");
                }

            }
            catch (TaskCanceledException ex) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "UpdateCustomer request timed out after one minute.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    $"Unable to call UpdateCustomer API. URL: " +
                    $"{_settings.FinancialBaseUrl2}/Customers/UpdateCustomer",
                    ex);
            }
        }
    }
}