using EskaCMS.Integrations.Models.JIF;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Services.JIF
{
	public interface IJIFService
	{
		Task<string> GetToken();
		Task<VehicleResponseViewModel> GetVehicle(VehicleInquiryViewModel vehicleInquiryViewModel);
		Task<PolicyResponseViewModel> GetPolicy(PolicyRequestViewModel vehicleInquiryViewModel);

	}
}