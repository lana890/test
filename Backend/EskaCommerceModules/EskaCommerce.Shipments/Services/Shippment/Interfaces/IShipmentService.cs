using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Infrastructure;
using EskaCommerce.Infrastructure;
using EskaCommerce.Shipments.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Shipments.Areas.Shipments.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Mvc;

namespace EskaCommerce.Shipments.Services.Shippment.Interfaces
{
    public interface IShipmentService
    {
        Task<IList<ShipmentItemVm>> GetShipmentItem(long orderId);

        Task<IList<ShipmentItemVm>> GetItemToShip(long orderId, long warehouseId);

        Task<Result> CreateShipment(Shipment shipment);
        Task<SmartTableResult<PickupsAdminVewModel>> GetPickups( SmartTableParam param);

        Task<bool> ChangeShipmentStatus(long ShipmentId, ShippmentStatus shippmentStatus);
        Task<List<ShipmentHistoryViewModel>> GetShipmentHistory(long id);
        Task<DeliveryStatusViewModel> GetShipmentById(long id);
    }
}
