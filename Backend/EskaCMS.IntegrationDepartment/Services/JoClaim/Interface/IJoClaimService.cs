using EskaCMS.Integrations.Models.CBVehicleAssessment;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EskaCMS.Integrations.Services.JoClaim.Interface
{
    public interface IJoClaimService
    {
        public Task<string> JoclaimInteg(JoclaimIntegRequest request,long? cartId);
        public  Task<object> GetVehicleHistoryAsync(string vin, string language);
        public Task CBVehicleAssessment(long userId, string reportJson,long cartId,long siteId);
    }
}
