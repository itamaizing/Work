using UnityEngine;
using UnityEngine.Events;

namespace GlobalEvents
{
    public class StopFireworksEvent:MonoBehaviour
    {
        public static readonly UnityEvent OnStopFireworksEvent = new UnityEvent();

        public static void SendStopFireworksEvent()
        {
            OnStopFireworksEvent.Invoke();
        }
    }
}