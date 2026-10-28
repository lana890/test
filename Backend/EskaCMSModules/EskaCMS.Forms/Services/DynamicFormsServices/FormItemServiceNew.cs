using AutoMapper;
using cloudscribe.Pagination.Models;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.DynamicForms.Paginations;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.UnitOfWorks.Interfaces;
using EskaCMS.DynamicForms.ViewModels;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections;

using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Services;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.Interfaces;
using Org.BouncyCastle.Asn1.Ocsp;

namespace EskaCMS.DynamicForms.Services
{
    public class FormItemServiceNew : IFormItemServiceNew
    {

        private readonly IRepository<FormItem> formItemRepository;
        private readonly IRepository<Classifications> classificationRepository;
        private readonly IRepository<FormField> formFieldRepository;
        private readonly IWorkContext workContext;
        private readonly IUserSiteService userSiteService;
        private readonly ISiteCultureService _siteCultureService;

        public FormItemServiceNew(
                                  IRepository<FormItem> FormItemRepository,
                                  IRepository<Classifications> ClassificationRepository,
                                  IRepository<FormField> FormFieldRepository,
                                  IWorkContext workContext,
                                  ISiteCultureService siteCultureService,
                                  IUserSiteService userSiteService)
        {
            formItemRepository = FormItemRepository;
            classificationRepository = ClassificationRepository;
            formFieldRepository = FormFieldRepository;
            this.workContext = workContext;
            this.userSiteService = userSiteService;
            _siteCultureService = siteCultureService;
        }



