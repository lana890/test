using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.ServerFlows
{
    public enum ServerFlowStepStatus
    {
        /// <summary>Written before the call is sent. Still "Calling" after a crash means the result is unknown.</summary>
        Calling = 1,

        Succeeded = 2,

        Failed = 3,

        /// <summary>Its runIf / executeIf condition was false.</summary>
        Skipped = 4,

        /// <summary>A call that changes data ended without a clear answer (timeout, crash, server error).</summary>
        Unclear = 5,
    }

    /// <summary>
    /// One attempt of one request in a run: what happened, how long it took, the body it sent, the
    /// answer it got and the declared outputs. The body and the answer are kept for the admin page
    /// (admins of the site only); values under secret-looking keys are masked and both are capped.
    /// </summary>
    public class ServerFlowStep : EntityBase
    {
        public ServerFlowStep()
        {
            StartedOn = DateTimeOffset.UtcNow;
            Status = ServerFlowStepStatus.Calling;
        }

        public long RunId { get; set; }

        public ServerFlowRun Run { get; set; }

        [StringLength(ServerFlowRun.NameLength)]
        public string GroupName { get; set; }

        [StringLength(ServerFlowRun.NameLength)]
        public string RequestName { get; set; }

        /// <summary>"call", "guard" (the check that asks whether an unclear call went through) or "manual" (an admin's decision).</summary>
        [StringLength(20)]
        public string Kind { get; set; }

        public int Attempt { get; set; }

        public ServerFlowStepStatus Status { get; set; }

        public DateTimeOffset StartedOn { get; set; }

        public DateTimeOffset? EndedOn { get; set; }

        public int? DurationMs { get; set; }

        [StringLength(ServerFlowRun.ErrorLength)]
        public string Error { get; set; }

        /// <summary>The request's declared outputs, as JSON.</summary>
        public string Outputs { get; set; }

        /// <summary>The body this attempt sent (the values given to the data source or operation), as JSON.</summary>
        public string Request { get; set; }

        /// <summary>The answer this attempt got, as JSON (for a refused call, the other side's error body when it sent one).</summary>
        public string Response { get; set; }

        /// <summary>Admin who recorded a manual decision for this request.</summary>
        public long? DecidedById { get; set; }
    }
}
