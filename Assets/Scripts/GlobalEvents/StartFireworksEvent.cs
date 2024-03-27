using UnityEngine;
using UnityEngine.Events;

namespace GlobalEvents
{
    public class StartFireworksEvent : MonoBehaviour
    {
        public static readonly UnityEvent OnStartFireworksEvent = new UnityEvent();

        public static void SendStartFireworksEvent()
        {
            OnStartFireworksEvent.Invoke();
        }
    }
}