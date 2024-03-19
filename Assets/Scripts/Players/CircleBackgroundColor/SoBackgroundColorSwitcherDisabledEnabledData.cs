using UnityEngine;

namespace Players.CircleBackgroundColor
{
    [CreateAssetMenu(menuName = "Create SoBackgroundColorSwitcherDisabledEnabledData ",
        fileName = "SoBackgroundColorSwitcherDisabledEnabledData")]
    public class SoBackgroundColorSwitcherDisabledEnabledData : ScriptableObject
    {
        [SerializeField] private float _switchInterval = 0.5f;
        public float SwitchInterval => _switchInterval;
    }
}