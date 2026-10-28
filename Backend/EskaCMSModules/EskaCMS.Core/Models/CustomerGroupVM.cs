using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Core.Models
{
    public class CustomerGroupVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FireBaseTopic { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public int DepartmentId { get; set; }
        public string Email { get; set; }
    } 
    public class CustomerGroupAdditionalInfoVM
    {
        public int DepartmentId { get; set; }
        public string DepartmentCode { get; set; }
    }
}
