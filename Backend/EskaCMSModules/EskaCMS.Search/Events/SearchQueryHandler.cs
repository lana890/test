using EskaCMS.Core.Events;
using EskaCMS.Entities.Entity.Search;
using EskaCMS.Infrastructure.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.Search.Events
{
    public class SearchQueryHandler : INotificationHandler<SearchQuery>
    {
        private readonly IRepository<Query> _queryRepository;

        public SearchQueryHandler(IRepository<Query> queryRepository)
        {
            _queryRepository = queryRepository;
        }

        public async Task Handle(SearchQuery notification, CancellationToken cancellationToken)
        {

            if (!string.IsNullOrEmpty(notification.SearchText))
            {
                var searchQueryToBeAdded = new Query
                {
                    QueryText = notification.SearchText,
                    ResultsCount = notification.ResultCount,
                    SiteId=notification.SiteId,
                    CreatedOn = DateTimeOffset.Now
                };

                _queryRepository.Add(searchQueryToBeAdded);
                await _queryRepository.SaveChangesAsync();

            }



        }
    }
}
