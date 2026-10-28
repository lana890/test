using EskaCMS.Addresses.Models;
using EskaCMS.ADIntegration.Interfaces;
using EskaCMS.ADIntegration.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.Services
{

    public class ADService : IADService
    {
        private readonly IRepository<Address> _addressRepository;
        public readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        public ADService(IRepository<Address> addressRepository, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _addressRepository = addressRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }
        public AddressVM GetAddressById(long id)
        {
            AddressVM addressVM = _addressRepository.Query().Where(x => x.Id == id).Select(a => new AddressVM
            {
                AddressLine1 = a.AddressLine1,
                ContactName = a.ContactName,
                Id = a.Id,
                City = a.City,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            }).FirstOrDefault();
            return addressVM;
        }
        public async Task<GetADUserViewModel> ActiveDirectoryLogin()
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                string AdUrl= _configuration.GetValue<string>("ActiveDirectory:ActiveDirectoryUrl");
                client.BaseAddress = new Uri(AdUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //GET Method  
              

                HttpResponseMessage response = await client.GetAsync("api/Identity/GetCurrentUser");
                string responseContent = response.Content.ReadAsStringAsync().Result;
                GetADUserViewModel returnResponse = JsonSerializer.Deserialize<GetADUserViewModel>(responseContent);
                return returnResponse;

            }

        }

        private async Task<GetADUserViewModel> ADAuthenticate()
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                string AdUrl = _configuration.GetValue<string>("ActiveDirectory:ActiveDirectoryUrl");

                client.BaseAddress = new Uri(AdUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //GET Method  
                HttpResponseMessage response = await client.GetAsync("api/Identity/GetCurrentUser");
                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    GetADUserViewModel returnResponse = JsonSerializer.Deserialize<GetADUserViewModel>(responseContent);

                    return returnResponse;
                }
                else
                {
                    return new GetADUserViewModel();
                }
            }
        }
    }
}
