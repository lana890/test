using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class RestrictedWordsServices : IRestrictedWordsServices
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<RestrictedWords> _restrictedWordsRepository;
        public RestrictedWordsServices(IWorkContext workContext, IRepository<RestrictedWords> restrictedWordsRepository)
        {
            _workContext = workContext;
            _restrictedWordsRepository = restrictedWordsRepository;
        }
        public async Task<long> UpsertRestrictedPasswords(RestrictedWordVM model)
        {
            try
            {
                bool IsEditMode = model.Id != 0;

                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var UserId = await _workContext.GetCurrentUserId();

                var restrictedword = new RestrictedWords();
                if (IsEditMode)
                {
                    restrictedword = await _restrictedWordsRepository.Query()
                                                                           .Where(r => r.Id == model.Id)
                                                                           .FirstOrDefaultAsync();
                    if (restrictedword == null)
                    {
                        throw new Exception("Restricted Word Not Found");
                    }

                    restrictedword.ModifiedById = UserId;
                    restrictedword.ModificationDate = DateTimeOffset.Now;
                }
                else
                {
                    restrictedword.CreatedById = UserId;
                    restrictedword.CreationDate = DateTimeOffset.Now;
                    restrictedword.SiteId = SiteId;
                    restrictedword.Status = EStatus.Published;

                }

                restrictedword.RestrictedWord = model.RestrictedText;

                if (!IsEditMode)
                {
                    _restrictedWordsRepository.Add(restrictedword);
                }

                await _restrictedWordsRepository.SaveChangesAsync();
                return restrictedword.Id;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> UpdateRestrictedWordStatus(long Id, EStatus NewStatus)
        {
            try
            {
                var restrictedword = await _restrictedWordsRepository.Query()
                                              .Where(r => r.Id == Id)
                                              .FirstOrDefaultAsync();
                if (restrictedword == null)
                {
                    throw new Exception("Restricted Password Not Found");
                }

                restrictedword.Status = NewStatus;
                await _restrictedWordsRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<bool> IsRestrictedWord(string content,long? SiteId=null)
        {

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }
           // var SiteId = await _workContext.GetCurrentSiteIdAsync();
            var restrictedWords = await _restrictedWordsRepository.Query()
                                .Where(r => r.SiteId == SiteId &&
                                            r.Status == EStatus.Published)
                                .Where(x=>x.SiteId==SiteId)
                                .Select(x=>x.NormalizedRestrictedWord)
                                .ToListAsync();

            var upperContent = content.ToUpper();



            var containsRestrictedWord = restrictedWords.Any(word => upperContent.Contains(word));

            return containsRestrictedWord;

        }

        public async Task<PaginationResult<AllRestrictedWordVM>> AllRestrictedWords(SmartSearchTableParam<GetAllRestrictedWordFilter> model)
        {

   

            var siteId = _workContext.GetCurrentSiteId();

          

            var allrestrictedWords = _restrictedWordsRepository.Query()
                                          .Where(x => x.SiteId==siteId  &&
                                                 x.Status != EStatus.Deleted)
                                          .AsNoTracking();






            if (!string.IsNullOrEmpty(model.Search.RestrictedText))
            {
                string RestrictedText = model.Search.RestrictedText;
                allrestrictedWords = allrestrictedWords.Where(x => x.NormalizedRestrictedWord.Contains(RestrictedText.ToUpper()));
            }


            var result = await allrestrictedWords.OrderByDescending(x=>x.CreationDate).Select(x => new AllRestrictedWordVM
            {
                Id = x.Id,
                CreationDate = x.CreationDate,
                CreatedBy=x.CreatedBy.UserName,
                RestrictedText=x.RestrictedWord,
                Status=x.Status,
            }).PaginateAsync(model.Pagination.Number, model.Pagination.Start);



            return result;
        }
    }
}
