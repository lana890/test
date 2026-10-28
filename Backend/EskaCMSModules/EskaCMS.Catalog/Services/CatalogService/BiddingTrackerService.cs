using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Linq;
using Microsoft.EntityFrameworkCore;

using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Infrastructure.Data;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;

namespace EskaCommerce.Catalog.Services.CatalogService
{
    public class BiddingTrackerService : IBiddingTrackerService
    {
        private readonly IRepository<BiddingTracker> _biddingTrackerRepository;

        public BiddingTrackerService(IRepository<BiddingTracker> biddingTrackerRepository)
        {
            _biddingTrackerRepository = biddingTrackerRepository;
        }

        public IList<BiddingTrackerVM> GetAll()
        {
            var allBiddingTracker = _biddingTrackerRepository.Query().Select(p => new BiddingTrackerVM
            {
                Id = p.Id,
                BiddingAmount = p.BiddingAmount,
                BuyerId = p.BuyerId,
                CreatedById = p.CreatedById,
                SellerId = p.SellerId,
                AuctionId = p.AuctionId,
                CreationDate = p.CreationDate
            }).ToList();

            return allBiddingTracker;
        }

        public async Task<IList<BiddingTrackerVM>> GetAllAsync()
        {
            var allBiddingTracker = await _biddingTrackerRepository.Query().Select(p => new BiddingTrackerVM
            {
                Id = p.Id,
                BiddingAmount = p.BiddingAmount,
                BuyerId = p.BuyerId,
                CreatedById = p.CreatedById,
                SellerId = p.SellerId,
                AuctionId = p.AuctionId,
                CreationDate = p.CreationDate
            }).ToListAsync();
            return allBiddingTracker;
        }

        public async Task<BiddingTrackerVM> GetByIdAsync(long Id)
        {
            var biddingTrackerInfo = await _biddingTrackerRepository.Query()
                .Where(f => f.CreatedById == Id)
                .Select(p => new BiddingTrackerVM
                {
                    Id = p.Id,
                    BiddingAmount = p.BiddingAmount,
                    BuyerId = p.BuyerId,
                    CreatedById = p.CreatedById,
                    SellerId = p.SellerId,
                    AuctionId = p.SellerId,
                    CreationDate = p.CreationDate
                })
                .FirstOrDefaultAsync();

            return biddingTrackerInfo;
        }
    }
}
