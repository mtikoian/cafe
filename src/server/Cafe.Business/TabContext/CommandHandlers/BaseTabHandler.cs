﻿using AutoMapper;
using Cafe.Core;
using Cafe.Domain;
using Cafe.Domain.Entities;
using Cafe.Domain.Events;
using Cafe.Persistance.EntityFramework;
using FluentValidation;
using Marten;
using Optional;
using Optional.Async;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cafe.Business.TabContext.CommandHandlers
{
    public class BaseTabHandler<TCommand> : BaseHandler<TCommand>
    {
        public BaseTabHandler(
            IValidator<TCommand> validator,
            ApplicationDbContext dbContext,
            IDocumentSession documentSession,
            IEventBus eventBus,
            IMapper mapper,
            IMenuItemsService menuItemsService)
            : base(validator, dbContext, documentSession, eventBus, mapper)
        {
            MenuItemsService = menuItemsService;
        }

        protected IMenuItemsService MenuItemsService { get; }

        protected Task<Tab> GetTabFromStore(Guid id, CancellationToken cancellationToken) =>
            Session.LoadAsync<Tab>(id, cancellationToken);

        protected Task<Option<Tab, Error>> GetTabIfExists(Guid id, CancellationToken cancellationToken) =>
            GetTabFromStore(id, cancellationToken)
                .SomeNotNull<Tab, Error>(Error.NotFound($"No tab with id {id} was found."));

        protected Task<Option<Tab, Error>> TabShouldNotBeClosed(Guid id, CancellationToken cancellationToken) =>
            GetTabIfExists(id, cancellationToken)
                .FilterAsync(async tab => tab.IsOpen, Error.Validation($"Tab {id} is closed."));

        protected Task<Option<Tab, Error>> TabShouldNotExist(Guid id, CancellationToken cancellationToken) =>
            GetTabFromStore(id, cancellationToken)
                .SomeWhen<Tab, Error>(t => t == null, Error.Conflict($"Tab {id} already exists."))
                .MapAsync(async _ => new Tab(id));

        protected Task<Option<Tab, Error>> TabShouldExist(Guid id, CancellationToken cancellationToken) =>
            GetTabFromStore(id, cancellationToken)
                .SomeWhen<Tab, Error>(t => t != null, Error.Conflict($"Tab {id} does not exist."))
                .MapAsync(async _ => new Tab(id));

        protected Task<Option<IList<MenuItem>, Error>> MenuItemsShouldExist(IList<int> menuItemNumbers) =>
            // Wrapping to improve readability
            MenuItemsService.ItemsShouldExist(menuItemNumbers);
    }
}
