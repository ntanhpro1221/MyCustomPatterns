using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ECS_OOP_EventSystem {
    /// <summary>
    /// Event System for ECS System.<br/>
    /// TODO: use generic abstract SystemBase to able to manual add more <see cref="LazyEventData"/>
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
    partial struct EventSystem : ISystem {
        private EntityQuery receiveEventRequestQuery;
        private EntityQuery sendEventRequestQuery;
        
        public void OnCreate(ref SystemState state) {
            receiveEventRequestQuery = state.EntityManager.CreateEntityQuery(
                typeof(ReceiveEventRequest));
            sendEventRequestQuery = state.EntityManager.CreateEntityQuery(
                typeof(SendEventRequest));

            state.RequireAnyForUpdate(
                receiveEventRequestQuery,
                sendEventRequestQuery);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            NativeArray<Entity> receiveEventEntityArr = receiveEventRequestQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Entity> sendEventEntityArr = sendEventRequestQuery.ToEntityArray(Allocator.Temp);

            // Destroy old event
            state.EntityManager.DestroyEntity(receiveEventEntityArr);

            // Post new event
            state.EntityManager.AddComponent(sendEventEntityArr, typeof(ReceiveEventRequest));
            state.EntityManager.RemoveComponent(sendEventEntityArr, typeof(SendEventRequest));
        }
    }
}
