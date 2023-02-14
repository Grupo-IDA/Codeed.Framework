﻿using Codeed.Framework.Domain;
using Codeed.Framework.Tenant;
using Microsoft.Extensions.DependencyInjection;

namespace Codeed.Framework.EventBus
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly IEventBusSubscriptionsManager _eventBusSubscriptionsManager;
        private readonly IServiceCollection _serviceCollection;
        private readonly ITenantService _tenantService;

        public InMemoryEventBus(
            IServiceCollection serviceCollection, 
            IEventBusSubscriptionsManager eventBusSubscriptionsManager,
            ITenantService tenantService)
        {
            _eventBusSubscriptionsManager = eventBusSubscriptionsManager;
            _serviceCollection = serviceCollection;
            _tenantService = tenantService;
        }

        public async Task Publish<TEvent>(TEvent @event)
            where TEvent : Event
        {
            var eventName = _eventBusSubscriptionsManager.GetEventKey(@event);
            if (!_eventBusSubscriptionsManager.HasSubscriptionsForEvent(eventName))
            {
                return;
            }

            var handlers = _eventBusSubscriptionsManager.GetHandlersForEvent(eventName);
            var tenant = _tenantService.Tenant;

            foreach (var subscription in handlers)
            {
                var serviceProvider = _serviceCollection.BuildServiceProvider();
                using (var serviceScope = serviceProvider.CreateScope())
                {
                    if (@event is ITenantEvent tenantEvent)
                    {
                        tenantEvent.Tenant = tenant;
                        var tenantServiceScope = serviceScope.ServiceProvider.GetRequiredService<ITenantService>();
                        tenantServiceScope.SetTenant(tenant);
                    }

                    var handler = ActivatorUtilities.CreateInstance(serviceProvider, subscription.HandlerType);
                    if (handler == null)
                    {
                        continue;
                    }

                    var eventType = _eventBusSubscriptionsManager.GetEventTypeByName(eventName);
                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                    await Task.Yield();
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                }
            }
        }

        public void Subscribe<TEvent, TEventHandler>()
            where TEvent : Event
            where TEventHandler : IEventHandler<TEvent>
        {
            _eventBusSubscriptionsManager.AddSubscription<TEvent, TEventHandler>();
        }
    }
}
