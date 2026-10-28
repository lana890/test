using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ApiMapperAndLogs;
using EskaCMS.Entities.Entity.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class StaticInvokeVM
    {
        public string ApiUrl { get; set; }
        public string HttpMethod { get; set; }
        public string ContentType { get; set; }
        public JObject Requestdata { get; set; }

    }
    public class SearchApiMapperTransactionHistory
    {
        public long? ApiMapperId { get; set; }

        public string SearchText { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
    }
    public class GetApiMapperTransactionLogVM
    {
        public long ApiMapperId { get; set; }
        public string ApiMapperName { get; set; }
        public string Request { get; set; }

        public string Response { get; set; }

        public string Headers { get; set; }

        public string Token { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }

        public string PhoneNumber { get; set; }

        public User CreatedBy { get; set; }
    }
    public class GetApiMapperTransactionLogHistoryVM
    {
        public string Request { get; set; }

        public string Response { get; set; }

        public string Headers { get; set; }

        public string Token { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }

        public string PhoneNumber { get; set; }

        public User CreatedBy { get; set; }
    }
    public class DatasourceVM
    {
        public ApiMapperViewModel apiMapperViewModel { get; set; }
        public List<ApiMapperParamsViewModel> inputs { get; set; }
        public List<OutputsParameters> outputs { get; set; }
        public List<string> paths { get; set; }
        public ETokenSource? tokenSource { get; set; }

        /// <summary>
        /// Optional (child input parameter -&gt; parent API output parameter) links to create together with the
        /// datasource, the same links the LinkChildParamToParent API creates for an already saved datasource.
        /// </summary>
        public List<DatasourceChildParamVM> children { get; set; }

    }

    /// <summary>
    /// One (child input parameter -&gt; parent API output parameter) link posted together with a datasource.
    /// </summary>
    public class DatasourceChildParamVM
    {
        /// <summary>
        /// The input parameter of the datasource being saved. The inputs can be created by the same call, so the
        /// link is matched by <see cref="ParamName"/>; Id is only used when no name is sent.
        /// </summary>
        public long? Id { get; set; }
        public string ParamName { get; set; }

        /// <summary>
        /// The parent API output parameter that feeds the input above, and the API that owns it
        /// (dependeOnAnotherApiId, optional: it is derived from the output param when it is not sent).
        /// </summary>
        public long? parentId { get; set; }
		public long? ApiMapperParamChildId { get; set; }
		public long executionOrder { get; set; }
        public long? ApiMapperId { get; set; }

        /// <summary>
        /// Optional path to read the value inside the parent API response when it is not a plain property of it:
        /// data.customer.id, items[0].id, $.data.id. Empty means "look the parent output param name up at any level".
        /// </summary>
        public string ResponsePath { get; set; }
    }

    public class OutputsParameters
    {
		public long? Id{ get; set; }
		public string Name { get; set; }
        public string Alias { get; set; }

    }

    public class LinkChildParamToParentRequest {
		public long childParamId { get; set; }
		public long parentOutputParamId { get; set; }
	}

	public class ApiMapperViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string URL { get; set; }
        public ApihttpMethods HttpMethod { get; set; }
        public string MediaTypes { get; set; }
        public ApiTypes Type { get; set; }
        public EStatus Status { get; set; }
        public ICollection<ApiMapperParams> ApiMapperParams { get; set; }
		public ICollection<APIMapperParamChild> APIMapperParamChild { get; set; }

		public ApiAuthorizationTypes AuthorizationType { get; set; }
        public List<ApiMapperAuthorization> AuthorizationKeys { get; set; }
        public ApiReponseType ApiReponseType { get; set; }
        public long? SiteId { get; set; }
        public string ContractName { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public string WebMethodName { get; set; }
        public string Path { get; set; }
        public List<string> OutputPath { get; set; }
        public JObject obj { get; set; }
        public ETokenSource? TokenSource { get; set; }
        public long? ParentId { get; set; }

        public string? token { get; set; }
        public bool EnableLog { get;  set; }
    }



    public class ApiMapperDetailsViewModel
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string URL { get; set; }
        public ApihttpMethods HttpMethod { get; set; }
        public string MediaTypes { get; set; }
        public ApiTypes Type { get; set; }
        public EStatus Status { get; set; }
        public ApiAuthorizationTypes AuthorizationType { get; set; }
        public List<ApiMapperAuthorization> AuthorizationKeys { get; set; }
        public ApiReponseType ApiReponseType { get; set; }
        public long? SiteId { get; set; }
        public string ContractName { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public string WebMethodName { get; set; }
        public ICollection<ApiMapperParams> ApiMapperParams { get; set; }
		public ICollection<APIMapperParamChild> APIMapperParamChild { get; set; }

		public List<ApiMapperParamsViewModel> inputs { get; set; }
		public List<ApiMapperParamsViewModel> parentInputs { get; set; }


		public List<OutputsParameters> outputs { get; set; }
        public string path { get; set; }
        public long? ParentId { get; set; }
        public bool EnableLog { get;  set; }
    }

    public class inputValuesViewModel
    {
        public SourceType SourceType { get; set; }
        public Dictionary<string, string> inputValue { get; set; }
        public string path { get; set; }
    }





}
