using EskaCommerce.Reviews.Models;
using EskaCommerce.Reviews.Models.ReplayModels;
using System;
using System.Collections.Generic;

namespace EskaCommerce.Reviews.Areas.Reviews.ViewModels
{
    public class ReviewItem
    {
        public long Id { get; set; }

        public int Rating { get; set; }

        public string Title { get; set; }

        public string Comment { get; set; }

        public string ReviewerName { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public IList<ReplyViewModel> Replies { get; set; } = new List<ReplyViewModel>();
        public IList<ProductReviewer> Products { get; set; } = new List<ProductReviewer>();
        public IList<ReviewViewMedia> Medias { get; set; } = new List<ReviewViewMedia>();

    }
}
