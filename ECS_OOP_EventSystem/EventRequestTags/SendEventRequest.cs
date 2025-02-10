using Unity.Entities;

namespace ECS_OOP_EventSystem {
    /// <summary>
    /// Create entity with this and exactly one more component to send event request
    /// </summary>
    public struct SendEventRequest : IComponentData { }
}
