﻿using Cafe.Core;
using Cafe.Core.TabContext.Queries;
using Cafe.Domain;
using Cafe.Domain.Views;
using Marten;
using Optional;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cafe.Business.TabContext.QueryHandlers
{
    public class GetAllOpenTabsHandler : IQueryHandler<GetAllOpenTabs, IList<TabView>>
    {
        private readonly IDocumentSession _session;

        public GetAllOpenTabsHandler(IDocumentSession session)
        {
            _session = session;
        }

        public async Task<Option<IList<TabView>, Error>> Handle(GetAllOpenTabs request, CancellationToken cancellationToken)
        {
            var tabs = (IList<TabView>)await _session
                .Query<TabView>()
                .Where(t => t.IsOpen)
                .ToListAsync();

            return tabs.Some<IList<TabView>, Error>();
        }
    }
}
