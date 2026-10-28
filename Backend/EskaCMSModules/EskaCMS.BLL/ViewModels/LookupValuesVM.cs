using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.General;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.General.ViewModels
{
    public class LookupValuesVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public long Lookup_Order { get; set; }
        public long LookupId { get; set; }
        public long? ParentId { get; set; }
        public Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public List<LookupValuesVM> Children { get; set; }
        public Role Roles { get; set; }   
    }
    public class LookupValuesAddVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public long Lookup_Order { get; set; }
    }
    public class LookupValuesWithLookupId
    {
        public List<LookupValues> LookupValuesList { get; set; }
        public long lookupId { get; set; }
        public long TotalCount { get; set; }
    }
    public class LookupValuesWithLookupIdModel
    {
        public List<LookupValuesVM> LookupValuesList { get; set; }
        public long lookupId { get; set; }
        public long TotalCount { get; set; }
    }

    public class LookupValuesModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public long Lookup_Order { get; set; }
        public long LookupId { get; set; }
        public long? ParentId { get; set; }
        //public List<LookupValuesVM> Children { get; set; }

    }

     public class LookupValuesWithoutOrder
     {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public long LookupId { get; set; }
        public long? ParentId { get; set; }
        public Entities.Enum.GeneralEnums.EStatus Status { get; set; }
     }


    public class LookupValuesWithOrder
    {
        public long LookupId { get; set; }
        public List<LookupValuesWithoutOrderVM> SortedList { get; set; }
    }
    public class LookupValuesWithoutOrderVM
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public long  Lookup_Order { get; set; }

    }
    public class LookupValuesDetails
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<LookupValuesVM> LookupValuesList { get; set; }
    }
    public class LookupValueVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public long Lookup_Order { get; set; }
        public long LookupId { get; set; }
        public long? ParentId { get; set; }
        public Entities.Enum.GeneralEnums.EStatus Status { get; set; }
    }
}
