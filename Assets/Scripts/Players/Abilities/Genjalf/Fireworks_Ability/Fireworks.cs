using System.Collections;
using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    public class Fireworks : MonoBehaviour
    {
        [SerializeField] private SoFireworksData _soFireworksData;
        [SerializeField] private float _scaleChangeSpeed = 1.0f;

        private void Start()
        {
            StartSetScaleFireworks();
        }

        private void StartSetScaleFireworks()
        {
            // Вычисляем конечный размер, используя данные из SoFireworksData
            Vector3 targetScale = new Vector3(_soFireworksData.ScaleX, _soFireworksData.ScaleY, transform.localScale.z);
            
            // Запускаем корутину для плавного изменения масштаба
            StartCoroutine(ScaleOverTime(targetScale));
        }

        private IEnumerator ScaleOverTime(Vector3 targetScale)
        {
            // Начальный размер объекта
            Vector3 startScale = transform.localScale;

            // Вычисляем расстояние до цели
            float distance = Vector3.Distance(startScale, targetScale);
            
            while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _scaleChangeSpeed);
                
                yield return null;
            }
            
            transform.localScale = targetScale;
        }
    }
}