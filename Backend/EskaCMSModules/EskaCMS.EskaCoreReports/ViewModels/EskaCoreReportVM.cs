using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.EskaCoreReports;
using EskaCMS.Entities.Entity.Menus;
using EskaCMS.Menus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.BlogArchive;

namespace EskaCMS.EskaCoreReports.ViewModels
{
    public class EskaCoreReportVM
    {
        public long? Id { get; set; }
        public long MenuId { get; set; }
        public EStatus? Status { get; set; }
        public List<long> PostIds { get; set; }
    }

    public class SlugVM
    {
        public string Slug { get; set; }
    }
    public class ReportsBreadCrumbVM
    {
        public List<MenuBreadCrumbVM> MenuBreadCrumb { get; set; }
        public long PostId { get; set; }
        public string PostTitle { get; set; }
    }



    public class EskaCoreReportRepositryVM
    {
        public long Id { get; set; }
        public long MenuId { get; set; }
        public EStatus? Status { get; set; }
        public long PostId { get; set; }
        public long ReportId { get; set; }
    }
    public class ReportDetailsVM
    {
        public long Id { get; set; }
        public long MenuId { get; set; }
        public long? MenuCategoryId { get; set; }
        public long MenuGroupId { get; set; }
        public string MenuName { get; set; }
        public string PostName { get; set; }
        public string ShortDescription { get; set; }
        public string Author { get; set; }
        public DateTimeOffset PostCreationDate { get; set; }
        public DateTimeOffset? PostPublishDate { get; set; }
        public DateTimeOffset? PostExpiryDate { get; set; }
        public List<string> PostsCategories { get; set; }
        public List<string> PostsRoles { get; set; }
        public EStatus Status { get; set; }
        public EStatus PostStatus { get; set; }
        public List<MediaReport>? MediaReport { get; set; }

        public Menu Menu { get; set; }
        public long PostId { get; set; }

        public string Slug { get; set; }
        public string Customers { get; set; }
        
        public long TemplateId { get; set; }

    }
    public class ReportMenuListVM
    {
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public string Thumbnail { get; set; }        
        public long TemplateId{ get; set; }  
        public DateTimeOffset CreationDate{ get; set; }  
        public List<MediaReport>? MediaReport{ get; set; }  
        

    }

    public class MediaReport
    {
        public long Id { get; set; }
        public string publicUrl { get; set; }
      


    }
    public class PagenationReportMenuListVM
    {
        public long TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<ReportMenuListVM> ReportList { get; set; }


    }

    public class ReportsByMenuIdSearchVM
    {
        public long? MenuId { get; set; }
        public string Title { get; set; }
        public List<long> CategoryIds { get; set; }
        public List<long> TagIds { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public OrderByEnum FilterBy { get; set; }
        public EStatus Status { get; set; }
        public List<long> Authors { get; set; }


    }
    public class ReportVM
    {
        public long Id { get; set; }
        public long MenuId { get; set; }
        public long? MenuCategoryId { get; set; }
        public long MenuGroupId { get; set; }
        public string MenuName { get; set; }

        public long ReportPostId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public long PostId { get; set; }

        public DateTimeOffset PostCreationDate { get; set; }
        public DateTimeOffset? PostPublishDate { get; set; }
        public DateTimeOffset? PostExpiryDate { get; set; }
        public List<string> PostsCategories { get; set; }
        public EStatus Status { get; set; }

       

    }

    public class ReportHistoryVM
    {
        public EStatus? OldStatus { get; set; }
        public EStatus NewStatus { get; set; }
        public string NewStatusString { get; set; }
        public string? OldStatusString { get; set; }
        public long ModifyById { get; set; }
        public DateTimeOffset ModifyDate { get; set; }
        public string ModifyBy { get; set; }



    }


}
