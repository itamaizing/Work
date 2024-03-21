using UnityEngine;
using UnityEngine.Events;

namespace GlobalEvents
{
    public class StopBackgroundSwitcherEvent:MonoBehaviour
    {
        public static readonly UnityEvent OnStartStopBackgroundSwitcher = new UnityEvent();

        public static void SendStartStopBackgroundSwitcher()
        {
            OnStartStopBackgroundSwitcher.Invoke();
        }
    }
}