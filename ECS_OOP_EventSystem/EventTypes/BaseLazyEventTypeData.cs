using System;
using Unity.Entities;

namespace ECS_OOP_EventSystem {
    internal interface ILazyEventTypeData : IComponentData {
        public int CalcEventTypeInt();
    }
    /// <summary>
    /// Inherit this class to create custom lazy event data. <br/>
    /// Then, just define your event in <see cref="TEventType"/>
    /// </summary>
    /// <typeparam name="TEventType">Enum to define custom lazy event data</typeparam>
    public class BaseLazyEventTypeData<TEventType> : ILazyEventTypeData where TEventType : Enum {
        public TEventType eventType;
        public int CalcEventTypeInt() => Convert.ToInt32(eventType);
    }
}
