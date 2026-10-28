
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Entities.Entity.Core
{
    [Index(nameof(ProviderValue), Name = "UserNameidx")]
    [Index(nameof(Purpose), Name = "Purposeidx")]
    [Index(nameof(Value), Name = "Valueidx")]
    public class UserOTP : EntityBase
    {

        public long UserId { get; set; }

        public User User { get; set; }

        public string Value { get; set; }
        public Enum.GeneralEnums.E2FAProviders ProviderType { get; set; }
        public string ProviderValue { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Purpose { get; set; }
        public TimeSpan Validity { get; set; }

        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public DateTimeOffset ExpiryDate { get; set; }
        public string Username { get; set; }



    }


}
