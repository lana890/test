using AutoMapper;
using EskaCMS.BlogArchive.Models;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models;
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.ViewModels;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Pages.Models;
using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Interfaces;
using EskaCMS.Sites.Services.Model;
using EskaCMS.Theming.Services.Interfaces;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Services
{
    public class SiteSlugServices : ISiteSlugServices

    {
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly string _connectionString;
        private readonly EskaCMS.Infrastructure.Enums.EProvider _provider;

        private readonly IUserDashboardService _UserDashboardService;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _Configration;
        public SiteSlugServices(

           IWorkContext workContext,
           EskaDCMSDbContext context,
           IConfiguration configuration,
           IUserDashboardService UserDashboardService,
           ICurrencyService currencyService,
           ICacheService cacheService,
           IConfiguration Configration)
        {

            _workContext = workContext;
            _currencyService = currencyService;
            _UserDashboardService = UserDashboardService;
            _cacheService = cacheService;
            _Configration = Configration;
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _provider = DbProviderHelper.ParseProvider(
                configuration.GetSection("ConnectionStrings:DatabaseProvider").Value);

        }



        public async Task<SlugInfoSettings> GetSlugSettingsForWebSp(UserNavigationVm userNavigationVm)
        {
            long siteId = _workContext.GetCurrentSiteId();
            long cultureId = _workContext.GetCurrentCultureId();

            var userId = await _workContext.GetCurrentUserId();
            var roleId = await GetUserRoles(userId);

            if (string.IsNullOrEmpty(userNavigationVm.Slug) || userNavigationVm.Slug == "0")
            {
                userNavigationVm.Slug = string.Empty;
            }

            string cacheKey = $"Slug_{userNavigationVm.Slug}_{cultureId}_{siteId}_{roleId}";
            var cacheData = _cacheService?.GetData<SlugInfoSettings>(cacheKey);

            if (cacheData != null)
            {
                if (cacheData.ProductSettings != null)
                {
                    await _UserDashboardService.ExecuteTakeNavigationSnapShot(
                        userNavigationVm,
                        cacheData.SlugType,
                        cacheData.ProductSettings.Id);
                }

                return cacheData;
            }

            var slugData = await spGetSlugSettings(
                userNavigationVm.Slug,
                cultureId,
                siteId,
                (long)EStatus.Published,
                roleId);

            if (slugData != null && slugData.ProductSettings != null)
            {
                await _UserDashboardService.ExecuteTakeNavigationSnapShot(
                    userNavigationVm,
                    slugData.SlugType,
                    slugData.ProductSettings.Id);
            }

            _cacheService.SetData(
                cacheKey,
                slugData,
                DateTimeOffset.Now.AddDays(
                    double.Parse(_Configration.GetSection("CacheConfig:CachePeriod").Value)));

            return slugData;
        }
        private async Task<SlugInfoSettings> spGetSlugSettings(
       string slug, long cultureId, long siteId, long status, long roleId)
        {
            try
            {


                SlugInfoSettings slugResult = new SlugInfoSettings();

                await using var connection = dbFactory.CreateConnection(_connectionString, _provider);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                DbProviderHelper.ConfigureCommand(command);
                command.CommandTimeout = 30;

                AddInputParameter(command, "@P_SLUG", slug, DbType.String);
                AddInputParameter(command, "@P_CULTUREID", cultureId, DbType.Int64);
                AddInputParameter(command, "@P_SITEID", siteId, DbType.Int64);
                AddInputParameter(command, "@P_STATUS", status, DbType.Int64);
                AddInputParameter(command, "@P_POSTSLUGTYPE", (long)SlugsTypes.Blog, DbType.Int64);
                AddInputParameter(command, "@P_PAGESLUGTYPE", (long)SlugsTypes.Page, DbType.Int64);
                AddInputParameter(command, "@P_PRODUCTSLUGTYPE", (long)SlugsTypes.Product, DbType.Int64);
                AddInputParameter(command, "@P_BRANDSLUGTYPE", (long)SlugsTypes.Brand, DbType.Int64);
                AddInputParameter(command, "@P_CATEGORYSLUGTYPE", (long)SlugsTypes.Category, DbType.Int64);
                AddInputParameter(command, "@P_ROLEID", roleId, DbType.Int64);
                ConfigureProcedureCommand(
                    command,
                    "GET_SLUG_SETTINGS",
                    "P_SLUG",
                    "P_CULTUREID",
                    "P_SITEID",
                    "P_STATUS",
                    "P_POSTSLUGTYPE",
                    "P_PAGESLUGTYPE",
                    "P_PRODUCTSLUGTYPE",
                    "P_BRANDSLUGTYPE",
                    "P_CATEGORYSLUGTYPE",
                    "P_ROLEID");

                await using var result = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                if (!result.HasRows)
                    return null;

                bool isFirstRound = true;

                while (await result.ReadAsync())
                {
                    var slugType = (SlugsTypes)ParseAnd0IfNull(result["SlugType"]?.ToString());

                    if (isFirstRound)
                    {
                        slugResult = slugType switch
                        {
                            SlugsTypes.Page => await fillPageDataFromSP(result),
                            SlugsTypes.Blog => await fillPostDataFromSP(result),
                            SlugsTypes.Product => await fillProductDataFromSP(result),
                            SlugsTypes.Category => await fillCategoryDataFromSP(result),
                            SlugsTypes.Brand => await fillBrandDataFromSP(result),
                            _ => slugResult
                        };

                        isFirstRound = false;
                    }

                    if (slugType == SlugsTypes.Blog)
                        slugResult =await fillPostOneToManyRelationData(result, slugResult);
                    else if (slugType == SlugsTypes.Category)
                        slugResult =await fillCategoryOneToManyRelationData(result, slugResult);
                }

                return slugResult;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> CanAccessSlug(long TargetId, SlugsTypes SlugType)
        {
            long UserId = await _workContext.GetCurrentUserId();
            long SiteId = await _workContext.GetCurrentSiteIdAsync();

            await using var connection = dbFactory.CreateConnection(_connectionString, _provider);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            DbProviderHelper.ConfigureCommand(command);

            AddInputParameter(command, "@TargetId", TargetId, DbType.Int64);
            AddInputParameter(command, "@SLUG_ENTITY", (int)SlugType, DbType.Int32);
            AddInputParameter(command, "@P_USERID", UserId, DbType.Int64);
            AddInputParameter(command, "@SiteId", SiteId, DbType.Int64);
            ConfigureProcedureCommand(
                command,
                "Can_Access_Slug",
                "TargetId",
                "SLUG_ENTITY",
                "P_USERID",
                "SiteId");

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.CloseConnection); // <-- closes connection when reader closes

            if (await reader.ReadAsync())
                return reader["Exist"].ToString() == "1";

            return false;
        }
        private async Task<SlugInfoSettings> fillProductDataFromSP(DbDataReader result)
        {
            try
            {
                var data = new SlugInfoSettings();

                ProductViewModel model = new ProductViewModel();
                model.Id = ParseAnd0IfNull(result["Id"].ToString());
                model.Name = result["Name"].ToString();
                model.Slug = result["Slug"].ToString();
                model.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());
                model.Description = result["Description"].ToString();
                model.ShortDescription = result["ShortDescription"].ToString();
                model.AdditonalMetaTags = result["ProductAdditionalMetatags"].ToString();

                model.TemplateId =ParseAnd0IfNull(result["TemplateId"].ToString());
                //   model.ParentId =ParseAnd0IfNull( result["ParentId"].ToString());
                model.CultureId =ParseAnd0IfNull(result["CultureId"].ToString());

                //  model. = result["MetaKeywords"].ToString();
                model.ThumbnailImageUrl = result["ThumbnailImageUrl"].ToString();
                model.ImageWidth = ReadOptionalInt(result, "ImageWidth");      // NEW
                model.ImageHeight = ReadOptionalInt(result, "ImageHeight");    // NEW
                model.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());
                data.SlugType = SlugsTypes.Product;
                model.SlugType = SlugsTypes.Product;
                data.ProductSettings = model;
                return data;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private bool ParseAnd0IfNulltoboolean(string v)
        {
            return ParseDbBoolean(v);
        }

        private async Task<SlugInfoSettings> fillCategoryDataFromSP(DbDataReader result)
        {
            try
            {
                var data = new SlugInfoSettings();
                CategoryViewModel model = new CategoryViewModel();
                model.Id = ParseAnd0IfNull(result["CategoryId"].ToString());
                model.Name = result["CategoryName"].ToString();

                model.Slug = result["Slug"].ToString();

                model.DisplayOrder =Convert.ToInt32(result["DisplayOrder"].ToString());

                model.IncludeInMenu = ParseAnd0IfNulltoboolean(result["IncludedInMenu"].ToString());

                model.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());

                model.Description = result["Description"].ToString();
                model.MetaDescription = result["MetaDescription"].ToString();
                model.MetaTitle = result["MetaTitle"].ToString();
                model.MetaKeywords = result["MetaKeywords"].ToString();
                model.ParentId = ParseAnd0IfNull(result["ParentId"].ToString());
                model.Status =(EStatus)Convert.ToInt32(result["Status"].ToString());
                model.ThumnailImageId =string.IsNullOrEmpty(result["ThumbnailImageId"].ToString()) ? 0 : Convert.ToInt64(result["ThumbnailImageId"].ToString());
                model.PublicUrl = result["Imageurl"].ToString();
                model.ImageWidth = ReadOptionalInt(result, "ImageWidth");      // NEW
                model.ImageHeight = ReadOptionalInt(result, "ImageHeight");    // NEW
                model.FileName = result["FileName"].ToString();
                data.SlugType = SlugsTypes.Category;
                model.SlugType = SlugsTypes.Category;
                data.CategoriesSettings = model;
                return data;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<SlugInfoSettings> fillBrandDataFromSP(DbDataReader result)
        {
            try
            {

                var data = new SlugInfoSettings();
                BrandViewModel model = new BrandViewModel();
                model.Id = ParseAnd0IfNull(result["BrandId"].ToString());
                model.Name = result["BrandName"].ToString();
                model.Description = result["Description"].ToString();
                model.Slug = result["Slug"].ToString();
                // model.ParentId = ParseAnd0IfNull(result["ParentId"].ToString());
                model.Status =(EStatus)Convert.ToInt32(result["Status"].ToString());
                model.ThumbnailId =string.IsNullOrEmpty(result["ThumbnailImageId"].ToString()) ? null : Convert.ToInt64(result["ThumbnailImageId"].ToString());
                model.PublicUrl = result["PublicUrl"].ToString();
                model.ImageWidth = ReadOptionalInt(result, "ImageWidth");      // NEW
                model.ImageHeight = ReadOptionalInt(result, "ImageHeight");    // NEW
                model.CultureId = ParseAnd0IfNull(result["CultureId"].ToString());
                data.SlugType = SlugsTypes.Brand;
                model.SlugType = SlugsTypes.Brand;
                data.BrandSettings = model;
                return data;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        private async Task<SlugInfoSettings> fillPostDataFromSP(DbDataReader result)
        {

            SlugInfoSettings slugInfo = new SlugInfoSettings();

            PostAndPageViewModel data = new PostAndPageViewModel();

            data.Id = ParseAnd0IfNull(result["PostId"].ToString());

            data.EncryptedUserId =EncryptDecryptHelper.Encrypt(result["CreatedById"].ToString() ??"");

            data.Keywords = result["PostKeywords"].ToString();

            data.Title = result["PostTitle"].ToString();

            data.Image = result["PostImage"].ToString();
            data.ImageWidth = ReadOptionalInt(result, "ImageWidth");      // NEW
            data.ImageHeight = ReadOptionalInt(result, "ImageHeight");    // NEW

            data.Slug = result["PostSlug"].ToString();

            data.IsLoginRequired = ParseDbBoolean(result["IsLoginRequired"]);

            var PublishDate = result["PublishDate"].ToString();

            if (!string.IsNullOrEmpty(PublishDate))
            {
                data.PublishDate = Convert.ToDateTime(PublishDate);
            }

            data.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());

            var TemplateId = result["PostTemplateId"].ToString();

            data.PostContent = result["PostContent"].ToString();

            data.Description = result["PostDescription"].ToString();

            data.LoginSlug = result["LoginSlug"].ToString();

            data.ShortDescription = result["PostShortDescription"].ToString();

            data.MetaDescription = result["PostMetaDescription"].ToString();

            data.PostSettings = result["PostSettings"].ToString();

            data.RelatedCategoriesSettings = result["PostRelatedCategoriesSettings"].ToString();

            slugInfo.SlugType = SlugsTypes.Blog;
            data.SlugType = SlugsTypes.Blog;


            data.TemplateId = string.IsNullOrEmpty(TemplateId) ? 0 : long.Parse(TemplateId);

            slugInfo.PostSettings = data;
            return slugInfo;
        }

        private async Task<SlugInfoSettings> fillPostOneToManyRelationData(DbDataReader result, SlugInfoSettings SlugResult)
        {
            // Category List 
            var CatId = ParseAnd0IfNull(result["PostCategoryId"].ToString());
            if (CatId != 0)
            {
                TempListsObj tempListsObj = new TempListsObj()
                {
                    id = CatId,
                    label = result["PostCategoryName"].ToString()
                };

                if (SlugResult.PostSettings.CategoryList.FirstOrDefault(c => c.id == tempListsObj.id) == null)
                {
                    SlugResult.PostSettings.CategoryList.Add(tempListsObj);
                }
            }




            //// PostsTags List 
            //var TagId = ParseAnd0IfNull(result["PostTagId"].ToString());
            //if (TagId != 0)
            //{
            //    var tempListsObj = new TempListsObj()
            //    {
            //        id = TagId,
            //        label = result["PostTagTitle"].ToString()
            //    };

            //    if (SlugResult.PostSettings.TagList.FirstOrDefault(c => c.id == tempListsObj.id) == null)
            //    {
            //        SlugResult.PostSettings.TagList.Add(tempListsObj);
            //    }
            //}



            // MediaURLs List 
            var mediaId = ParseAnd0IfNull(result["PostMediaId"].ToString());
            if (mediaId != 0)
            {
                PostMediaWithURLViewModel PostMediaWithURLObj = new PostMediaWithURLViewModel()
                {
                    Id = mediaId,
                    Url = result["PostPublicUrl"].ToString(),
                    ContentType = result["MediaContentType"].ToString()

                };

                if (SlugResult.PostSettings.MediaURLs.FirstOrDefault(c => c.Id == PostMediaWithURLObj.Id) == null)
                {
                    SlugResult.PostSettings.MediaURLs.Add(PostMediaWithURLObj);
                }
            }




            //// Imported Files List 
            //var ImportedFileId = ParseAnd0IfNull(result["ImportedID"].ToString());
            //if (SlugResult.Template != null 
            //    && SlugResult.Template.CurrentVersion != null
            //    && ImportedFileId !=0)
            //{

            //    ImportedFilesVM importedFilesVM = new ImportedFilesVM()
            //    {
            //        Id = ParseAnd0IfNull(result["ImportedID"].ToString()),
            //        FileId = ParseAnd0IfNull(result["MediaId"].ToString()),
            //        SourceId = ParseAnd0IfNull(result["SourceId"].ToString()),
            //        FileName = result["FileName"].ToString(),
            //        FileUrl = result["FileUrl"].ToString()
            //    };

            //    if (importedFilesVM.Id != 0 && SlugResult.Template.CurrentVersion.ImportFilesList.FirstOrDefault(c => c.Id == importedFilesVM.Id) == null)
            //    {
            //        SlugResult.Template.CurrentVersion.ImportFilesList.Add(importedFilesVM);
            //    }
            //}


            return SlugResult;
        }


        private async Task<SlugInfoSettings> fillPageDataFromSP(DbDataReader result)
        {
            var Result = new SlugInfoSettings();
            PostAndPageViewModel data = new PostAndPageViewModel();

            data.Id = ParseAnd0IfNull(result["Id"].ToString());
            data.AdditionalContent = result["AdditionalContent"].ToString();
            data.Description = result["Description"].ToString();
            data.IsLoginRequired = ParseDbBoolean(result["IsLoginRequired"]);
            data.Keywords = result["Keywords"].ToString();
            data.Name = result["Name"].ToString();
            data.Slug = result["Slug"].ToString();
            data.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());
            Result.SlugType = SlugsTypes.Page;
            data.SlugType = SlugsTypes.Page;
            data.LoginSlug = result["LoginSlug"].ToString();

            data.Image = result["ThumbnailUrl"].ToString();
            data.ImageWidth = ReadOptionalInt(result, "ImageWidth");      // NEW
            data.ImageHeight = ReadOptionalInt(result, "ImageHeight");    // NEW

            var TemplateId = result["PageTemplateId"].ToString();

            data.TemplateId = string.IsNullOrEmpty(TemplateId) ? null : Convert.ToInt64(TemplateId);




            //data.Template = string.IsNullOrEmpty(TemplateId)
            //? null
            //: new PageTemplateVM()
            //{
            //    Description = result["TemplateDescription"].ToString(),
            //    CurrentVersion = new TemplateVersionVM()
            //    {
            //        Css = result["TemplateCss"].ToString(),
            //        Scripts = result["TemplateScripts"].ToString(),
            //        SettingsBody = result["TemplateSettingsBody"].ToString(),
            //        TemplateComponents = string.IsNullOrEmpty(result["TemplateComponents"].ToString())
            //        ? JsonConvert.SerializeObject(new List<string>())
            //        : result["TemplateComponents"].ToString()
            //    }

            //};


            var PageCurrentVersionId = result["PageCurrentVersionId"].ToString();
            data.CurrentVersion = string.IsNullOrEmpty(PageCurrentVersionId)
                ? null
                : new PageVersionVM()
                {
                    SettingsBody = result["PageSettingsBody"].ToString(),
                    PageComponents = string.IsNullOrEmpty(result["PageComponents"].ToString())
                    ? JsonConvert.SerializeObject(new List<string>())
                    : result["PageComponents"].ToString()

                };

            Result.PageSettings = data;

            return Result;
        }
        private long ParseAnd0IfNull(string value)
        {
            return long.Parse(string.IsNullOrEmpty(value) ? "0" : value);
        }


        private async Task<SlugInfoSettings> fillCategoryOneToManyRelationData(DbDataReader result, SlugInfoSettings slugResult)
        {
            try
            {
                var model = new ProductCategoryVM();
                var ProductId = ParseAnd0IfNull(result["ProductId"].ToString());
                if (ProductId != 0)
                {
                    if (slugResult.CategoriesSettings.ProductCategory.FirstOrDefault(c => c.Id == ProductId) == null)
                    {
                        model.Id = ParseAnd0IfNull(result["ProductId"].ToString());
                        model.Name = result["ProductName"].ToString();

                        model.Slug = result["ProductSlug"].ToString();


                        model.IsCallForPricing = ParseAnd0IfNulltoboolean(result["IsCallForPricing"].ToString());

                        model.IsAllowToOrder = ParseAnd0IfNulltoboolean(result["IsAllowToOrder"].ToString());

                        model.ThumbnailImageUrl = result["ThumbnailImageUrl"].ToString();

                        model.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());

                        model.StockTrackingIsEnabled = ParseAnd0IfNulltoboolean(result["StockTrackingIsEnabled"].ToString());
                        model.Description = result["ProductDescription"].ToString();
                        model.ShortDescription = result["ProductShortDescription"].ToString();
                        model.ProductAdditionalMetatags = result["ProductAdditionalMetatags"].ToString();
                        model.Sku = result["SKU"].ToString();
                        var ReviewsCount = result["ReviewsCount"].ToString();
                        model.ReviewsCount = Convert.ToInt32(ReviewsCount);

                        model.RatingAverage = string.IsNullOrEmpty(result["RatingAverage"].ToString()) ? null : Convert.ToDouble(result["RatingAverage"].ToString());
                        model.ThumbnailImageUrl = result["ThumbnailImageUrl"].ToString();

                        model.SiteId = ParseAnd0IfNull(result["SiteId"].ToString());

                        slugResult.CategoriesSettings.ProductCategory.Add(model);
                    }
                }
                return slugResult;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        //public EskaCMS.Core.Areas.Core.ViewModels.Shared.CalculatedItemPrice CalculateProductPrice(decimal price, decimal? oldPrice, decimal? specialPrice, DateTimeOffset? specialPriceStart, DateTimeOffset? specialPriceEnd)
        //{
        //    var percentOfSaving = 0;
        //    var calculatedPrice = price;


        //    if (specialPrice.HasValue && specialPriceStart < DateTimeOffset.Now && DateTimeOffset.Now < specialPriceEnd)
        //    {
        //        calculatedPrice = specialPrice.Value;

        //        if (!oldPrice.HasValue || oldPrice < price)
        //        {
        //            oldPrice = price;
        //        }
        //    }
        //    if (oldPrice.HasValue && oldPrice.Value > 0 && oldPrice > calculatedPrice)
        //    {
        //        percentOfSaving = (int)(100 - Math.Ceiling((calculatedPrice / oldPrice.Value) * 100));
        //    }



        //    return new CalculatedItemPrice(_currencyService)
        //    {
        //        Price = calculatedPrice,
        //        OldPrice = oldPrice,
        //        PercentOfSaving = percentOfSaving
        //    };
        //}

        public async Task<SlugInfoSettings> GetSettingsByPreviewCodeForWebSp(string? code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    throw new NullReferenceException(code);
                long SiteId = _workContext.GetCurrentSiteId();

                long cultureId = _workContext.GetCurrentCultureId();

                var SlugData = await GetSettingsByPreviewCode(code, cultureId, SiteId, (long)EStatus.Published);
                return SlugData;

            }
            catch (Exception e)
            {

                throw;
            }

        }
        private async Task<SlugInfoSettings> GetSettingsByPreviewCode(
      string code, long CultureId, long SiteId, long Status)
        {
            await using var connection = dbFactory.CreateConnection(_connectionString, _provider);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            DbProviderHelper.ConfigureCommand(command);

            AddInputParameter(command, "@P_CODE", code, DbType.String);
            AddInputParameter(command, "@P_CULTUREID", CultureId, DbType.Int64);
            AddInputParameter(command, "@P_SITEID", SiteId, DbType.Int64);
            AddInputParameter(command, "@P_STATUS", Status, DbType.Int64);
            AddInputParameter(command, "@P_PAGESLUGTYPE", (long)SlugsTypes.Page, DbType.Int64);
            ConfigureProcedureCommand(
                command,
                "GET_PREVIEWED_PAGE_SETTINGS",
                "P_CODE",
                "P_CULTUREID",
                "P_SITEID",
                "P_STATUS",
                "P_PAGESLUGTYPE");

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.CloseConnection);

            if (await reader.ReadAsync())
                return await fillPageDataFromSP(reader);

            return null;
        }


        #region helpers

        // Reads an optional integer column. Returns null when the column is missing
        // (procedure not updated yet), NULL, or not a positive number.
        private static int? ReadOptionalInt(DbDataReader reader, string column)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (!string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (reader.IsDBNull(i))
                    return null;

                return int.TryParse(reader.GetValue(i)?.ToString(), out var value) && value > 0
                    ? value
                    : (int?)null;
            }

            return null;
        }

        public async Task<long> GetUserRoles(long userId)
        {
            return await _context.UserRoles.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Role.Type)
                .ThenBy(x => x.RoleId)
                .Select(x => x.RoleId)
                .FirstOrDefaultAsync();
        }

        private static void AddInputParameter(DbCommand command, string name, object? value, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = IsOracleCommand(command)
                ? name.TrimStart('@', ':')
                : DbProviderHelper.NormalizeParameterName(command, name);
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = ParameterDirection.Input;
            command.Parameters.Add(parameter);
        }

        private static void ConfigureProcedureCommand(DbCommand command, string procedureName, params string[] parameterNames)
        {
            if (IsOracleCommand(command))
            {
                command.CommandType = CommandType.Text;
                command.CommandText =
                    $"BEGIN {procedureName}({string.Join(", ", parameterNames.Select(name => $":{name}"))}); END;";
                return;
            }

            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
        }

        private static bool IsOracleCommand(DbCommand command) =>
            string.Equals(
                command.GetType().FullName,
                "Oracle.ManagedDataAccess.Client.OracleCommand",
                StringComparison.Ordinal);

        private static bool ParseDbBoolean(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (long.TryParse(text, out var numericValue))
            {
                return numericValue != 0;
            }

            return bool.TryParse(text, out var parsedBool) && parsedBool;
        }

        #endregion

    }
}
