using EskaCMS.BLL.Services.Interfaces;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.AspNetCore.Identity;
using EskaCMS.Menus.Models;
using EskaCMS.EmailSender.SMTP.Services;
using System.Globalization;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Sites.Models;
using EskaCMS.Entities.Entity.Menus;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.BlogArchive;

namespace EskaCMS.General.Services
{
    public class GeneralServices : IGeneral
    {
        private readonly IRepository<BuildVersions> _BuildVersionsRepository;
        private readonly string _BundleDestination;
        private readonly IRepository<Posts> _PostRepository;
        private UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        private readonly string _WebDestination;
        //  private readonly IRepository<TypesSettings> _typesSettingsRepository;
        private readonly IRepository<SitesHostNames> _SitesHostNameRepository;


        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<PagesRoles> _pageRoleRepository;
        private readonly IRepository<Menu> _MenuRepository;


        private readonly IRepositoryWithTypedId<Culture, string> _CultureRepository;
        public GeneralServices(RoleManager<Role> roleManager, IRepository<Menu> MenuRepository, IRepository<Posts> postRepository, IRepository<Category> postCategoriesRepository,
        IRepository<Page> pageRepository, IRepository<BuildVersions> BuildVersionsRepository,
        IConfiguration Configuration, UserManager<User> userManager,
        IRepository<PagesRoles> pageRoleRepository, IRepositoryWithTypedId<Culture, string> CultureRepository, IRepository<SitesHostNames> SitesHostNameRepository)
        {
            _BuildVersionsRepository = BuildVersionsRepository;
            //  _typesSettingsRepository = typesSettingsRepository;
            _pageRepository = pageRepository;
            _MenuRepository = MenuRepository;
            _PostRepository = postRepository;
            //_FormBuilderRepository = FormBuilderRepository;
            // _AppKeys = Configuration["AppSettings:CMSConfig"];
            _pageRoleRepository = pageRoleRepository;
            _BundleDestination = Configuration.GetSection("CMSConfig").GetSection("BuilderDestination").Value;
            _WebDestination = Configuration.GetSection("CMSConfig").GetSection("WebDestination").Value;
            _userManager = userManager;
            _roleManager = roleManager;
            _CultureRepository = CultureRepository;
            _SitesHostNameRepository = SitesHostNameRepository;

        }
        public async Task<bool> TransferCMSFiles(string FileName, string Description, long CreatedById, long SiteId, EComponentType ComponentType, IFormFileCollection Files)
        {
            try
            {
                return await _BuildVersionsRepository.ExecuteInTransactionAsync<bool>(async () =>
                {
                    BuildVersions OBuildVersions = new BuildVersions();

                    OBuildVersions.FileName = FileName;
                    OBuildVersions.Description = Description;
                    OBuildVersions.CreatedById = CreatedById;
                    OBuildVersions.CreationDate = DateTimeOffset.Now;
                    //OBuildVersions.SiteId = SiteId;

                    string pathToSave = string.Empty;
                    if (ComponentType == Entities.Enum.GeneralEnums.EComponentType.Web)
                    {
                        pathToSave = Path.Combine(Directory.GetCurrentDirectory(), _WebDestination.Trim());
                        OBuildVersions.ComponentType = Entities.Enum.GeneralEnums.EComponentType.Web;
                    }
                    else
                    {
                        pathToSave = Path.Combine(Directory.GetCurrentDirectory(), _BundleDestination.Trim());
                        OBuildVersions.ComponentType = Entities.Enum.GeneralEnums.EComponentType.Builder;
                    }

                    _BuildVersionsRepository.Add(OBuildVersions);
                    await _BuildVersionsRepository.SaveChangesAsync();


                    for (int i = 0; i < Files.Count; i++)
                    {
                        var file = Files[i];
                        var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                        var fullPath = Path.Combine(pathToSave, fileName.Trim());

                        if (!Directory.Exists(pathToSave))
                            Directory.CreateDirectory(pathToSave);

                        using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                            file.CopyTo(stream);
                    }
                    return true;
                });
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        //public async Task<bool> AddNewTypesSettings(TypesSettingsViewModel TypesSettings)
        //{
        //    try
        //    {

        //        var ObjTypesSettings = new TypesSettings();
        //        ObjTypesSettings.Type = TypesSettings.Type;
        //        ObjTypesSettings.PageId = TypesSettings.PageId;
        //        ObjTypesSettings.PostId = TypesSettings.PostId;
        //        ObjTypesSettings.MetaDescription = TypesSettings.MetaDescription;
        //        ObjTypesSettings.MetaKeywords = TypesSettings.MetaKeywords;
        //        ObjTypesSettings.RedirectUrl = TypesSettings.RedirectUrl;
        //        ObjTypesSettings.RedirectType = TypesSettings.RedirectType;
        //        ObjTypesSettings.Route = TypesSettings.Route;


        //        _typesSettingsRepository.Add(ObjTypesSettings);
        //        await _typesSettingsRepository.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }
        //}

        public List<BuildVersions> GetAllVersion()
        {
            try
            {
                return _BuildVersionsRepository.Query().Include(y => y.CreatedBy).Select(x => x).OrderByDescending(x => x.CreationDate).Take(200).ToList();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


        public List<string> GetAllCulture()
        {
            try
            {

                var additionalCulturesToBeAdded = new List<string> { "ku-KU" };
                var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                List<string> cultures = new List<string>();
                foreach (var ci in allCultures.Where(x => x.IsNeutralCulture == false).ToList())
                {
                    if (!ci.Name.IsNullOrEmpty())
                    {
                        cultures.Add(ci.Name);
                    }
                }
                return additionalCulturesToBeAdded.Concat(cultures).ToList();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        //public async Task<bool> EditTypesSettings(long TypesSettingsId, TypesSettingsViewModel TypesSettings)
        //{
        //    try
        //    {
        //        var ObjTypesSettings = await _typesSettingsRepository.Query().Where(x => x.Id == TypesSettingsId).FirstOrDefaultAsync();

        //        ObjTypesSettings.Type = TypesSettings.Type;
        //        ObjTypesSettings.PageId = TypesSettings.PageId;
        //        ObjTypesSettings.PostId = TypesSettings.PostId;
        //        ObjTypesSettings.MetaDescription = TypesSettings.MetaDescription;
        //        ObjTypesSettings.MetaKeywords = TypesSettings.MetaKeywords;
        //        ObjTypesSettings.RedirectUrl = TypesSettings.RedirectUrl;
        //        ObjTypesSettings.RedirectType = TypesSettings.RedirectType;
        //        ObjTypesSettings.Route = TypesSettings.Route;

        //        //   _pageRepository.Update(ObjPage);
        //        await _typesSettingsRepository.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }

        //}
        //public async Task<TypesSettings> GetTypesSettingsByTypesSettingsId(long TypesSettingsId)
        //{
        //    try
        //    {
        //        if (TypesSettingsId == 0)
        //            throw new Exception("Types Settings Id is missing");
        //        else
        //            return await _typesSettingsRepository.Query().Where(y => y.Id == TypesSettingsId).FirstOrDefaultAsync();

        //    }
        //    catch (Exception exc)
        //    {

        //        throw exc;
        //    }
        //}
        //public async Task<TypesSettings> GetTypesSettingsByPostId(long PostId)
        //{
        //    try
        //    {
        //        if (PostId == 0)
        //            throw new Exception("Post Id is missing");
        //        else
        //            return await _typesSettingsRepository.Query().Where(y => y.PostId == PostId).FirstOrDefaultAsync();

        //    }
        //    catch (Exception exc)
        //    {

        //        throw exc;
        //    }
        //}

        //public async Task<bool> DeleteTypesSettings(long TypesSettingsId)
        //{
        //    try
        //    {
        //        var ObjTypesSettingsId = await _typesSettingsRepository.Query().Where(x => x.Id == TypesSettingsId).FirstOrDefaultAsync();
        //        _typesSettingsRepository.Remove(ObjTypesSettingsId);
        //        await _typesSettingsRepository.SaveChangesAsync();
        //        return true;

        //    }
        //    catch (Exception exc)
        //    {

        //        throw exc;
        //    }
        //}


        public async Task<MenuRolesViewModel> GetPageAssginedRoles(long PageId, long SiteId)
        {
            try
            {
                var data = new MenuRolesViewModel();

                data.assingedRoles = await _pageRoleRepository.Query().Include(x => x.Role).Where(x => x.PageId == PageId).Select(x =>
                 new Role
                 {
                     Id = x.Role.Id,
                     Name = x.Role.Name,
                     Type = x.Role.Type,
                 }).ToListAsync();


                var unassingedRoles = await _roleManager.Roles.Where(x => x.SiteId == SiteId || x.SiteId == null).Select(x =>
                   new Role
                   {
                       Id = x.Id,
                       Name = x.Name,
                       Type = x.Type,
                   }).ToListAsync();


                var found = false;
                data.unassingedRoles = new List<Role>();
                foreach (var item in unassingedRoles)
                {
                    found = false;
                    foreach (var item2 in data.assingedRoles)
                    {
                        if (item.Id == item2.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        data.unassingedRoles.Add(item);
                    }

                }


                return data;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<bool> SavePageAssginedRoles(MenuRolesViewModel vm)
        {
            try
            {
                var oldRoles = await _pageRoleRepository.Query().Where(x => x.PageId == vm.Id).Select(x => x).ToListAsync();
                foreach (var item in oldRoles)
                {
                    _pageRoleRepository.Remove(item);
                }

                await _pageRoleRepository.SaveChangesAsync();
                var RelatedPages = await _pageRepository.Query().Where(x => x.ParentId == vm.Id).ToListAsync();

                foreach (var value in RelatedPages)
                {
                    foreach (var item in vm.assingedRoles)
                    {
                        PagesRoles newRole = new PagesRoles();
                        newRole.PageId = value.Id;
                        newRole.RoleId = item.Id;
                        newRole.CreatedById = vm.UserId;
                        _pageRoleRepository.Add(newRole);
                        await _pageRoleRepository.SaveChangesAsync();

                    }

                }
                await _pageRoleRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<SiteSlugsVM>> GetSiteSlugs(long siteId, long UserId)
        {
            try
            {
                var pages = await _pageRepository.Query().Include(x => x.SlugInfo).Where(x => x.Status == EStatus.Published /*published*/
                //&& x.IsLoginRequired == false
                && x.SiteId == siteId
                   ).ToListAsync();

                //if (UserId != 0)
                //{
                //    var user = await _userManager.FindByIdAsync(UserId.ToString());
                //    if (user.Roles.Count != 0)
                //    {
                //        var userpages = await _pageRoleRepository.Query().Include(x => x.Page).Where(x => x.RoleId == user.Roles[0].RoleId && x.Page.Status == EStatus.Published /*published*/
                //                             && x.Page.SiteId == siteId).Select(x => x.Page).ToListAsync();

                //        foreach (var item in userpages)
                //        {
                //            pages.Add(item);
                //        }

                //    }

                //}
                //   var menus = await _MenuRepository.Query().Where(y => y.Type == GeneralEnums.EMenuTypes.Static && y.Status == GeneralEnums.EStatus.Published  /*static & published*/
                //   && y.SiteId == siteId).ToListAsync();

                var blogs = await _PostRepository.Query()
                    .Include(x => x.SlugInfo)
                    .Where(y => y.SiteId == siteId && y.Status != EStatus.Deleted
                && (y.ExpiryDate.Value.Date > DateTimeOffset.Now.Date || y.ExpiryDate == null) && y.StatusDate.Date <= DateTimeOffset.Now.Date)
                    .ToListAsync();

                var slugs = new List<SiteSlugsVM>();

                var temp = new SiteSlugsVM();

                foreach (var item in pages)
                {
                    temp = new SiteSlugsVM();
                    temp.Id = item.Id;
                    temp.Name = item.Name;
                    temp.Slug = item.SlugInfo.Slug;
                    // temp.Type = "page";

                    slugs.Add(temp);
                }



                //foreach (var item in menus)
                //{
                //    temp = new SiteSlugsVM();
                //    temp.Id = item.Id;
                //    temp.Name = item.Name;
                //    temp.Slug = item.Slug;
                //    temp.Type = "menu";

                //    slugs.Add(temp);
                //}




                foreach (var item in blogs)
                {
                    temp = new SiteSlugsVM();
                    temp.Id = item.Id;
                    temp.Name = item.Title;
                    temp.Slug = item.SlugInfo.Slug;
                    // temp.Type = "blog";

                    slugs.Add(temp);
                }


                return slugs;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }





        public async Task<List<string>> GetBreadcrumByType(int type, long id, long siteId)
        {
            try
            {
                var list = new List<string>();


                if (type == 1)
                {

                    var page = await _pageRepository.Query().Where(y => y.Id == id).FirstOrDefaultAsync();
                    list.Add(page.Name);
                    return list;
                }
                else if (type == 2)
                {
                    var blog = _PostRepository.Query().Where(y => y.Id == id).FirstOrDefault();

                    //var cat = await _PostCategoriesRepository.Query().Where(x => x.Id == blog.PostCategoryId).FirstOrDefaultAsync();
                    //list.Add(cat.Name);
                    list.Add(blog.Title);

                    return list;

                }


                return list;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        private List<string> GetMenuPath(long? id)
        {
            var list = new List<string>();
            var menu = _MenuRepository.Query().Where(y => y.Id == id).FirstOrDefault();


            if (menu.ParentId != null)
            {
                list.Add(menu.Name);
                return GetMenuName(menu.ParentId, list);

            }
            else
            {
                list.Add(menu.Name);
                return list;
            }

        }


        private List<string> GetMenuName(long? id, List<string> list)
        {

            var menu = _MenuRepository.Query().Where(y => y.Id == id).FirstOrDefault();
            list.Add(menu.Name);

            if (menu.ParentId != null)
            {
                return GetMenuName(menu.ParentId, list);
            }
            else
            {
                list.Reverse();
                return list;

            }

        }




        public async Task<string> CheckSiteSlugAsync(long siteId, string slug)
        {

            try
            {
                var Page = await _pageRepository.Query().AsNoTracking().Include(x => x.SlugInfo).Where(x => x.Status != EStatus.Inactive /*published*/
           && x.SiteId == siteId && x.SlugInfo.Slug == slug).FirstOrDefaultAsync();
                if (Page != null)
                {
                    return "This Slug already Used By the Page ( " + Page.Name + " ) , please change it";
                }
                //var m = await _MenuRepository.Query().Where(y => y.Type == GeneralEnums.EMenuTypes.Static && y.Status != GeneralEnums.EStatus.Deleted /*static & published*/
                //&& y.SiteId == siteId && y.Slug == slug).FirstOrDefaultAsync();
                //if (m != null)
                //{
                //    return "This Slug already Used By the Menu ( " + m.Name + " ) , please change it";
                //}
                var Post = await _PostRepository.Query().AsNoTracking().Include(x => x.SlugInfo).Where(y => y.SiteId == siteId
                && y.Status != EStatus.Deleted

               && y.SlugInfo.Slug == slug).FirstOrDefaultAsync();
                if (Post != null)
                {
                    return "This slug already used by the blog ( " + Post.Title + " ) , please change it";
                }

                return "ok";

            }
            catch (Exception)
            {
                return "ok";
            }



        }

        public async Task<List<SitesHostNamesViewModel>> GetCulturebysiteId(long SiteId)
        {
            try
            {
                var Sites = await _SitesHostNameRepository.Query().Include(x => x.Culture).Where(x => x.SiteId == SiteId).Select(s => new SitesHostNamesViewModel
                {

                    CultureId = s.CultureId,
                    CultureName = s.Culture.CultureId,

                }).Distinct().ToListAsync();
                return Sites.ToList();


            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public DateTime? HijriToGreg(string Hijri)
        {

            string[] allFormats = {"yyyy/MM/dd","yyyy/M/d",
                                    "dd/MM/yyyy","d/M/yyyy",
                                    "dd/M/yyyy","d/MM/yyyy","yyyy-MM-dd",
                                    "yyyy-M-d","dd-MM-yyyy","d-M-yyyy",
                                    "dd-M-yyyy","d-MM-yyyy","yyyy MM dd",
                                    "yyyy M d","dd MM yyyy","d M yyyy",
                                    "dd M yyyy","d MM yyyy"};

            if (Hijri.Contains("T"))
                Hijri = Hijri.Remove(Hijri.IndexOf("T"));

            CultureInfo arSA = new CultureInfo("ar-SA");
            arSA.DateTimeFormat.Calendar = new UmAlQuraCalendar();
            try
            {
                var dateValue = DateTime.ParseExact(Hijri, allFormats, arSA);
                return dateValue;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public string GregToHijri(string greg)

        {
            string[] allFormats = {"yyyy/MM/dd","yyyy/M/d",
                                    "dd/MM/yyyy","d/M/yyyy",
                                    "dd/M/yyyy","d/MM/yyyy","yyyy-MM-dd",
                                    "yyyy-M-d","dd-MM-yyyy","d-M-yyyy",
                                    "dd-M-yyyy","d-MM-yyyy","yyyy MM dd",
                                    "yyyy M d","dd MM yyyy","d M yyyy",
                                    "dd M yyyy","d MM yyyy"};
            CultureInfo arCul = new CultureInfo("ar-SA");
            CultureInfo enCul = new CultureInfo("en-US");
            DateTime tempDate = DateTime.ParseExact(greg, allFormats, enCul.DateTimeFormat, DateTimeStyles.AllowWhiteSpaces);
            return tempDate.ToString("yyyy-MM-dd", arCul.DateTimeFormat);


        }

    }

}

