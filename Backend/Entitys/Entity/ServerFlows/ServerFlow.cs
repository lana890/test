using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.ServerFlows
{
    public enum ServerFlowStatus
    {
        Draft = 1,
        Active = 2,
        Archived = 3,
    }

    /// <summary>
    /// One saved version of a server flow: an apiGroupList (the same JSON the dynamic data provider
    /// modal saves) that the server runs on its own, for example after an order is paid.
    ///
    /// Versions are never edited once saved. A change is a new Draft; activating it archives the
    /// version that was active under the same name. A run keeps the id of the version it started
    /// with, so a change never alters a run already under way.
    /// </summary>
    public class ServerFlow : EntityBase
    {
        public ServerFlow()
        {
            CreatedOn = DateTimeOffset.UtcNow;
            Status = ServerFlowStatus.Draft;
        }

        public long SiteId { get; set; }

        /// <summary>Stable name shared by all versions, e.g. "pa-after-payment".</summary>
        [StringLength(100)]
        public string Name { get; set; }

        public int Version { get; set; }

        public ServerFlowStatus Status { get; set; }

        /// <summary>What starts a run (copied from the definition so it can be queried), e.g. "orderPaid".</summary>
        [StringLength(50)]
        public string Trigger { get; set; }

        /// <summary>The flow JSON: { "settings": {...}, "apiGroupList": [...] }.</summary>
        public string Definition { get; set; }

        /// <summary>SHA-256 of Definition, so an activation approves exactly the text that was reviewed.</summary>
        [StringLength(64)]
        public string Checksum { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset? ActivatedOn { get; set; }

        public long? ActivatedById { get; set; }

        public DateTimeOffset? ArchivedOn { get; set; }

        public long? ArchivedById { get; set; }
    }
}
