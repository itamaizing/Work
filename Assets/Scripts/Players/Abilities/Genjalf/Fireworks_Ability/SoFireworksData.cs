using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    [CreateAssetMenu(menuName = "Create Fireworks Data",fileName = "Fireworks Data")]
    public class SoFireworksData:ScriptableObject
    {
        [SerializeField] private float _scaleX = 2f; //Scale в 2ед, это ширина клетки в одну единицу.

        public float ScaleX
        {
            get => _scaleX;
            set => _scaleX = value;
        }
    }
}