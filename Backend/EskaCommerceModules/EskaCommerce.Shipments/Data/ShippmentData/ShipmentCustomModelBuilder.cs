

using EskaCMS.Infrastructure.Data;
using EskaCommerce.Shipments.Models;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Shipments;

namespace EskaCommerce.Shipments.Data.ShippmentData
{
    public class ShipmentCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Shipment>(s =>
            //{
            //    s.HasOne(x => x.CreatedBy)
            //   .WithMany()
            //   .HasForeignKey(x => x.CreatedById)
            //   .OnDelete(DeleteBehavior.Restrict);
            //});

            //modelBuilder.Entity<Shipment>(s =>
            //{
            //    s.HasOne(x => x.Order)
            //   .WithMany()
            //   .HasForeignKey(x => x.OrderId)
            //   .OnDelete(DeleteBehavior.Restrict);
            //});

            //modelBuilder.Entity<Shipment>(s =>
            //{
            //    s.HasOne(x => x.Warehouse)
            //   .WithMany()
            //   .HasForeignKey(x => x.WarehouseId)
            //   .OnDelete(DeleteBehavior.Restrict);
            //});
        }
    }
}
