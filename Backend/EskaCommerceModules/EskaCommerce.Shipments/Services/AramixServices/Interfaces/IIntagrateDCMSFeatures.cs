
using EskaCommerce.Shipments.Models.AramixModels.Requests.RateCaluculator;
using EskaCommerce.Shipments.Models.AramixModels.Requests.Settings;
using EskaCommerce.Shipments.Models.AramixModels.Requests.Shipping;
using EskaCommerce.Shipments.Models.AramixModels.Requests.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Services.Interfaces
{
    public interface IIntagrateDCMSFeatures
    {

        Task<ShipmentCreationRequest> CreateShipments(long OrderId, SettingsAramxViewModel settings);
        Task<RateCalculatorRequestDtos> CreateCalculateRate(long CartId);
        Task<TrackingShippmentDto> CreateShipmentTracking(long id);
        Task<PickupTrackingDto> CreateTrackingPickup(long orderId);
        Task<PickupCreationRequest> CreatePickup(RequestPickupAdmin request,List<long> OrderIds, SettingsAramxViewModel model);
    }
}
