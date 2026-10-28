using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Models.ReviewModels
{
    public class ReviewForm
    {
        [Range(0, 5)]
        public int Rating { get; set; }

        public string Title { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Comment { get; set; }
        public long ProductId { get; set; }

        public ReviewStatus? Status { get; set; }

    }
}
