using UnityEngine;
using UnityEngine.Events;

namespace GlobalEvents
{
    public class SwitcherColorEvent:MonoBehaviour
    {
        public static readonly UnityEvent OnStartSwitcherColorRed = new UnityEvent();
        public static readonly UnityEvent OnStartSwitcherColorGreen = new UnityEvent();

        public static void SendStartSwitcherColorRed()
        {
            OnStartSwitcherColorRed.Invoke();
        }
        public static void SendStartSwitcherColorGreen()
        {
            OnStartSwitcherColorGreen.Invoke();
        }
    }
}