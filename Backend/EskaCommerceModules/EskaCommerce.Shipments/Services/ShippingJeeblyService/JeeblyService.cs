using EskaCMS.Core.Data;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.ShippingJeebly.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Shipments.Services.ShippingJeeblyService.Interfaces;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;

namespace EskaCommerce.ShippingJeebly.Services
{
    public class JeeblyService : IJeeblyService
    {
        private readonly IRepository<ProductAttributeValue> _productAttributeRepository;
        private IRepository<Shipment> _shipmentRepository;
        private IRepository<ShipmentItem> _shipmentitemRepository;
        private IRepository<Warehouse> _warehouseRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly ISiteSettingsService _settingservice;
        private readonly IHttpClientFactory _httpClientFactory;
        public JeeblyService(IRepository<ProductAttributeValue> productAttributeRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentItem> shipmentitemRepository,
            IRepository<Warehouse> warehouseRepository,
            EskaDCMSDbContext context,
            ISiteSettingsService settingservice,
            IHttpClientFactory httpClientFactory
            )
        {
            _productAttributeRepository = productAttributeRepository;
            _shipmentRepository = shipmentRepository;
            _shipmentitemRepository = shipmentitemRepository;
            _warehouseRepository = warehouseRepository;
            _context = context;
            _settingservice = settingservice;
            _httpClientFactory = httpClientFactory;
        }

