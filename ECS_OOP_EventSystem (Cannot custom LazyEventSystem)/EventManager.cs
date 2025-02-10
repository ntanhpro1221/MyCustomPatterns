using SingletonUtil;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Assertions;

namespace ECS_OOP_EventSystem {
    /// <summary>
    /// Event System for OOP System
    /// </summary>
    public class EventManager : Singleton<EventManager> {
        public delegate void LazyEventHandler();
        public delegate void ConcreteEventHandler<TEventArgs>(IConcreteEventArgs eventArgs, object sender) where TEventArgs : IConcreteEventArgs;

        private readonly Dictionary<LazyEventType, LazyEventHandler> lazyEventDict = new();
        /// <summary>
        /// Key is automatic store in <see cref="Type"/> of <see cref="{TEventArgs}"/> of <see cref="ConcreteEventHandler{TEventArgs}"/>.<br/>
        /// You can use your custom event key when add or remove event listener and some function like that.
        /// </summary>
        private readonly ContravarianceDelegateDictionary<Type, ConcreteEventHandler<IConcreteEventArgs>> concreteEventDict = new();

        public void AddListener(LazyEventType eventType, LazyEventHandler callback) {
            if (callback == null) return;
            if (!lazyEventDict.ContainsKey(eventType))
                lazyEventDict.Add(eventType, null);

            lazyEventDict[eventType] += callback;
        }

        public void AddListener<TEventArgs>(ConcreteEventHandler<TEventArgs> callback) where TEventArgs : IConcreteEventArgs {
            Type eventType = typeof(TEventArgs);

            if (callback == null) return;
            if (!concreteEventDict.ContainsKey(eventType))
                concreteEventDict.Add(eventType, null);

            concreteEventDict.AddToKey(
                eventType, 
                callback, 
                dele => (eventArgs, sender) => ((ConcreteEventHandler<TEventArgs>)dele).Invoke(eventArgs, sender));
        }
        
        public void AddListener(ConcreteEventHandler<IConcreteEventArgs> callback, Type customEventType) {
            if (callback == null) return;
            Assert.IsNotNull(customEventType);
            if (!concreteEventDict.ContainsKey(customEventType))
                concreteEventDict.Add(customEventType, null);

            concreteEventDict.Dict[customEventType] += callback;
        }

        public void RemoveListener(LazyEventType eventType, LazyEventHandler callback) {
            if (callback == null) return;
            if (!lazyEventDict.ContainsKey(eventType)) return;

            lazyEventDict[eventType] -= callback;
            if (lazyEventDict[eventType] == null)
                lazyEventDict.Remove(eventType);
        }

        public void RemoveListener<TEventArgs>(ConcreteEventHandler<TEventArgs> callback) where TEventArgs : IConcreteEventArgs {
            Type eventType = typeof(TEventArgs);

            if (callback == null) return;
            if (!concreteEventDict.ContainsKey(eventType)) return;

            concreteEventDict.RemoveFromKey(
                eventType, 
                callback);
        }

        public void RemoveListener(ConcreteEventHandler<IConcreteEventArgs> callback, Type customEventType) {
            if (callback == null) return;
            Assert.IsNotNull(customEventType);
            if (!concreteEventDict.ContainsKey(customEventType)) return;

            concreteEventDict.Dict[customEventType] -= callback;
        }

        public void PostEvent_OOP(LazyEventType eventType) {
            if (lazyEventDict.ContainsKey(eventType))
                lazyEventDict[eventType]?.Invoke();
        }

        public void PostEvent_OOP(IConcreteEventArgs eventArgs, object sender) {
            Type eventType = eventArgs.GetType();
            if (concreteEventDict.ContainsKey(eventType))
                concreteEventDict[eventType]?.Invoke(eventArgs, sender);
        }
        
        public void PostEvent_ECS(LazyEventType eventType, World world) {
            Assert.IsNotNull(world);
            PostEvent_ECS(new LazyEventData { eventType = eventType }, world);
        }

        public void PostEvent_ECS<TEventArgs>(TEventArgs eventArgs, World world) where TEventArgs : unmanaged, IConcreteEventArgs {
            Assert.IsNotNull(world);

            Entity entity = world.EntityManager.CreateEntity();
            world.EntityManager.AddComponentData(entity, eventArgs);
        }

        public void PostEvent_ECS_Managed<TEventArgs>(TEventArgs eventArgs, World world) where TEventArgs : class, IConcreteEventArgs, new() {
            Assert.IsNotNull(world);

            Entity entity = world.EntityManager.CreateEntity();
            world.EntityManager.AddComponentData(entity, eventArgs);
        }

        public void PostEvent_OOP_ECS(LazyEventType eventType, World world) {
            Assert.IsNotNull(world);

            PostEvent_OOP(eventType);
            
            PostEvent_ECS(eventType, world);
        }

        public void PostEvent_OOP_ECS<TEventArgs>(TEventArgs eventArgs, object sender, World world) where TEventArgs : unmanaged, IConcreteEventArgs {
            Assert.IsNotNull(world);

            PostEvent_OOP(eventArgs, sender);

            PostEvent_ECS(eventArgs, world);
        }

        public void PostEvent_OOP_ECS_Managed<TEventArgs>(TEventArgs eventArgs, object sender, World world) where TEventArgs : class, IConcreteEventArgs, new() {
            Assert.IsNotNull(world);

            PostEvent_OOP(eventArgs, sender);

            PostEvent_ECS_Managed(eventArgs, world);
        }
    }
}
