using System.Collections;
using GlobalEvents;
using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    public class Fireworks : MonoBehaviour
    {
        [SerializeField] private SoFireworksData _soFireworksData;
        [SerializeField] private float _scaleChangeSpeed = 1.0f;
        
        public SoFireworksData soFireworksData => _soFireworksData;

      
        private void Start()
        {
            //StartTimeToEndFireworks();
           // StartSetScaleFireworks(); //Использовать, если спвним объект, а не, если он находится внутри префаба деда.
        }

        public void StopTimeToEndFireworks()
        {
            StopCoroutine(nameof(TimeDoEndFireworks));
        }
        
        public void StartTimeToEndFireworks()
        {
            StartCoroutine(TimeDoEndFireworks());
        }

        private void StartSetScaleFireworks()
        {
            Vector3 targetScale = new Vector3(soFireworksData.ScaleX, soFireworksData.ScaleY, transform.localScale.z);
            StartCoroutine(ScaleOverTime(targetScale));
            StartCoroutine(TimeDoEndFireworks());
        }

        private IEnumerator ScaleOverTime(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;

            float distance = Vector3.Distance(startScale, targetScale);

            while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
            {
                transform.localScale =
                    Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _scaleChangeSpeed);

                yield return null;
            }

            transform.localScale = targetScale;
        }

        //Время закинания.
        private IEnumerator TimeDoEndFireworks()
        {
            //StartFireworksEvent.SendStartFireworksEvent(); //Использовать при спавне.
            yield return new WaitForSeconds(soFireworksData.TimeToDie);
            StopFireworksEvent.SendStopFireworksEvent();
            //Destroy(gameObject); // Использовать, если спавним объект.
        }

        
    }
}