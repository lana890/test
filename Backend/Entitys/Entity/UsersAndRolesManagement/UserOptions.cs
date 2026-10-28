using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Xml.Linq;
namespace EskaCMS.Entities.Entity.Core
{

    [Index(nameof(NormalizedPropertyName), Name = "NormalizedPropertyNameidx")]
    [Index(nameof(NormalizedValue), Name = "NormalizedValueidx")]
  
    public class UserOptions : EntityBase
    {
        [NotMapped]
        private string _NormalizedPropertyName;
        [NotMapped]
        private string _PropertyName;

        [NotMapped]
        private string _NormalizedValue;
        [NotMapped]
        private string _Value;
        public string PropertyName
        {
            get
            {
                return _PropertyName;
            }

            set
            {
                _PropertyName = value;
                _NormalizedPropertyName = value.ToUpper();
            }
        }
        public string NormalizedPropertyName
        {
            get
            {
                return _NormalizedPropertyName;
            }
            set
            {
              
                    this._NormalizedPropertyName = value;
               
            }
        }
        public string NormalizedValue
        {
            get
            {
                return _NormalizedValue;
            }
            set
            {
                if (value.Length < 760)
                {
                    this._NormalizedValue = value;
                }
                else
                {

                    this._NormalizedValue = "";

                }
            }
        }



        public Enum.GeneralEnums.EControlTypes ControlType { get; set; }
        public string Value
        {

            get
            {
                return _Value;

            }
            set
            {
                _Value = value;
                _NormalizedValue = value != null? value.ToUpper():null;
            }
        }
        public User User { get; set; }
        public long UserId { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEditable { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
    }
}
