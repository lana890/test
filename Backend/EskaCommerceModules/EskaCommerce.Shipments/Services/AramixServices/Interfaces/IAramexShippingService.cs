using EskaCMS.Entities.Entity.Shipments;
using EskaCommerce.Shipments.Models.AramixModels.Requests.Shipping;
using EskaCommerce.Shipments.Models.AramixModels.Responses.RateCalculate;
using EskaCommerce.Shipments.Models.AramixModels.Responses.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Services.Interfaces
{
    public interface IAramexShippingService
    {
        #region Shippment
        Task<bool> CreateShipmentAsync(long OrderId,long?ShipmentId=null);
        Task<bool> RepushShipmentAsync(long orderId);

        Task<bool> CreatePickup(RequestPickupAdmin  model);
        //Task<dynamic> CancelPickupAsync(PickupCancelationRequestDto request);
        //Task<dynamic> PrintLabelAsync(LabelPrintingRequestDto request);

        #endregion

        #region Tracking
        Task<RootViewModel> TrackShipments(long ShipmentId);
        Task<TrackingPickupResponse> TrackPickup(long PickupId);
        Task<bool> ChangeShipmentStatus(List<long> orderIds, ShippmentStatus shippmentStatus);
        #endregion
        #region
        Task<RateCalculatorResponse> CalculateRate(long CartId);
        Task<bool> AddCitiesByAramex();
        Task<bool> CancelPickup(long pickupId);
        Task<string> PrintLabel(long ShipmentId);
        #endregion
    }
}
