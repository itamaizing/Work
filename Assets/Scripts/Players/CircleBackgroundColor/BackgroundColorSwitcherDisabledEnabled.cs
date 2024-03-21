using System;
using GlobalEvents;
using UnityEngine;

namespace Players.CircleBackgroundColor
{
    public class BackgroundColorSwitcherDisabledEnabled : MonoBehaviour
    {
        [SerializeField] private SoBackgroundColorSwitcherDisabledEnabledData _soSwitcher;
        
        
        private bool isObjectActive = true;
        private bool isSwitching = true;
        private bool isRunning = false;

        private void Awake()
        {
            StopBackgroundSwitcherEvent.OnStartStopBackgroundSwitcher.AddListener(StopSwitching);
        }

        private void SwitchObject()
        {
            if (isSwitching)
            {
                isObjectActive = !isObjectActive;
                gameObject.SetActive(isObjectActive);
            }
        }

        //Запуск включения и выключения компонента с красным фоном на объекте.
        public void StartSwitching()
        {
            if (!isRunning&& isSwitching)
            {
                isSwitching = true;
                isRunning = true;
                InvokeRepeating("SwitchObject", 0, _soSwitcher.SwitchInterval);
                Debug.Log("Репитинг работает");
            }
        }

        //Остановка включения и выключения компонента с красным фоном на объекте.
        public void StopSwitching()
        {
            isSwitching = false;
            isRunning = false;
            CancelInvoke("SwitchObject");
            Debug.Log("Репитинг не работает");
        }
    }
}