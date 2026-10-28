using MediatR;

namespace EskaCMS.Core.Events
{
    public class ReviewSummaryChanged : INotification
    {
        public long? ProductId { get; set; }

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }
    }
}
