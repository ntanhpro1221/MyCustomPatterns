using Unity.Entities;

namespace ECS_OOP_EventSystem {
    /// <summary>
    /// Create managed or unmanaged event args inherit from this interface to create custom event.<br/>
    /// Two different event args do not necessarily have different field.
    /// </summary>
    public interface IConcreteEventArgs : IComponentData { }
}
