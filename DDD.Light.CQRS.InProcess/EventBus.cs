﻿using System;
using System.Linq;
using System.Reflection;
using DDD.Light.CQRS.Contracts;
using DDD.Light.EventStore.Contracts;

namespace DDD.Light.CQRS.InProcess
{
    public class EventBus : IEventBus
    {
        private static volatile IEventBus _instance;
        private static object token = new Object();
        private IEventStore _eventStore;
        private IEventSerializationStrategy _eventSerializationStrategy;
        private bool _checkLatestEventTimestampPriorToSavingToEventStore;

        public static IEventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (token)
                    {
                        if (_instance == null)
                            _instance = new EventBus();
                    }
                }
                return _instance;
            }
        }

        private EventBus(){}

        public void Subscribe<T>(IEventHandler<T> handler)
        {
            EventHandlersDatabase<T>.Instance.Add(handler);
        }

        public void Subscribe<T>(Action<T> handleMethod)
        {
            EventHandlersDatabase<T>.Instance.Add(handleMethod);
        }

        public void Publish<T>(Type aggregateType, Guid aggregateId, T @event)
        {
            StoreEvent(aggregateType, aggregateId, @event);
            HandleEvent(@event);
        }

        public void Publish<TAggregate, T>(Guid aggregateId, T @event)
        {
            StoreEvent(typeof(TAggregate), aggregateId, @event);
            HandleEvent(@event);
        }

        //todo: make reason behind checkLatestEventTimestampPriorToSavingToEventStore less ambiguious
        public void Configure(IEventStore eventStore, IEventSerializationStrategy eventSerializationStrategy, bool checkLatestEventTimestampPriorToSavingToEventStore)
        {
            _eventStore = eventStore;
            _eventSerializationStrategy = eventSerializationStrategy;
            _checkLatestEventTimestampPriorToSavingToEventStore = checkLatestEventTimestampPriorToSavingToEventStore;
        }

        private void HandleEvent<T>(T @event)
        {
            try
            {
                if (!Equals(@event, default(T)))
                    new Transaction<T>(@event, EventHandlersDatabase<T>.Instance.Get().ToList()).Commit();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Transaction<T>(@event, EventHandlersDatabase<T>.Instance.Get().ToList()).Commit() failed");
            }
        }

        private void StoreEvent<T>(Type aggregateType, Guid aggregateId, T @event)
        {
            if (_eventStore == null) throw new ApplicationException("Event Store is not configured. Use 'EventBus.Instance.Configure(eventStore, eventSerializationStrategy);' to configure it.");
            try
            {
                if (_checkLatestEventTimestampPriorToSavingToEventStore)
                {
                    //todo: in try catch
                    var latestCreatedOnInEventStore = _eventStore.LatestEventTimestamp(aggregateId);
                    if (DateTime.Compare(DateTime.UtcNow, latestCreatedOnInEventStore) < 0)
                        //earlier than in event store
                    {
                        Publish(GetType(), aggregateId, new AggregateCacheCleared(aggregateId, aggregateType));
                    }
                }
                _eventStore.Save(new AggregateEvent
                    {
                        Id = Guid.NewGuid(),
                        AggregateId = aggregateId,
                        AggregateType = aggregateType.AssemblyQualifiedName,
                        EventType = typeof (T).AssemblyQualifiedName,
                        CreatedOn = DateTime.UtcNow,
                        SerializedEvent = _eventSerializationStrategy.SerializeEvent(@event)
                    });
            }
            catch (Exception ex)
            {
                throw new ApplicationException("DDD.Light.CQRS.InProcess.EventBus -> StoreEvent<T>: Saving to event store failed", ex);
            }
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        public void RestoreReadModel()
        {
            _eventStore.GetAll().ToList().ForEach(HandleRestoreReadModelEvent);
        }

        public void RestoreReadModel(DateTime until)
        {
            _eventStore.GetAll(until).ToList().ForEach(HandleRestoreReadModelEvent);
        }

        private void HandleRestoreReadModelEvent(AggregateEvent aggregateEvent)
        {
            var eventType = Type.GetType(aggregateEvent.EventType);
            var @event = _eventSerializationStrategy.DeserializeEvent(aggregateEvent.SerializedEvent, eventType);
            GetType().GetMethod("HandleEvent", BindingFlags.NonPublic | BindingFlags.Instance)
                     .MakeGenericMethod(eventType)
                     .Invoke(Instance, new[] {@event});
        }

    }
}
