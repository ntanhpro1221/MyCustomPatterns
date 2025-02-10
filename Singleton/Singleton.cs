using System.Reflection;
using UnityEngine;

namespace SingletonUtil {
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
        private static MethodBase m_OnTouchedMethod;
        private static T m_Instance;

        public static T Instance {
            get {
                m_Instance ??= FindFirstObjectByType<T>();
                m_OnTouchedMethod ??= typeof(T).GetMethod("OnTouched", BindingFlags.NonPublic | BindingFlags.Instance);
                if (m_Instance != null) m_OnTouchedMethod.Invoke(m_Instance, null);
                return m_Instance;
            }
        }

        protected virtual void OnTouched() { }

        protected virtual void Awake() => DontDestroyOnLoad(gameObject);
    }
}
