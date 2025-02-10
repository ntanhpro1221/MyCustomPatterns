using System;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace ECS_OOP_EventSystem {
    [UpdateAfter(typeof(EventSystem))]
    partial class PostEventFrom_ECS_To_OOP_System : SystemBase {
        private EntityQuery receiveEventRequestQuery;

        private MethodInfo getComponentDataMethod;
        private const string GET_COMPONENT_DATA_METHOD_NAME = "GetComponentData";
        private readonly Type[] GET_COMPONENT_DATA_METHOD_PARAMS = new[] { typeof(Entity) };

        protected override void OnCreate() {
            receiveEventRequestQuery = GetEntityQuery(typeof(ReceiveEventRequest));

            RequireForUpdate(receiveEventRequestQuery);

            GetGetMethodGetComponentData(out getComponentDataMethod);
        }

        protected override void OnUpdate() {
            NativeArray<Entity> receiveEventEntityArr = receiveEventRequestQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity receiveEventEntity in receiveEventEntityArr) {
                NativeArray<ComponentType> componentTypes = EntityManager.GetComponentTypes(receiveEventEntity, Allocator.Temp);

                if (componentTypes.Length != 2) {
                    LogErrorIncorrectFormatEvent();
                    continue;
                }

                ComponentType componentType = componentTypes[
                    componentTypes[0] == typeof(ReceiveEventRequest)
                    ? 1 : 0];
                Type type = componentType.GetManagedType();

                // CONCRETE EVENT DATA
                if (typeof(IConcreteEventTypeArgs).IsAssignableFrom(type))
                    EventManager.OnlyOOP.PostEvent_Concrete(
                        componentType.IsManagedComponent
                            ? EntityManager.GetComponentObject<IConcreteEventTypeArgs>(receiveEventEntity, componentType)
                            : (IConcreteEventTypeArgs)getComponentDataMethod.MakeGenericMethod(type).Invoke(EntityManager, new object[] { receiveEventEntity }),
                        this);
                // CUSTOM LAZY EVENT DATA
                else if (typeof(ILazyEventTypeData).IsAssignableFrom(type))
                    EventManager.OnlyOOP.LowLevel.PostEvent_Lazy(type, EntityManager.GetComponentObject<ILazyEventTypeData>(receiveEventEntity).CalcEventTypeInt());
                else LogErrorIncorrectFormatEvent();
            }
        }

        private void GetGetMethodGetComponentData(out MethodInfo methodInfo) {
            methodInfo = typeof(EntityManager).GetMethod(GET_COMPONENT_DATA_METHOD_NAME, GET_COMPONENT_DATA_METHOD_PARAMS);
            Assert.IsNotNull(methodInfo, 
                $"Method {GET_COMPONENT_DATA_METHOD_NAME}" +
                $"({string.Join(", ", GET_COMPONENT_DATA_METHOD_PARAMS.Select(type => type.Name))})" +
                $" is no longer exist in {typeof(EntityManager).Name}, you must fix this!!!");
        }

        private void LogErrorIncorrectFormatEvent() {
            Debug.LogError("event was posted in incorrect format, it will be skipped");
        }
    }
}
