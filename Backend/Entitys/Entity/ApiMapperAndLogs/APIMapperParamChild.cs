using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Entity.ApiMapperAndLogs
{
    public class APIMapperParamChild:EntityBase
    {
        public long Id {  get; set; }
        public long ApiMapperId {  get; set; }
        public string ParamName {  get; set; }
        public long ApiMapperParamChildId {  get; set; }
        public long ExecutionOrder {  get; set; }

        /// <summary>
        /// Where to read the value inside the parent API response, when it is not a plain property of it:
        /// a path like data.customer.id, items[0].id or $.data.id. Optional - when it is empty the value is
        /// looked up by the parent output param name (at any level) as before.
        /// </summary>
        public string ResponsePath { get; set; }
        //Navigations
        public ApiMapper ApiMapper {  get; set; }
        public ApiMapperParams ApiMapperParams { get; set; }



		public long? ParentId { get; set; }
		public virtual ApiMapper Parent { get; set; }
	}
}
