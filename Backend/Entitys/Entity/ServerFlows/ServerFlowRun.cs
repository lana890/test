using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.ServerFlows
{
    public enum ServerFlowRunStatus
    {
        /// <summary>Ready to run now.</summary>
        Queued = 1,

        /// <summary>A worker holds the lease and is running it.</summary>
        Running = 2,

        /// <summary>A step failed in a way worth retrying; runs again at NextAttemptAt.</summary>
        Waiting = 3,

        Succeeded = 4,

        /// <summary>Stopped until a person looks at it (retries used up, a business error, an unclear result).</summary>
        NeedsAttention = 5,

        /// <summary>Created, but only starts when an admin starts it (e.g. the order was not paid by a verified card payment).</summary>
        AwaitingApproval = 6,

        /// <summary>Stopped by an admin; never runs again unless started again.</summary>
        Stopped = 7,
    }

    /// <summary>
    /// One run of a server flow for one subject (an order). RunKey is unique, so the same flow can
    /// never run twice for the same order, whatever triggers it and however often.
    /// </summary>
    public class ServerFlowRun : EntityBase
    {
        /// <summary>The longest texts the capped columns keep (the writers cut to these).</summary>
        public const int NameLength = 100;
        public const int ReferenceLength = 200;
        public const int ReasonLength = 1000;
        public const int ErrorLength = 2000;

        public ServerFlowRun()
        {
            CreatedOn = DateTimeOffset.UtcNow;
            UpdatedOn = CreatedOn;
            Status = ServerFlowRunStatus.Queued;
        }

        public long SiteId { get; set; }

        /// <summary>The exact flow version this run uses.</summary>
        public long FlowId { get; set; }

        public ServerFlow Flow { get; set; }

        [StringLength(100)]
        public string FlowName { get; set; }

        /// <summary>What the run is about, e.g. "order".</summary>
        [StringLength(30)]
        public string SubjectType { get; set; }

        public long SubjectId { get; set; }

        /// <summary>"{SubjectType}:{SubjectId}:{FlowName}", unique.</summary>
        [StringLength(200)]
        public string RunKey { get; set; }

        public ServerFlowRunStatus Status { get; set; }

        /// <summary>How many times the run was picked up (not the per-step attempts).</summary>
        public int Attempts { get; set; }

        public DateTimeOffset? NextAttemptAt { get; set; }

        [StringLength(100)]
        public string LeaseOwner { get; set; }

        public DateTimeOffset? LeaseUntil { get; set; }

        /// <summary>The request the run is on (or stopped at).</summary>
        [StringLength(NameLength)]
        public string CurrentStep { get; set; }

        /// <summary>The data the run reads, frozen when it is created (order, items, payment) as JSON.</summary>
        public string Context { get; set; }

        /// <summary>Declared outputs of the finished requests, { "requestName": { "output": value } }, as JSON.</summary>
        public string Outputs { get; set; }

        /// <summary>
        /// Bodies an admin wrote by hand, { "requestName": body }, as JSON. The request sends that body
        /// instead of building one, until it succeeds (then its entry is removed).
        /// </summary>
        public string RequestOverrides { get; set; }

        /// <summary>The value shown to the customer when the run succeeds, e.g. the policy number.</summary>
        [StringLength(ReferenceLength)]
        public string Reference { get; set; }

        /// <summary>Why the run waits or needs attention, in plain words for the admin page.</summary>
        [StringLength(ReasonLength)]
        public string Reason { get; set; }

        [StringLength(ErrorLength)]
        public string LastError { get; set; }

        /// <summary>Admin who started, retried or stopped the run last (null when only the system touched it).</summary>
        public long? LastActionById { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public DateTimeOffset? CompletedOn { get; set; }
    }
}
