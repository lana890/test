using cloudscribe.Pagination.Models;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;

namespace EskaCommerce.Reviews.Models
{
    public class ReviewVm
    {
        public int ReviewsCount { get; set; }

        public double RatingAverage
        { get; set;
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

        public PaginationResult<ReviewItem> Items { get; set; } 
    }
}
