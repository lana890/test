using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.EmailSender.SMTP.Areas.ViewModels
{
    public class EmailTemplateSearchVM
    {
        public string SearchValue { get; set; }
        public long SiteId { get; set; }
        public EmailTemplateSearchSortVM EmailTemplateSort { get; set; }
        public EmailType type { get; set; }
		public int PageSize { get; set; }
        public int PageNo { get; set; }
		public string cultureId { get; set; }
	}
    public class EmailTemplateSearchSortVM
    {
        public EmailTemplateSearchSortFieldsEnum SortField { get; set; }
        public EmailTemplateSearchSortTypeEnum SortType { get; set; }

    }
    public enum EmailTemplateSearchSortFieldsEnum
    {
        Id = 1,
        Subject = 2,
        EmailType = 3,
        Date = 4
    }
    public enum EmailTemplateSearchSortTypeEnum
    {
        asc = 1,
        desc = 2,
    }

	public enum EmailType
	{
		Payment = 1,
		FromBuilder = 2,
        UserEmail = 3,
        OrderInvoice=4,
        All= 5,
	}


	public class EmailTemplateListOutputVM<T>
    {
        public List<T> List { get; set; }
        public int TotalCount { get; set; }

    }
    public class EmailTemplateOutputVM
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public string EmailType { get; set; }
		public string CultureId { get; set; }


	}

	public class GetEmailTemplateByIdVM
    {
        public string Id { get; set; }

    }
}
