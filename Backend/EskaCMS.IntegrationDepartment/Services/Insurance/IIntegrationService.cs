using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Models.Insurance;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Services.Interfaces
{
  
		public interface IIntegrationServices
		{
		Task CreatePolicyAsync(long orderId);
		Task<RepushResultVM> RepushPolicyAsync(long orderId);
		Task CalculateQoutation( long userId,long CartId,List<IdFiles>files);

	}
	
}
