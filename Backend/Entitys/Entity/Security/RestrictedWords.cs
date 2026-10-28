using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Security
{

    [Index(nameof(NormalizedRestrictedWord), Name = "NormalizedRestrictedWordidx")]
    public class RestrictedWords : EntityBase
    {
        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public string RestrictedWord
        {
            get
            {
                return _RestrictedWord;
            }

            set
            {
                _RestrictedWord = value;
                _NormalizedRestrictedWord = value.ToUpper();
            }
        }
        public string NormalizedRestrictedWord
        {
            get
            {
                return _NormalizedRestrictedWord;
            }
            set
            {

                this._NormalizedRestrictedWord = value;

            }
        }
        public virtual User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public User ModifiedBy { get; set; }
        public long? ModifiedById { get; set; }

        [NotMapped]
        private string _RestrictedWord;
        
        [NotMapped]
        private string _NormalizedRestrictedWord;

    }
}
