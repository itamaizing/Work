using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    public class Fireworks : MonoBehaviour
    {
        [SerializeField] private SoFireworksData _soFireworksData;

        private void Start()
        {
            StartSetScaleFireworks();
        }

        private void StartSetScaleFireworks()
        {
            transform.localScale =
                new Vector3(_soFireworksData.ScaleX, _soFireworksData.ScaleY, transform.localScale.z);
        }
    }
}