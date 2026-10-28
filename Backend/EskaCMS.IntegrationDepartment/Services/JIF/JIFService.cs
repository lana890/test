using EskaCMS.Core.Cache;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Models.JIF;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Services.JIF
{
	public class JIFService : HttpServiceBase, IJIFService
	{
		private readonly JIFSettings _settings;
		private readonly ICacheService _cacheService;

		string baseURL = "";
		public JIFService(ICacheService cacheService,HttpClient client) : base(client) {
			_settings = IntegrationSettingsLoader.Load<JIFSettings>(AppContext.BaseDirectory, "JIF");
			baseURL = _settings.baseURL;
			_cacheService = cacheService;
		}

		public async Task<VehicleResponseViewModel> GetVehicle(VehicleInquiryViewModel vehicleInquiryViewModel)
		{
			string token = _cacheService.GetData<string>("Token");

			if (string.IsNullOrEmpty(token))
			{
				token= await GetToken();

				_cacheService.SetData<string>("Token", token, DateTimeOffset.Now.AddDays(double.Parse(_settings.CachePeriod)));


			}
			vehicleInquiryViewModel.USER_ID = _settings.USER_ID;
			vehicleInquiryViewModel.PASSWORD = _settings.PASSWORD;

			VehicleResponseViewModel res = await GetAsync<VehicleResponseViewModel>(
			   $"{baseURL}/JIFHUBTEST/api/Vehicle/GetVehicle", vehicleInquiryViewModel,token);



			return res;
			
		}

		public async Task<string> GetToken()
		{
			string token = _cacheService.GetData<string>("Token");

			if (!string.IsNullOrEmpty(token))
			{

				return token;
			}
			else {

				var payload = new JifLoginRequest
				{
					USER_ID = _settings.USER_ID,
					PASSWORD = _settings.PASSWORD
				};
				JifLoginResponse res = await PostAsync<JifLoginResponse>(
				   $"{baseURL}/JIFHUBTEST/api/CoUser/GetToken", payload);

				_cacheService.SetData<string>("Token", res.Token, DateTimeOffset.Now.AddDays(double.Parse(_settings.CachePeriod)));

				return res.Token;
			}



	
		}



		public async Task<PolicyResponseViewModel> GetPolicy(PolicyRequestViewModel policy)
		{
			string token = _cacheService.GetData<string>("Token");

			if (string.IsNullOrEmpty(token))
			{
				token = await GetToken();
				_cacheService.SetData<string>("Token", token, DateTimeOffset.Now.AddDays(double.Parse(_settings.CachePeriod)));

			}
			policy.USER_ID = _settings.USER_ID;
			policy.PASSWORD = _settings.PASSWORD;

			PolicyResponseViewModel res = await GetAsync<PolicyResponseViewModel>(
			   $"{baseURL}/JIFHUBTEST/api/Policy/GetPolicy", policy, token);



			return res;

		}






	}
}
