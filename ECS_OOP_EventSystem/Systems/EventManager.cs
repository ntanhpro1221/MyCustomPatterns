using SingletonUtil;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace ECS_OOP_EventSystem {
    /// <summary>
    /// Event System for OOP System
    /// </summary>
    public class EventManager : Singleton<EventManager> {
        #region EVENT DELEGATE
        public delegate void LazyEventHandler();
        public delegate void ConcreteEventHandler<TEventArgs>(IConcreteEventTypeArgs eventArgs, object sender) where TEventArgs : IConcreteEventTypeArgs;
        #endregion

        #region DICTIONARY STUFF
        /// <summary>
        /// Key is automatic store in <see cref="Type"/> of <see cref="BaseLazyEventTypeData{TEventType}"/>.<br/>
        /// </summary>
        private readonly Dictionary<Type, Dictionary<int, LazyEventHandler>> lazyEventDict = new();
        /// <summary>
        /// Key is automatic store in <see cref="Type"/> of <see cref="{TEventTypeArgs}"/> of <see cref="ConcreteEventHandler{TEventTypeArgs}"/>.<br/>
        /// You can use your custom event key when add or remove event listener and some function like that.
        /// </summary>
        private readonly ContravarianceDelegateDictionary<Type, ConcreteEventHandler<IConcreteEventTypeArgs>> concreteEventDict = new();

        private static bool Check_LazyEventDict_KeyExist(Type groupType, int eventType) {
            if (!Instance.lazyEventDict.ContainsKey(groupType)) return false;
            if (!Instance.lazyEventDict[groupType].ContainsKey(eventType)) return false;
            return true;
        }

        private static void Ensure_LazyEventDict_KeyExist(Type groupType, int eventType) {
            if (!Instance.lazyEventDict.ContainsKey(groupType))
                Instance.lazyEventDict.Add(groupType, new());
            if (!Instance.lazyEventDict[groupType].ContainsKey(eventType))
                Instance.lazyEventDict[groupType].Add(eventType, null);
        }

        private static bool Check_ConcreteEventDict_KeyExist(Type eventType) {
            if (!Instance.lazyEventDict.ContainsKey(eventType)) return false;
            return true;
        }

        private static void Ensure_ConcreteEventDict_KeyExist(Type eventType) {
            if (!Instance.concreteEventDict.ContainsKey(eventType))
                Instance.concreteEventDict.Add(eventType, null);
        }
        #endregion

        #region POST EVENT IN BOTH OOP_ECS System
        public static void PostEvent_Lazy<TEventType>(BaseLazyEventTypeData<TEventType> eventData, in EntityManager entityManager)
            where TEventType : Enum {
            OnlyOOP.PostEvent_Lazy(eventData);
            OnlyECS.PostEvent_Managed(eventData, entityManager);
        }

        public static void PostEvent_Concrete<TEventArgs>(TEventArgs eventArgs, object sender, in EntityManager entityManager)
            where TEventArgs : unmanaged, IConcreteEventTypeArgs {
            OnlyOOP.PostEvent_Concrete(eventArgs, sender);
            OnlyECS.PostEvent_Unmanaged(eventArgs, entityManager);
        }

        public static void PostEvent_Concrete_Managed<TEventArgs>(TEventArgs eventArgs, object sender, in EntityManager entityManager)
            where TEventArgs : class, IConcreteEventTypeArgs, new() {
            OnlyOOP.PostEvent_Concrete(eventArgs, sender);
            OnlyECS.PostEvent_Managed(eventArgs, entityManager);
        }
        #endregion

        public static class OnlyOOP {
            #region ADD LISTENER
            public static void AddListener_Lazy<TEventType>(BaseLazyEventTypeData<TEventType> eventTypeData, LazyEventHandler callback) where TEventType : Enum
                => LowLevel.AddListener_Lazy(eventTypeData.GetType(), eventTypeData.CalcEventTypeInt(), callback);

            public static void AddListener_Concrete<TEventTypeArgs>(ConcreteEventHandler<TEventTypeArgs> callback) where TEventTypeArgs : IConcreteEventTypeArgs
                => LowLevel.AddListener_Concrete(typeof(TEventTypeArgs), callback);

            public static void AddListener_Concrete(ConcreteEventHandler<IConcreteEventTypeArgs> callback, Type customEventType)
                => LowLevel.AddListener_Concrete(customEventType, callback);
            #endregion

            #region REMOVE LISTENER
            public static void RemoveListener_Lazy<TEventType>(BaseLazyEventTypeData<TEventType> eventTypeData, LazyEventHandler callback) where TEventType : Enum
                => LowLevel.RemoveListener_Lazy(eventTypeData.GetType(), eventTypeData.CalcEventTypeInt(), callback);

            public static void RemoveListener_Concrete<TEventArgs>(ConcreteEventHandler<TEventArgs> callback) where TEventArgs : IConcreteEventTypeArgs
                => LowLevel.RemoveListener_Concrete(typeof(TEventArgs), callback);

            public static void RemoveListener_Concrete(ConcreteEventHandler<IConcreteEventTypeArgs> callback, Type customEventType)
                => LowLevel.RemoveListener_Concrete(customEventType, callback);
            #endregion

            #region POST EVENT
            public static void PostEvent_Lazy<TEventType>(BaseLazyEventTypeData<TEventType> eventData) where TEventType : Enum
                => LowLevel.PostEvent_Lazy(eventData.GetType(), Convert.ToInt32(eventData.eventType));

            public static void PostEvent_Concrete<TEventArgs>(TEventArgs eventArgs, object sender) where TEventArgs : IConcreteEventTypeArgs
                => LowLevel.PostEvent_Concrete(typeof(TEventArgs), eventArgs, sender);

            public static void PostEvent_Concrete(IConcreteEventTypeArgs eventArgs, object sender, Type customEventType)
                => LowLevel.PostEvent_Concrete(customEventType, eventArgs, sender);
            #endregion

            internal static class LowLevel {
                #region ADD LISTENER
                public static void AddListener_Lazy(Type groupType, int eventTypeInt, LazyEventHandler callback) {
                    if (callback == null) return;

                    Ensure_LazyEventDict_KeyExist(groupType, eventTypeInt);

                    Instance.lazyEventDict[groupType][eventTypeInt] += callback;
                }

                public static void AddListener_Concrete<TEventArgs>(Type eventType, ConcreteEventHandler<TEventArgs> callback) where TEventArgs : IConcreteEventTypeArgs {
                    if (callback == null) return;

                    Ensure_ConcreteEventDict_KeyExist(eventType);

                    Instance.concreteEventDict.AddToKey(
                        eventType,
                        callback,
                        dele => (eventArgs, sender) => ((ConcreteEventHandler<TEventArgs>)dele).Invoke(eventArgs, sender));
                }
                #endregion

                #region REMOVE LISTENER
                public static void RemoveListener_Lazy(Type groupType, int eventTypeInt, LazyEventHandler callback) {
                    if (callback == null) return;

                    if (!Check_LazyEventDict_KeyExist(groupType, eventTypeInt)) return;

                    Instance.lazyEventDict[groupType][eventTypeInt] -= callback;
                }

                public static void RemoveListener_Concrete<TEventArgs>(Type eventType, ConcreteEventHandler<TEventArgs> callback) where TEventArgs : IConcreteEventTypeArgs {
                    if (callback == null) return;

                    if (!Check_ConcreteEventDict_KeyExist(eventType)) return;

                    Instance.concreteEventDict.RemoveFromKey(eventType, callback);
                }
                #endregion

                #region POST EVENT
                public static void PostEvent_Lazy(Type eventGroup, int eventTypeInt) {
                    if (Check_LazyEventDict_KeyExist(eventGroup, eventTypeInt))
                        Instance.lazyEventDict[eventGroup][eventTypeInt]?.Invoke();
                }

                public static void PostEvent_Concrete(Type eventType, IConcreteEventTypeArgs eventArgs, object sender) {
                    if (Check_ConcreteEventDict_KeyExist(eventType))
                        Instance.concreteEventDict[eventType]?.Invoke(eventArgs, sender);
                }
                #endregion
            }
        }

        private static class OnlyECS {
            public static void PostEvent_Unmanaged<TEventData>(in TEventData eventData, in EntityManager entityManager) where TEventData : unmanaged, IComponentData
                => LowLevel.PostEvent_Unmanaged(eventData, entityManager);
            public static void PostEvent_Managed<TEventData>(in TEventData eventData, in EntityManager entityManager) where TEventData : class, IComponentData, new()
                => LowLevel.PostEvent_Managed(eventData, entityManager);

            internal static class LowLevel {
                public static void PostEvent_Unmanaged<TEventData>(in TEventData eventData, in EntityManager entityManager) where TEventData : unmanaged, IComponentData
                    => entityManager.AddComponentData(entityManager.CreateEntity(), eventData);
                public static void PostEvent_Managed<TEventData>(in TEventData eventData, in EntityManager entityManager) where TEventData : class, IComponentData, new()
                    => entityManager.AddComponentData(entityManager.CreateEntity(), eventData);
            }
        }
    }
}
