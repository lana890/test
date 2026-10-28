using AutoMapper;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.LoyaltyPoints;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Models.Response;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.LoyaltyPoints.Services
{
    public class LoyaltyWalletService : ILoyaltyWalletService
    {
        private readonly IRepository<LoyaltyWallet> _loyaltyWalletRepository;
        private readonly IRepository<LoyaltyPointsSetup> _loyaltyPointsSetup;
        private readonly IRepository<LoyaltyWalletTransactions> _loyaltyWalletTransactions;
        private readonly IPointsConversionService _pointsConversionService;
        private readonly IRepository<User> _userrepository;
        private readonly ICurrencyService _currencyService;
        private readonly IWorkContext _workContext;
        private readonly IMapper _mapper;
        public LoyaltyWalletService(IRepository<LoyaltyWallet> loyaltyWalletRepository,
                                     IPointsConversionService pointsConversionService,
                                     IRepository<LoyaltyWalletTransactions> loyaltyWalletTransactions,
                                        IWorkContext workContext,
                                        ICurrencyService currencyService,
                                          IMapper mapper,
                                          IRepository<User> userrepository,
                                           IRepository<LoyaltyPointsSetup> loyaltyPointsSetup)
        {
            _loyaltyWalletRepository = loyaltyWalletRepository;
            _pointsConversionService = pointsConversionService;
            _loyaltyWalletTransactions = loyaltyWalletTransactions;
            _userrepository = userrepository;
            _loyaltyPointsSetup = loyaltyPointsSetup;
            _currencyService = currencyService;
            _workContext = workContext;
            _mapper = mapper;

        }


        public async Task<bool> CreateLoyaltyWallet(long userId)
        {

            try
            {
                var siteId = _workContext.GetCurrentSiteId();


                if (_loyaltyWalletRepository.Query().Any(x => x.UserId == userId && x.SiteId == siteId))
                    throw new Exception("This User already has wallet at this site");

                var userLoyaltyWallet = new LoyaltyWallet
                {
                    UserId = userId,
                    SiteId = siteId,
                    Status = ELoyalityWalletStatus.Active,
                };

                _loyaltyWalletRepository.Add(userLoyaltyWallet);
                await _loyaltyWalletRepository.SaveChangesAsync();


                return true;

            }
            catch (Exception e)
            {

                throw e;
            }

        }

        //create loyalitywallet to bulk

        public async Task<bool> CreateLoyalityWallletToBulk(List<long> userIds, AddPointsDto points)
        {

            try
            {
           foreach(long userId in userIds)
                {
                    await DepositUserWalletByAdmin(userId, points);
                }
            } 
            catch (Exception e) 
            {
                throw;
            }
            
            //var siteId = _workContext.GetCurrentSiteId();
            //var existingUserWallet = _loyaltyWalletRepository.Query().Include(x => x.LoyaltyWalletTransactions).Where(x => userIds.Contains(x.UserId) && x.SiteId == siteId).ToHashSet();
            //// var result = new Dictionary<long, string>();
            //var wallets = new List<LoyaltyWallet>();
            //var existedwallets = new List<LoyaltyWallet>();

            //foreach (var userId in userIds)
            //{
            //    if (existingUserWallet.Any(x => x.UserId == userId))
            //    {
            //        existedwallets.Add(existedwallets.Where(x => x.UserId == userId).FirstOrDefault());
            //        continue;
            //    }
            //    else
            //    {
            //        wallets.Add(new LoyaltyWallet
            //        {
            //            UserId = userId,
            //            SiteId = siteId,
            //            Status = ELoyalityWalletStatus.Active,


            //        });

            //    }
            //}
            ////insert as loyalty wallets as bulk
            //_loyaltyWalletRepository.AddRange(wallets);
            //await _loyaltyWalletRepository.SaveChangesAsync();

            //wallets.AddRange(existedwallets);
            //List<LoyaltyWalletTransactions> loyaltywalletTransaction = null;


            ////add Loyalty points
            //foreach (var wallet in wallets)
            //{
            //    var lastTransaction = wallet.LoyaltyWalletTransactions.LastOrDefault();

            //    var loyaltyTransaction = new LoyaltyWalletTransactions
            //    {
            //        LoyaltyWalletId = wallet.Id,
            //        // LoyaltyPointsSetupId = loyaltySetup.Id,
            //        PointsAmount = pointAmount,
            //        TotalAmount = lastTransaction != null ? lastTransaction.TotalAmount + pointAmount : pointAmount,
            //        OperationType = EWalletOperationType.Gain,
            //        //  OrderId = orderId.HasValue ? orderId.Value : null,

            //    };

            //    loyaltywalletTransaction.Add(loyaltyTransaction);
            //}
            //_loyaltyWalletTransactions.AddRange(loyaltywalletTransaction);
            //await _loyaltyWalletTransactions.SaveChangesAsync();


            return true;
        
        }
        public async Task<bool> SuspendLoyaltyWallet(long userId)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var loyaltyWallet = await _loyaltyWalletRepository.Query()
                                                        .Where(x => x.SiteId == siteId && x.UserId == userId)
                                                        .FirstOrDefaultAsync();

            if (loyaltyWallet == null)
                throw new Exception("User Wallet Not Exist");


            loyaltyWallet.Status = ELoyalityWalletStatus.Suspended;


            await _loyaltyWalletRepository.SaveChangesAsync();

            return true;



        }

        public async Task<bool> ActiveLoyaltyWallet(long userId)
        {

            var siteId = _workContext.GetCurrentSiteId();

            var loyaltyWallet = await _loyaltyWalletRepository.Query()
                                                        .Where(x => x.SiteId == siteId && x.UserId == userId)
                                                        .FirstOrDefaultAsync();

            if (loyaltyWallet == null)
                throw new Exception("User Wallet Not Exist");



            loyaltyWallet.Status = ELoyalityWalletStatus.Active;


            await _loyaltyWalletRepository.SaveChangesAsync();

            return true;



        }

        public async Task<LoyaltyWalletDetails> GetLoyaltyWallet(long userId)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var checkIfGuest = await _workContext.IsGuestUser();

            if (checkIfGuest)
            {
                return new LoyaltyWalletDetails
                {
                    PointsAmount = 0,
                    TotalAmount = _currencyService.FormatByUserCurrency(0),
                    Status = ELoyalityWalletStatus.Suspended.ToString()

                };
            }

            var loyaltyWallet = await _loyaltyWalletRepository.Query().AsNoTracking()
                                                       .Where(x => x.SiteId == siteId && x.UserId == userId)
                                                       .Include(x => x.LoyaltyWalletTransactions)
                                                       .FirstOrDefaultAsync();

            if (loyaltyWallet == null)
                return new LoyaltyWalletDetails
                {
                    PointsAmount = 0,
                    TotalAmount = _currencyService.FormatByUserCurrency(0),
                    Status = ELoyalityWalletStatus.Suspended.ToString(),

                };

            var lastTransaction = loyaltyWallet.LoyaltyWalletTransactions.LastOrDefault();

            var pointsValue = (await _pointsConversionService.GetPointsConversion(siteId)).PointsValue;

            var totalValue = lastTransaction == null ? 0 : _currencyService.DcimalRound(lastTransaction.TotalAmount * pointsValue);

            string FormattedTotalValue = _currencyService.FormatByUserCurrency(totalValue);


            var loyaltyWalletDetails = new LoyaltyWalletDetails
            {
                PointsAmount = lastTransaction == null ? 0 : lastTransaction.TotalAmount,
                TotalAmount = FormattedTotalValue,
                Status = loyaltyWallet.Status.ToString()

            };

            return loyaltyWalletDetails;

        }

        public async Task<PaginationResult<LoyaltyTransaction>> GetLoyaltyTransactions(long userId, SmartTableParam param)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var checkIfGuest = await _workContext.IsGuestUser();


            var userWallet = await _loyaltyWalletRepository.Query().AsNoTracking()
                                                      .Where(x => x.SiteId == siteId && x.UserId == userId)
                                                      .FirstOrDefaultAsync();


            if (userWallet is null || checkIfGuest)
            {
                return new PaginationResult<LoyaltyTransaction>(new List<LoyaltyTransaction>(), 0, 0);
            }

            var query = _loyaltyWalletTransactions.Query()
                                                       .Where(x => x.LoyaltyWalletId == userWallet.Id && (x.OperationType != EWalletOperationType.Hold && x.OperationType != EWalletOperationType.Canceld))
                                                       .Include(x => x.LoyaltyPointsSetup)
                                                       .AsNoTrackingWithIdentityResolution();




            var pointsValue = (await _pointsConversionService.GetPointsConversion(siteId)).PointsValue;
            var UserCurrency = await _currencyService.GetUserCurrencyRateOrDefault();

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.From != null)
                {
                    DateTimeOffset From = search.From;
                    query = query.Where(x => x.CreationDate >= From);
                }


                if (search.To != null)
                {
                    DateTimeOffset To = search.To;
                    query = query.Where(x => x.CreationDate <= To);
                }


                if (search.OperationType != null)
                {
                    var OperationType = (EWalletOperationType)search.OperationType;
                    query = query.Where(x => x.OperationType == OperationType);
                }


            }

            var tranasactionList = await query.ToListAsync();


            if (tranasactionList.Any())
            {

                var result = await tranasactionList.OrderByDescending(x=>x.Id).Select(x => new LoyaltyTransaction()
                {
                    TotalAmount = x.TotalAmount,
                    OperationType = x.OperationType,
                    CreationDate = x.CreationDate,
                    OrderId = x.OrderId,
                    PointsAmount = x.PointsAmount,
                    LoyalitySource = x.LoyaltyPointsSetup != null ? x.LoyaltyPointsSetup.LoyalitySource :null,
                    TotalValue = _currencyService.FormatByUserCurrency(x.PointsAmount * pointsValue),
                    EncryptedOrderId = x.OrderId.HasValue ? EncryptDecryptHelper.Encrypt(x.OrderId.ToString()) : "N/A",
                    Note = x.Note,

                }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);

                return result;
            }
            return new PaginationResult<LoyaltyTransaction>(new List<LoyaltyTransaction>(), 0, 0);




        }


        public async Task<bool> DepositUserWallet(long userId, LoyaltyPointsSetup loyaltySetup, long? orderId, decimal? totalAmount, long? Qty)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();


                if (loyaltySetup == null)
                    return false;

                decimal pointsAmount = 0;

                if (loyaltySetup.LoyalitySource != ELoyalitySource.ConsumeRate)
                {
                    pointsAmount = loyaltySetup.PointAmount;
                    if (loyaltySetup.LoyalitySource == ELoyalitySource.SpecificProduct)
                        pointsAmount = loyaltySetup.PointAmount * (decimal)Qty;

                }
                else if (loyaltySetup.LoyalitySource == ELoyalitySource.ConsumeRate && totalAmount.HasValue)
                {

                    pointsAmount = loyaltySetup.PointPerUnit * totalAmount.Value;
                }

                var userWallet = _loyaltyWalletRepository.Query()
                                                        .Where(x => x.UserId == userId &&
                                                                    x.SiteId == siteId &&
                                                                    (x.Status == ELoyalityWalletStatus.Active || x.Status == ELoyalityWalletStatus.Suspended))
                                                        .Include(x => x.LoyaltyWalletTransactions)
                                                        .FirstOrDefault();
                // user doesn't have wallet 
                if (userWallet == null)
                {
                    await CreateLoyaltyWallet(userId);


                    userWallet = _loyaltyWalletRepository.Query()
                                                        .Where(x => x.UserId == userId &&
                                                                    x.SiteId == siteId)
                                                        .Include(x => x.LoyaltyWalletTransactions)
                                                        .FirstOrDefault();

                }

                if (userWallet.Status == ELoyalityWalletStatus.Suspended)
                    return false;

                var lastTransaction = userWallet.LoyaltyWalletTransactions.LastOrDefault();




                var loyaltyTransaction = new LoyaltyWalletTransactions
                {
                    LoyaltyWalletId = userWallet.Id,
                    LoyaltyPointsSetupId = loyaltySetup.Id,
                    PointsAmount = pointsAmount,
                    TotalAmount = lastTransaction != null ? lastTransaction.TotalAmount + pointsAmount : pointsAmount,
                    OperationType = EWalletOperationType.Gain,
                    OrderId = orderId.HasValue ? orderId.Value : null,

                };

                _loyaltyWalletTransactions.Add(loyaltyTransaction);
                await _loyaltyWalletTransactions.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }




        public async Task<bool> ProcessRegistrationLoyaltySetups(long userId, string code)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                //var time = DateTimeOffset.Now;
                var activeLoyaltySetups = _loyaltyPointsSetup.Query()
                    .Where(x => x.SiteId == siteId &&
                                x.Status == EStatus.Active &&
                                x.StartDate <= DateTimeOffset.Now &&
                                x.EndDate >= DateTimeOffset.Now &&
                                (x.LoyalitySource == ELoyalitySource.Referrer ||
                                 x.LoyalitySource == ELoyalitySource.ReferrerAndReferee ||
                                 x.LoyalitySource == ELoyalitySource.NewUser))
                    .ToList();

                if (!activeLoyaltySetups.Any())
                {
                    return true;
                }

                foreach (var loyaltySetup in activeLoyaltySetups)
                {
                    switch (loyaltySetup.LoyalitySource)
                    {
                        case ELoyalitySource.NewUser:

                            await DepositUserWallet(userId, loyaltySetup, null, null, null);

                            break;

                        case ELoyalitySource.ReferrerAndReferee:
                        case ELoyalitySource.Referrer:
                            if (!string.IsNullOrEmpty(code))
                            {
                                var referrerUser = _userrepository.Query()
                                    .Where(x => x.Code == code && x.Status == EStatus.Active)
                                    .FirstOrDefault();

                                if (referrerUser != null)
                                {
                                    await DepositUserWallet(referrerUser.Id, loyaltySetup, null, null, null);

                                    if (loyaltySetup.LoyalitySource == ELoyalitySource.ReferrerAndReferee)
                                    {
                                        await DepositUserWallet(userId, loyaltySetup, null, null, null);
                                    }

                                    var refereeUser = _userrepository.Query()
                                   .Where(x => x.Id == userId && x.Status == EStatus.Active)
                                   .FirstOrDefault();

                                    refereeUser.ReferrerId = refereeUser.Id;
                                    _userrepository.Update(refereeUser);
                                    _userrepository.SaveChanges();

                                }
                            }
                            break;
                    }
                }

                return true;
            }catch(Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> ProcessPurchesLoyaltySetups(Order order)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var activeLoyaltySetups = _loyaltyPointsSetup.Query()
                .Where(x => x.SiteId == siteId &&
                            x.Status == EStatus.Active &&
                            x.StartDate <= DateTimeOffset.Now &&
                            x.EndDate >= DateTimeOffset.Now &&
                            (x.LoyalitySource == ELoyalitySource.ConsumeRate ||
                             x.LoyalitySource == ELoyalitySource.SpecificProduct ||
                             x.LoyalitySource == ELoyalitySource.ConsumeRange))
                .ToList();

            if (!activeLoyaltySetups.Any())
            {
                return true;
            }

            foreach (var loyaltySetup in activeLoyaltySetups)
            {
                switch (loyaltySetup.LoyalitySource)
                {
                    case ELoyalitySource.SpecificProduct:
                        var targetItem = order.OrderItems.FirstOrDefault(x => x.ProductId == loyaltySetup.ProductId);
                        if (targetItem != null)
                        {
                            await DepositUserWallet(order.CustomerId, loyaltySetup, order.Id, null, targetItem.Quantity);

                        }

                        break;

                    case ELoyalitySource.ConsumeRange:
                        if (order.OrderTotal >= loyaltySetup.FromPrice && order.OrderTotal <= loyaltySetup.ToPrice)
                            await DepositUserWallet(order.CustomerId, loyaltySetup, order.Id, null, null);
                        break;
                    case ELoyalitySource.ConsumeRate:

                        await DepositUserWallet(order.CustomerId, loyaltySetup, order.Id, order.OrderTotal == 0 ? (order.SubTotalWithDiscount + order.ShippingFeeAmount) : order.OrderTotal, null);
                        break;
                }
            }

            return true;
        }


        public async Task<RedeemPointsValidationDto> RedeemPoints(long userId, long siteId, long? orderId, long? cartId, decimal totalAmount)
        {
            try
            {


                var userWallet = _loyaltyWalletRepository.Query()
                                                       .Where(x => x.UserId == userId &&
                                                                   x.SiteId == siteId &&
                                                                   x.Status == ELoyalityWalletStatus.Active)
                                                       .Include(x => x.LoyaltyWalletTransactions)
                                                       .FirstOrDefault();





                if (userWallet == null || userWallet.Status == ELoyalityWalletStatus.Suspended)
                {
                    return new RedeemPointsValidationDto
                    {
                        ErrorMessage = "User wallet doesn't exist or suspended",
                        Succeeded = false,
                        RedeemDiscount = 0,
                        TotalAmmountAfterDiscount = totalAmount,
                    };
                }



                var lastTransaction = userWallet.LoyaltyWalletTransactions.LastOrDefault();



                var pointsValue = (await _pointsConversionService.GetPointsConversion(siteId)).PointsValue;

                //var totalInWallet = lastTransaction == null ? 0 : _currencyService.DcimalRound(lastTransaction.TotalAmount * pointsValue);
                var totalInWallet = lastTransaction == null ? 0 : lastTransaction.TotalAmount * pointsValue;

                var defaultDigit = Math.Pow(10, Convert.ToInt64(_currencyService.GetDefaultDecimalPlaces(siteId)));

                var totalRedeem = totalInWallet - ((((decimal)defaultDigit * totalInWallet) % 1) / (decimal)defaultDigit);

                //var remindPoints = ((((decimal)defaultDigit * totalInWallet) % 1) / (decimal)defaultDigit) / pointsValue;

                if (totalInWallet == 0)
                {
                    return new RedeemPointsValidationDto
                    {
                        ErrorMessage = "User does't have enough balance",
                        Succeeded = false,
                        RedeemDiscount = 0,
                        TotalAmmountAfterDiscount = totalAmount,
                    };
                }


                decimal totalAfterDiscount = 0;
                decimal transactionAmount = 0;


                if (totalRedeem >= totalAmount)
                {
                    totalAfterDiscount = 0;
                    transactionAmount = totalAmount / pointsValue;
                }
                else
                {
                    totalAfterDiscount = totalAmount - totalRedeem;
                    transactionAmount = totalRedeem / pointsValue;
                }

                decimal redeemDiscount = transactionAmount * pointsValue;


                var loyaltyTransaction = new LoyaltyWalletTransactions
                {
                    LoyaltyWalletId = userWallet.Id,
                    LoyaltyPointsSetupId = null,
                    PointsAmount = transactionAmount,
                    TotalAmount = lastTransaction.TotalAmount - transactionAmount,
                    OperationType = EWalletOperationType.Hold,
                    OrderId = orderId.HasValue ? orderId.Value : null,
                    CartId = cartId.HasValue ? cartId.Value : null,

                };

                _loyaltyWalletTransactions.Add(loyaltyTransaction);
                await _loyaltyWalletTransactions.SaveChangesAsync();

                return new RedeemPointsValidationDto
                {

                    Succeeded = true,
                    RedeemDiscount = redeemDiscount,
                    TotalAmmountAfterDiscount = totalAfterDiscount,
                };
            }
            catch (Exception e)
            {

                throw e;
            }

        }


        public async Task<RedeemPointsValidationDto> RefundPoints(long userId, decimal RedeemAmount, long siteId, long? orderId, long? cartId)
        {
            try
            {


                var userWallet = _loyaltyWalletRepository.Query()
                                                       .Where(x => x.UserId == userId &&
                                                                   x.SiteId == siteId &&
                                                                   x.Status == ELoyalityWalletStatus.Active)
                                                       .Include(x => x.LoyaltyWalletTransactions)
                                                       .FirstOrDefault();





                if (userWallet == null || userWallet.Status == ELoyalityWalletStatus.Suspended)
                {
                    return new RedeemPointsValidationDto
                    {
                        ErrorMessage = "User wallet doesn't exist or suspended",
                        Succeeded = false,
                        RedeemDiscount = 0,
                        TotalAmmountAfterDiscount = 0,
                    };
                }


                var holdTransactions = userWallet.LoyaltyWalletTransactions.Where(x => x.OperationType == EWalletOperationType.Hold && x.CartId == cartId).ToList();


                //var targetTransaction = userWallet.LoyaltyWalletTransactions.Where(x=> (orderId.HasValue ? x.OrderId == orderId : true) && (cartId.HasValue ? x.CartId == cartId : true) && x.OperationType == EWalletOperationType.Hold).FirstOrDefault();
                var lastTransaction = userWallet.LoyaltyWalletTransactions.LastOrDefault();
                var pointsValue = (await _pointsConversionService.GetPointsConversion(siteId)).PointsValue;

                var totalPoints = RedeemAmount / pointsValue;


                var loyaltyTransaction = new LoyaltyWalletTransactions
                {
                    LoyaltyWalletId = userWallet.Id,
                    LoyaltyPointsSetupId = null,
                    PointsAmount = totalPoints,
                    TotalAmount = lastTransaction.TotalAmount + totalPoints,
                    OperationType = EWalletOperationType.Refund,
                    OrderId = orderId.HasValue ? orderId.Value : null,
                    CartId = cartId.HasValue ? cartId.Value : null,

                };

                if (holdTransactions.Any())
                {
                    foreach (var transaction in holdTransactions)
                        transaction.OperationType = EWalletOperationType.Canceld;
                }

                _loyaltyWalletTransactions.Add(loyaltyTransaction);
                await _loyaltyWalletTransactions.SaveChangesAsync();

                return new RedeemPointsValidationDto
                {

                    Succeeded = true,
                    RedeemDiscount = 0,
                    TotalAmmountAfterDiscount = 0,
                };
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        public async Task<RedeemPointsValidationDto> AddRedeemToOrder(long userId, long siteId, long orderId, long cartId)
        {
            try
            {


                var userWallet = _loyaltyWalletRepository.Query()
                                                       .Where(x => x.UserId == userId &&
                                                                   x.SiteId == siteId &&
                                                                   x.Status == ELoyalityWalletStatus.Active)
                                                       .Include(x => x.LoyaltyWalletTransactions)
                                                       .FirstOrDefault();





                if (userWallet == null || userWallet.Status == ELoyalityWalletStatus.Suspended)
                {
                    return new RedeemPointsValidationDto
                    {
                        ErrorMessage = "User wallet doesn't exist or suspended",
                        Succeeded = false,
                        RedeemDiscount = 0,
                        TotalAmmountAfterDiscount = 0,
                    };
                }





                var targetTransaction = userWallet.LoyaltyWalletTransactions.Where(x => x.CartId == cartId && x.OperationType == EWalletOperationType.Hold).FirstOrDefault();



                targetTransaction.OrderId = orderId;
                targetTransaction.OperationType = EWalletOperationType.Redeem;

                _loyaltyWalletTransactions.Update(targetTransaction);
                await _loyaltyWalletTransactions.SaveChangesAsync();

                return new RedeemPointsValidationDto
                {

                    Succeeded = true,
                    RedeemDiscount = 0,
                    TotalAmmountAfterDiscount = 0,
                };
            }
            catch (Exception e)
            {

                throw e;
            }

        }


        public async Task<bool> RefundGainedPoints(long userId, long siteId, long orderId)
        {
            try
            {


                var userWallet = _loyaltyWalletRepository.Query()
                                                       .Where(x => x.UserId == userId &&
                                                                   x.SiteId == siteId &&
                                                                   x.Status == ELoyalityWalletStatus.Active)
                                                       .Include(x => x.LoyaltyWalletTransactions)
                                                       .FirstOrDefault();





                if (userWallet == null || userWallet.Status == ELoyalityWalletStatus.Suspended)
                {
                    return false;
                }





                var targetTransaction = userWallet.LoyaltyWalletTransactions.Where(x => x.OrderId == orderId && x.OperationType == EWalletOperationType.Gain)
                                                                            .FirstOrDefault();

                if (targetTransaction is null)
                    return false;

                var lastLoyaltyWalletTransaction = userWallet.LoyaltyWalletTransactions.LastOrDefault();

                if (targetTransaction.PointsAmount > lastLoyaltyWalletTransaction.TotalAmount)
                {

                    throw new Exception($"The user gained {targetTransaction.PointsAmount} points from this order but currently has only {lastLoyaltyWalletTransaction.PointsAmount} points.");
                }





                var loyaltyTransaction = new LoyaltyWalletTransactions
                {
                    LoyaltyWalletId = userWallet.Id,
                    LoyaltyPointsSetupId = null,
                    PointsAmount = targetTransaction.PointsAmount,
                    TotalAmount = lastLoyaltyWalletTransaction.TotalAmount - targetTransaction.PointsAmount,
                    OperationType = EWalletOperationType.ReversedGain,
                    OrderId = orderId,
                    CartId = targetTransaction.CartId,

                };


                _loyaltyWalletTransactions.Add(loyaltyTransaction);
                await _loyaltyWalletTransactions.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        public async Task<bool> DepositUserWalletByAdmin(long userId, AddPointsDto model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var currentUserId = await _workContext.GetCurrentUserId();


                if(model.PointsAmount <= 0)
                {
                    throw new Exception("Kindly add a valid value for points amount");
                }


                var userWallet = _loyaltyWalletRepository.Query()
                                                        .Where(x => x.UserId == userId &&
                                                                    x.SiteId == siteId &&
                                                                    (x.Status == ELoyalityWalletStatus.Active || x.Status == ELoyalityWalletStatus.Suspended))
                                                        .Include(x => x.LoyaltyWalletTransactions)
                                                        .FirstOrDefault();
                // user doesn't have wallet 
                if (userWallet == null)
                {
                    await CreateLoyaltyWallet(userId);


                    userWallet = _loyaltyWalletRepository.Query()
                                                        .Where(x => x.UserId == userId &&
                                                                    x.SiteId == siteId)
                                                        .Include(x => x.LoyaltyWalletTransactions)
                                                        .FirstOrDefault();

                }

                if (userWallet.Status == ELoyalityWalletStatus.Suspended)
                    return false;

                var lastTransaction = userWallet.LoyaltyWalletTransactions.LastOrDefault();




                var loyaltyTransaction = new LoyaltyWalletTransactions
                {
                    LoyaltyWalletId = userWallet.Id,
                    PointsAmount = model.PointsAmount,
                    TotalAmount = lastTransaction != null ? lastTransaction.TotalAmount + model.PointsAmount : model.PointsAmount,
                    OperationType = EWalletOperationType.GainByAdmin,
                    CreationDate = DateTimeOffset.Now,
                    CreatedById = currentUserId,
                    Note = model.Note,


                };

                _loyaltyWalletTransactions.Add(loyaltyTransaction);
                await _loyaltyWalletTransactions.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }






    }
}
