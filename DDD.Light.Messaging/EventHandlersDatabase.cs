﻿using System;
using System.Collections.Generic;

namespace DDD.Light.Messaging.InProcess
{
    public class EventHandlersDatabase<T> : IEventHandlersDatabase<T>
    {
        private static volatile IEventHandlersDatabase<T> _instance;
        private static object token = new Object();
        private readonly List<Action<T>> _registeredHandlerActions;

        public static IEventHandlersDatabase<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (token)
                    {
                        if (_instance == null)
                            _instance = new EventHandlersDatabase<T>();
                    }
                }
                return _instance;
            }
        }

        private EventHandlersDatabase()
        {
            _registeredHandlerActions = new List<Action<T>>();
        }

        public void Add(IEventHandler<T> eventHandler)
        {
            _registeredHandlerActions.Add(eventHandler.Handle);
        }

        public void Add(Action<T> eventHandlerAction)
        {
            _registeredHandlerActions.Add(eventHandlerAction);
        }

        public IEnumerable<Action<T>> Get()
        {
            return _registeredHandlerActions;
        }

    }
}