namespace ECS_OOP_EventSystem {
    /// <summary>
    /// Add this component to send event request for lazy event data (there is no papram of event).
    /// </summary>
    public class DefaultLazyEventTypeData : BaseLazyEventTypeData<DefaultLazyEventTypeData.EventType> {
        public enum EventType { 
            Nope,
        }
    }
}
