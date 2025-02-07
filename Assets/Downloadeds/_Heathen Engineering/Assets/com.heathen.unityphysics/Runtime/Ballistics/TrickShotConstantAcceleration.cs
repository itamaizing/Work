#if HE_SYSCORE
using System.Collections.Generic;
using UnityEngine;

namespace HeathenEngineering.UnityPhysics
{
    [RequireComponent(typeof(TrickShot))]
    public class TrickShotConstantAcceleration : MonoBehaviour
    {
        public List<Vector3> globalConstants = new(new Vector3[] { new(0, -9.81f, 0) });
        public List<Vector3> localConstants = new();

        private TrickShot ts;

        private void Start()
        {
            ts = GetComponent<TrickShot>();
        }

        private void LateUpdate()
        {
            Vector3 sum = Vector3.zero;
            foreach (var v in globalConstants)
                sum += v;
            foreach (var v in localConstants)
                sum += ts.transform.rotation * v;

            ts.constantAcceleration = sum;

            AdjustLengthBasedOnMouse();
        }

        private void AdjustLengthBasedOnMouse()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                Vector3 directionToMouse = (hit.point - ts.transform.position).normalized;

                float distanceToMouse = Vector3.Distance(ts.transform.position, hit.point);

                float scaleFactor = Mathf.Log10(distanceToMouse + 1f);

                Vector3 scaledAcceleration = Vector3.zero;
                foreach (var constant in globalConstants)
                {
                    scaledAcceleration += constant * scaleFactor;
                }

                ts.constantAcceleration = scaledAcceleration + CalculateLocalAcceleration();
            }
        }

        private Vector3 CalculateLocalAcceleration()
        {
            Vector3 localAcceleration = Vector3.zero;
            foreach (var constant in localConstants)
            {
                localAcceleration += ts.transform.rotation * constant;
            }
            return localAcceleration;
        }
    }
}
#endif
