using EskaCMS.Entities.Entity.ApiMapperAndLogs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class ApiMapperParamsViewModel
    {
        public long Id { get; set; }

        public long ApiMapperId { get; set; }

       public string ParamName { get; set; }

         public int ParamOrder { get; set; }

        public ParameterDirection Direction { get; set; }
        public ParamsTypes ParamType { get; set; }
        public long CreatedById { get; set; }
        public SourceType SourceType { get; set; }
        public string ParamValue { get; set; }
        public string DefaultValue { get; set; }
        public bool IsDeleted { get; set; }
        public string Alias { get; set; }
		public ICollection<APIMapperParamChild> APIMapperParamChild { get; set; }

		public long? ParentId{ get; set; }

	}
    public class APIMapperParamTypesVM
    {
        public string QueryString { get; set; }
        public ExpandoObject BodyObj { get; set; } = new ExpandoObject();
        public ExpandoObject HeaderObj { get; set; } = new ExpandoObject();
        public string Path { get; set; }
        public MultipartFormDataContent FormDataObj { get; set; } = new MultipartFormDataContent();
        public ExpandoObject FormDataFields { get; set; } = new ExpandoObject();
        public List<Object> BodyArray { get; set; } = null;
    }
}
