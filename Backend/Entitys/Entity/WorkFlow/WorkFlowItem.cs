using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Forms;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.WorkFlow.Enums.WorkFlowEnums;

namespace EskaCMS.Entities.Entity.WorkFlow
{
    public class WorkFlowItem : EntityBase
    {

        public DateTimeOffset CreationDate { get; set; }
        public long CurrentStageId { get; set; }
        public Stage CurrentStage { get; set; }  
        public EFlowStatus Status { get; set; }
        public long? SubmittedFormId { get; set; }
        public SubmittedForm SubmittedForm { get; set; }
        public long? MediaId { get; set; }
        public Media Media { get; set; }
        public long? PageVersionId { get; set; }
        public PageVersions PageVersion { get; set; }        
        public long? TemplateVersionId { get; set; }
        public TemplateVersions TemplateVersion { get; set; }

        public long? RequiredApprovedId { get; set; }
        public User RequiredApproved { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public List<ItemHistory> ItemHistories { get; set; }


    }
}