        public async Task<PaginationResult<FormItemSummaryVM>> GetAllFormItemsWithClassificationPagination(long siteId, string? SearchText, int? page, int? pageSize)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();
                var userRoles = await userSiteService.GetUserRoles(siteId, userId);
                var FormItem = formItemRepository.Query().Include(x => x.Thumbnail)
                                                              .Include(x => x.FormItemRoles)
                                                              .Include(x => x.FormItemDetails)
                                                              .Include(x => x.Classifications)
                                                              .ThenInclude(x => x.CriteriaList)
                                                              .Include(x => x.FormFields)
                                                              .ThenInclude(x => x.FormFieldDetails)
                                                              .ThenInclude(x => x.Category.Children)
                                                              .ThenInclude(x => x.CategoryDetails)
                                                              .Include("FormFields.FormFieldDetails.Category.Children.CategoryDetails")
                                                              .Where(p => p.SiteId == siteId
                                                                       && p.Status == EStatus.Published)
                                                              .Where(p =>
                                                                       (string.IsNullOrEmpty(SearchText) ||
                                                                       p.FormItemDetails.Any(x => x.Name.ToUpper().Contains(SearchText.ToUpper()))))
                                                              .Where(x => x.FormItemRoles.Any() ? x.FormItemRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
;

                var Result = await FormItem.Select(x => new FormItemSummaryVM
                {

                    SiteId = x.SiteId,
                    Status = x.Status,
                    ThumbnailUrl = x.ThumbnailId.HasValue ? x.Thumbnail.PublicUrl : null,
                    ThumbnailId = x.ThumbnailId,
                    FormFieldNum = x.FormFields.Where(x => x.Status == EStatus.Published).Count(),
                    ClassificationsNum = x.Classifications.Where(x => x.Status == EStatus.Published).Count(),
                    FormItemDetails = x.FormItemDetails.Select(x => new FormItemsCultureViewModel
                    {

                        CultureId = x.CultureId,
                        Name = x.Name

                    }).ToList(),
                    Description = x.FormItemDetails.Select(x => x.Description).FirstOrDefault(),
                    Icon = x.Icon,
                    FormFieldsItems = x.FormFields.Where(x => x.Status == EStatus.Published).Select(x => new FormFieldMainInfoVM
                    {
                        FormFieldId = x.Id,
                        Id = x.Id,
                        Type = x.InputType,
                        Name = x.FormFieldDetails.SelectMany(x => x.Category.CategoryDetails).Select(c => c.Name).FirstOrDefault(),
                        OneOfOptionsType = x.OneOfOptionsType,
                        FormFieldDetails = x.FormFieldDetails.Select(x => new FormFieldDetailsMainInfoVM
                        {
                            CategoryId = x.CategoryId,
                            FormFieldId = x.Id,
                            Category = new CategoryVM
                            {
                                Id = x.CategoryId,
                                CategoryDetails = new List<ChildDetailsMainInfoVM> { x.Category.CategoryDetails.Select(childDetails => new ChildDetailsMainInfoVM
                                    {
                                        Id = childDetails.Id,
                                        Name = childDetails.Name,
                                    }).FirstOrDefault()},
                                Children = x.Category.Children.Select(child => new CategoryVM()
                                {
                                    CategoryDetails = new List<ChildDetailsMainInfoVM> { child.CategoryDetails.Select(childDetails => new ChildDetailsMainInfoVM
                                    {
                                        Id = childDetails.Id,
                                        Name = childDetails.Name,
                                    }).FirstOrDefault()},
                                    Id = child.Id
                                }).ToList()

                            },
                            DataSourceId = x.DataSourceId,
                            StaticApiLink = x.StaticApiLink,
                            AdditionalInfo = x.AdditionalInfo


                        }).ToList()

                    }).ToList(),
                    CreationDate = x.CreationDate,
                    Id = x.Id,
                    ClassificationsItems = x.Classifications.Where(x => x.Status == EStatus.Published).Select(x => new FormItemsClassificationVM
                    {

                        CriteriaCount = x.CriteriaList.Count(),
                        Id = x.Id,
                        Name = x.Name
                    }).ToList()
                }).OrderByDescending(x => x.Id).PaginateAsync(pageSize ?? 100, page ?? 0);

                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<FormItemsClassificationVM>> GetClassificationsByFormItemIdPagintaion(long FormItemId, long siteId, string? SearchText, int? page, int? pageSize)
        {
            try
            {
                var Result = await classificationRepository.Query().Include(x => x.CriteriaList)
                                                            .Where(p => p.FormItemId == FormItemId && p.Status != EStatus.Deleted && p.SiteId == siteId)
                                                            .Where(p => string.IsNullOrEmpty(SearchText) || p.Name.ToUpper().Contains(SearchText.ToUpper()))
                                                            .Select(x => new FormItemsClassificationVM
                                                            {
                                                                Id = x.Id,
                                                                Name = x.Name,
                                                                CriteriaCount = x.CriteriaList.Count()
                                                            }).OrderByDescending(x => x.Id).PaginateAsync(pageSize ?? 100, page ?? 0);
                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<FormFieldMainInfoVM>> GetFormFieldByFormItemIdPagintaion(long cultureId, long matrixId, long siteId, int? page, int? pageSize)
        {
            try
            {
                var FormFields = await formFieldRepository.Query().Where(x => x.Status == EStatus.Published && x.SiteId == siteId)
                                                                .Include(x => x.FormFieldDetails)
                                                                .ThenInclude(x => x.Category)
                                                                .ThenInclude(x => x.CategoryDetails)
                                                                .Select(x => new FormFieldMainInfoVM
                                                                {
                                                                    FormFieldId = x.Id,
                                                                    Id = x.Id,
                                                                    Name = x.FormFieldDetails.SelectMany(x => x.Category.CategoryDetails).Where(x => x.CultureId == cultureId).Select(c => c.Name).FirstOrDefault(),
                                                                    FormFieldDetails = x.FormFieldDetails.Where(x => x.CultureId == cultureId).Select(x => new FormFieldDetailsMainInfoVM
                                                                    {
                                                                        CategoryId = x.CategoryId,
                                                                        FormFieldId = x.Id,
                                                                        Category = new CategoryVM
                                                                        {
                                                                            Id = x.CategoryId,
                                                                            CategoryDetails = x.Category.CategoryDetails.Where(t => t.CultureId == cultureId).Select(x => new ChildDetailsMainInfoVM
                                                                            {
                                                                                Id = x.Id,
                                                                                Name = x.Name,
                                                                            }).ToList()

                                                                        }

                                                                    }).ToList()

                                                                }).OrderByDescending(x => x.Id).PaginateAsync(pageSize ?? 100, page ?? 0);

                return FormFields;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
