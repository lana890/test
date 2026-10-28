using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Localization.Models
{
    public class LocalizationViewModel
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public  List<LocalizationValueVM>  LocalizationValues { get; set; }
        public string Component { get; set; }
        public string GroupId { get; set; }
        public long? SlugInfoId { get; set; }
        public string Slug { get; set; }
        public long? CultureId { get; set; }

    }
    public class LocalizationCultureViewModel
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public List<LocalizationValueVM> LocalizationValues { get; set; }
        public string Component { get; set; }
        public string GroupId { get; set; }
        public long? SlugInfoId { get; set; }
        public string Slug { get; set; }
    }
    public class LocalizationValueVM
    {
        public long? Id { get; set; }
        public string Value { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }

    }

    public class LocalizationValueEditVM
    {

        public string Value { get; set; }
        //public long CultureId { get; set; }
        public long ValueId { get; set; }

    }

    public class LocalizationVM
    {
        [Required(ErrorMessage ="Key Can Not Be Null")]
        public string Key { get; set; }
        public string Value { get; set; }

        public long? SlugInfoId { get; set; }
        public string GroupId{ get; set; }
        public EResouseScope Scope { get; set; }
        public bool IsShared { get; set; }
        public string Component { get; set; }
    }

    public class GetLocalizationVM
    {
        public Dictionary<string, string> SharedResources { get; set; } =new  Dictionary<string, string>();

        public Dictionary<string, string> SlugResources { get; set; } = new Dictionary<string, string>();

    }


    public class SlugObjVM
    {
        public string Slug { get; set; }
         
    }


    public class ScanResourceVM
    {
        public string Key { get; set; }
        public string Component { get; set; }
        public string Group { get; set; }
        public string Slug { get; set; }
        
    }

}
