using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using EskaCommerce.Reviews.Models.ReplayModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Models.ReviewModels.ReviewPosts
{

    public class ReviewPostVM
    {
        [Range(0, 5)]
        public int Rating { get; set; }

        public ReviewStatus? Status { get; set; }

        public string Title { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Comment { get; set; }
        public long PostId { get; set; }

    }

    public class ReviewPostItem
    {
        public long Id { get; set; }

        public int Rating { get; set; }

        public string Title { get; set; }

        public string Comment { get; set; }

        public string ReviewerName { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public long  UserId { get; set; }

        public IList<ReplyViewModel> Replies { get; set; } = new List<ReplyViewModel>();
        public IList<PostsReviewer> Posts { get; set; } = new List<PostsReviewer>();
        public IList<ReviewViewMedia> Medias { get; set; } = new List<ReviewViewMedia>();


    }
    public class PostsReviewer
    {
        public long PostId { get; set; }
        public string PostName { get; set; }
    }


    public class ReviewPostsVm
    {
        public int ReviewsCount { get; set; }

        public double RatingAverage
        {
            get; set;
        }

        public double GetRatingPercent(int rateCount)
        {
            if (ReviewsCount > 0)
            {
                return (double)rateCount / ReviewsCount * 100;
            }

            return 0;
        }

        public int Rating1Count { get; set; }

        public int Rating2Count { get; set; }

        public int Rating3Count { get; set; }

        public int Rating4Count { get; set; }

        public int Rating5Count { get; set; }

        public PaginationResult<ReviewPostItem> Items { get; set; }
    }
}
