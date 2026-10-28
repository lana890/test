using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Shipments.Models.AramixModels.Responses.Tracking
{

    public class TrackingResponse
    {
        public string WaybillNumber { get; set; }
        public string UpdateCode { get; set; }
        public string UpdateDescription { get; set; }
        public DateTime? UpdateDateTime { get; set; }
        public string UpdateLocation { get; set; }
        public string Comments { get; set; }
        public string ProblemCode { get; set; }
        public string GrossWeight { get; set; }
        public string ChargeableWeight { get; set; }
        public string WeightUnit { get; set; }
    }

    public class TrackingResult
    {
        public string Key { get; set; }
        public List<TrackingResponse> Value { get; set; }
    }

    public class RootViewModel
    {
        public Requests.Shipping.Transaction Transaction { get; set; }
        public List<object> Notifications { get; set; }
        public bool HasErrors { get; set; }
        public List<TrackingResult> TrackingResults { get; set; }
        public List<object> NonExistingWaybills { get; set; }
    }


    public class TrackingPickupResponse
    {
        public Requests.Shipping.Transaction Transaction { get; set; }
        public List<object> Notifications { get; set; }
        public bool HasErrors { get; set; }
        public string Entity { get; set; }
        public string Reference { get; set; }
        public DateTime? CollectionDate { get; set; }
        public DateTime? PickupDate { get; set; }
        public string LastStatus { get; set; }
        public string LastStatusDescription { get; set; }
        public List<object> CollectedWaybills { get; set; }
    }

}
