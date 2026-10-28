using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using IronBarCode;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using EskaCMS.Entities.Entity;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;
using EskaCMS.Entities.Entity.Orders;
using OfficeOpenXml.Drawing.Chart.ChartEx;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.ViewModels;
namespace EskaCMS.Core.Services
{
    public class QrCodeService : IQrCodeService
    {
        public IRepository<OrderItem> _OrderItemRepository;
        private readonly IWorkContext _workContext;

        public QrCodeService(IRepository<OrderItem> OrderItemRepository, IWorkContext workContext)
        {
            _OrderItemRepository = OrderItemRepository;
            _workContext = workContext;
        }

        public async Task<string> GenerateQrCode(string Url, long orderId)
        {
            try
            {
                QRCodeGenerator QrGenerator = new QRCodeGenerator();
                QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(Url, QRCodeGenerator.ECCLevel.Q);
                QRCoder.QRCode QrCode = new QRCoder.QRCode(QrCodeInfo);
                Bitmap QrBitmap = QrCode.GetGraphic(6, Color.Black, Color.White, true);


                long SiteId = _workContext.GetCurrentSiteId();
             
                //byte[] bytes = await BitmapToBytes(QrBitmap);

                //var Base = Convert.ToBase64String(bytes);

               

                using (var memoryStream = new MemoryStream())
                {
                    QrBitmap.Save(memoryStream, ImageFormat.Png);

                    var file = new FormFile(memoryStream, 0, memoryStream.Length, "image", $"QrCode_{orderId}.png");

                    var FullPath = await SaveInFolder(file, SiteId);

                    await MappingToInvoice(orderId, FullPath);

                    return FullPath;

                }

            }
            catch (Exception ex)
            {

                throw ex;
            }



        }
        public async Task<string> GenerateQrCode(string Data,string FileName)
        {
            try
            {
                QRCodeGenerator QrGenerator = new QRCodeGenerator();
                QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(Data, QRCodeGenerator.ECCLevel.Q);
                QRCoder.QRCode QrCode = new QRCoder.QRCode(QrCodeInfo);
                Bitmap QrBitmap = QrCode.GetGraphic(6, Color.Black, Color.White, true);


                using (var memoryStream = new MemoryStream())
                {
                    QrBitmap.Save(memoryStream, ImageFormat.Png);

                    // var file = new FormFile(memoryStream, 0, memoryStream.Length, "image", $"{FileName}.png");
                    return Convert.ToBase64String(memoryStream.ToArray());
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }



        }

     
        private async Task<bool> MappingToInvoice(long orderId,string path)
        {
            try
            {
                var OrderItem = await _OrderItemRepository.Query().Include(x => x.Order).Where(x => x.Order.Id == orderId).FirstOrDefaultAsync();

                if (OrderItem == null)
                {

                    return false;
                }
                OrderItem.QrCode =path; 

               _OrderItemRepository.Update(OrderItem);
               await _OrderItemRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }     
        
        }

    
        private void CreateIfMissing(string path)
        {
            bool folderExists = Directory.Exists(path);
            if (!folderExists)
                Directory.CreateDirectory(path);
        }

        private async Task<string> SaveInFolder(FormFile  img, long SiteId)
        {
            try
            {
                string Folder = "wwwroot/Resources/QrCodes/siteId_" + SiteId+"/";

                CreateIfMissing(Folder);
                string FileName = Folder + "/" +img.FileName;

                if (File.Exists(FileName))
                {
                    File.Delete(FileName);


                }


                using (var stream = new FileStream(Path.Combine(Folder, img.FileName), FileMode.Create))
                {

                    await img.CopyToAsync(stream);
                }

                return Path.Combine(Folder, img.FileName);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


    }

}

