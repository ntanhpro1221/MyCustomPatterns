using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace ECS_OOP_EventSystem {
    [UpdateAfter(typeof(EventSystem))]
    partial struct PostEventFrom_ECS_To_OOP_System : ISystem {
        private EntityQuery receiveLazyEventRequestQuery;
        private EntityQuery receiveConcreteEventRequestQuery;

        private MethodInfo getComponentDataMethod;
        private const string GET_COMPONENT_DATA_METHOD_NAME = "GetComponentData";

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            receiveLazyEventRequestQuery = state.EntityManager.CreateEntityQuery(
                typeof(ReceiveEventRequest),
                typeof(LazyEventData));
            receiveConcreteEventRequestQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ReceiveEventRequest>()
                .WithNone<LazyEventData>()
                .Build(state.EntityManager);

            state.RequireAnyForUpdate(
                receiveLazyEventRequestQuery,
                receiveConcreteEventRequestQuery);

            TryGetMethodGetComponentData(out getComponentDataMethod);
        }

        public void OnUpdate(ref SystemState state) {
            NativeArray<LazyEventData> receiveLazyEventDataArr = receiveLazyEventRequestQuery.ToComponentDataArray<LazyEventData>(Allocator.Temp);
            NativeArray<Entity> receiveConcreteEventEntityArr = receiveLazyEventRequestQuery.ToEntityArray(Allocator.Temp);

            foreach (LazyEventData lazyEventData in receiveLazyEventDataArr)
                EventManager.Instance.PostEvent_OOP(lazyEventData.eventType);

            foreach (Entity receiveConcreteEventEntity in receiveConcreteEventEntityArr)
                if (TryGetEventDataFromEntity(receiveConcreteEventEntity, state.EntityManager, out IConcreteEventArgs data))
                    EventManager.Instance.PostEvent_OOP(data, this);
                else LogErrorIncorrectFormatEvent();
        }
        
        private bool TryGetMethodGetComponentData(out MethodInfo methodInfo) {
            methodInfo = typeof(EntityManager).GetMethod(GET_COMPONENT_DATA_METHOD_NAME);
            Assert.IsNotNull(methodInfo, $"Method name: {GET_COMPONENT_DATA_METHOD_NAME} is no longer exist in {nameof(EntityManager)}, you must check this!!!");
            return true;
        }

        private bool TryGetEventDataFromEntity<TEventData>(in Entity entity, in EntityManager entityManager, out TEventData data) where TEventData : IConcreteEventArgs {
            data = default;

            NativeArray<ComponentType> componentTypes = entityManager.GetComponentTypes(entity, Allocator.Temp);
            if (componentTypes.Length != 2) return false;

            foreach (ComponentType componentType in componentTypes) {
                if (componentType == (ComponentType)typeof(ReceiveEventRequest)) continue;

                Type type = componentType.GetManagedType();
                if (type == null) return false;
                if (!typeof(TEventData).IsAssignableFrom(type)) return false;
                data = componentType.IsManagedComponent
                    ? entityManager.GetComponentObject<TEventData>(entity, componentType)
                    : (TEventData)getComponentDataMethod.MakeGenericMethod(type).Invoke(entityManager, new object[] { entity });
            }

            return true;
        }
        
        private void LogErrorIncorrectFormatEvent() {
            Debug.LogError("event was posted in incorrect format, it will be skipped");
        }
    }
}