        public async Task CreateShipmentOrder(Order request)
        {
            try
            {

                List<long> lstOrderItemsId = new List<long>();
                foreach (var pies in request.OrderItems)
                {

                    var ItemPlan = JsonConvert.DeserializeObject<AdditionCartInfoVM>(pies.AdditionalDetails);
                    if (ItemPlan.CartType == CartTypes.Shop)
                    {

                        lstOrderItemsId.Add(pies.Id); // Get Only ShopItems

                        //var WeightStr = _productAttributeRepository.Query().Where(x => x.AttributeId == 6 && x.ProductId == pies.ProductId).Select(x => x.Value).FirstOrDefault();
                        //string Weight = Regex.Replace(WeightStr, "[^0-9.]", "");
                        //TotalWeight += Convert.ToDecimal(Weight) * pies.Quantity;
                        //Quantity += pies.Quantity;


                        //PiescesDetail objpiescesDetail = new PiescesDetail();
                        //objpiescesDetail.description = pies.Product.Name;// "ShaveobjpiescesDetail";
                        //objpiescesDetail.declared_value = "100";
                        //objpiescesDetail.width = ".5";
                        //objpiescesDetail.height = "150";
                        //objpiescesDetail.weight = "100";
                        //objConsigment.pieces_detail.Add(objpiescesDetail);
                    }
                }


                if (lstOrderItemsId.Count > 0)
                {
                    //   var cards = _context.Cards.Where(c => ids.Contains(c.ID));

                    request.OrderItems = request.OrderItems.Where(x => lstOrderItemsId.Contains(x.Id)).ToList();

                    var settings = await _settingservice.GetSiteSettingById("JeeblySettings", request.SiteId);
                    if (settings == null)
                        throw new Exception("No settings for such provider");

                    var ObjSettings = JsonConvert.DeserializeObject<JeeblySettingsVM>(settings.Value);

                    Consignments objConsigment = new Consignments();
                    objConsigment.customer_code = ObjSettings.customer_code;// "HCS";

                    objConsigment.service_type_id = ObjSettings.service_type_id;// "PREMIUM";
                    objConsigment.load_type = ObjSettings.load_type;// "NON-DOCUMENT";
                    objConsigment.customer_reference_number = request.CustomerId.ToString(); //OrderId.ToString();
                  //  objConsigment.description = request.OrderItems[0].Product.Description;// "Shaving";
                    objConsigment.is_risk_surcharge_applicable = ObjSettings.is_risk_surcharge_applicable;// true;



                    var ApiURl = ObjSettings.ApiURl;// "http://demodashboardapi.shipsy.in/api/customer/integration/consignment/softdata";
                    var ApiKey = ObjSettings.ApiKey;// "47ef0ed0b30b600e4335371ee1370f";

                    JeeblyVM objOrder = new JeeblyVM();


                    //objConsigment.cod_favor_of = "Ram Manohar";
                    // objConsigment.cod_collection_mode = "Cheque";
                    // objConsigment.dimension_unit = "";
                    /// objConsigment.length = "";
                    //objConsigment.width = "";
                    //objConsigment.height = "";
                    // objConsigment.weight_unit = "kg";
                    //objConsigment.weight = "0.25";
                    // objConsigment.commodity_id = "2";
                     objConsigment.declared_value = request.SubTotal.ToString();
                    //  objConsigment.cod_amount = "100";
                    objConsigment.num_pieces = request.OrderItems.Count().ToString();
                    // objConsigment.customer_reference_number = "XF878723";

                    var warehouse = _warehouseRepository.Query().Where(x => x.Id == 1).Include(x => x.Address).ThenInclude(x => x.StateOrProvince).FirstOrDefault(); // HomeCutswarehouse
                    objConsigment.origin_details.name = warehouse.Name;// "HomeCutsASA";
                    objConsigment.origin_details.phone = warehouse.Address.Phone;// "00962788877722";
                    objConsigment.origin_details.address_line_1 = warehouse.Address.AddressLine1;// "Street test";
                    objConsigment.origin_details.address_line_2 = warehouse.Address.AddressLine2; //"Street test";
                    objConsigment.origin_details.pincode = warehouse.Address.ZipCode;// "122002";
                    objConsigment.origin_details.city = warehouse.Address.City;// "1234546";
              //      objConsigment.origin_details.state = warehouse.Address.StateOrProvince.Name;// "1234546";
                                                                                                // objConsigment.origin_details.alternate_phone = "1234546";


                    objConsigment.destination_details.name = request.ShippingAddress.ContactName;// "ShipsyDSD";
                    objConsigment.destination_details.phone = request.ShippingAddress.Phone;// "078956543266";
                    objConsigment.destination_details.address_line_1 = request.ShippingAddress.AddressLine1;// "TestAdrerss";
                    objConsigment.destination_details.pincode = request.ShippingAddress.ZipCode; //"122002";
                    objConsigment.destination_details.city = request.ShippingAddress.City;// "Amman";
                    objConsigment.destination_details.alternate_phone = "";
         //         objConsigment.destination_details.state = request.ShippingAddress.StateOrProvince.Name;//  "state";





                    objOrder.consignments.Add(objConsigment);
                    string result = string.Empty;

                    string Json = JsonConvert.SerializeObject(objOrder);
                    HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");
                    using (var client = _httpClientFactory.CreateClient())
                    {
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("api-key", ApiKey);
                        HttpResponseMessage response = await client.PostAsync(ApiURl, Obj);
                        result = await response.Content.ReadAsStringAsync();
                        if (response.IsSuccessStatusCode)
                        {
                            var Resobj = JsonConvert.DeserializeObject<JeeblyCreateOrderReponseVM>(result);
                            if (Resobj.status == "OK")
                            {
                                foreach (var data in Resobj.data)
                                {


                                    if (data.success)
                                    {
                                      

                                        var shipment = new Shipment
                                        {
                                            OrderId = request.Id,
                                            TrackingNumber = data.reference_number,
                                            CreatedById = request.CustomerId,
                                            WarehouseId = warehouse.Id,


                                        };

                                        _shipmentRepository.Add(shipment);
                                        await _shipmentRepository.SaveChangesAsync();

                                        foreach (var item in request.OrderItems)
                                        {
                                            var shipmentitem = new ShipmentItem
                                            {
                                                OrderItemId = item.Id,
                                                ProductId = item.ProductId,
                                                Quantity = item.Quantity,
                                                ShipmentId = shipment.Id
                                                

                                            };

                                            _shipmentitemRepository.Add(shipmentitem);
                                            await _shipmentitemRepository.SaveChangesAsync();
                                        
                                        }


                                    }
                                }


                            }
                        }

                    }
                }

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
    }
}
