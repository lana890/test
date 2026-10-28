using EskaCMS.DynamicForms.ViewModels;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms
{
    public static class QuerableExtensions
    {

        public static IQueryable<SubmissionInfo> FilterSubmission(this IQueryable<SubmissionInfo> submissions, RequestSubmission  request)
        {

            if (!string.IsNullOrEmpty(request.FullName))
            {
                submissions = submissions.Where(n => n.Submission.CreatedBy.FullName.Contains(request.FullName));
            }
           
            if (request.FromDate.HasValue)
            {
                submissions = submissions.Where(n =>
                                     n.Submission.CreationDate.Date >= request.FromDate.Value.Date);
            }
            if (request.ToDate.HasValue)
            {
                submissions = submissions.Where(n =>
                               n.Submission.CreationDate.Date <= request.ToDate.Value.Date);
            }
         

            return submissions;

        }
        
    }
}
