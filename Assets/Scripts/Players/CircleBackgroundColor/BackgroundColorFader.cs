using System.Collections;
using UnityEngine;

namespace Players.CircleBackgroundColor
{
    public class BackgroundColorFader : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private float delayBetweenFades = 0;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void StartFadeSprite()
        {
            StartCoroutine(FadeSprite());
        }

        public void StopFadeSprite()
        {
            StopCoroutine(FadeSprite());
        }

        private IEnumerator FadeSprite()
        {
            Color startColor = _spriteRenderer.color;

            while (true)
            {
                float startTime = Time.time;
                while (Time.time - startTime < fadeDuration)
                {
                    float normalizedTime = (Time.time - startTime) / fadeDuration;
                    Color newColor = startColor;
                    newColor.a = Mathf.Lerp(startColor.a, 0f, normalizedTime);
                    _spriteRenderer.color = newColor;
                    yield return null;
                }

                _spriteRenderer.color = startColor;
                yield return new WaitForSeconds(delayBetweenFades);

                startTime = Time.time;
                while (Time.time - startTime < fadeDuration)
                {
                    float normalizedTime = (Time.time - startTime) / fadeDuration;
                    Color newColor = startColor;
                    newColor.a = Mathf.Lerp(0f, startColor.a, normalizedTime);
                    _spriteRenderer.color = newColor;
                    yield return null;
                }

                _spriteRenderer.color = startColor;
            }
        }
    }
}