using System;
using System.Collections;
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

		private SpriteRenderer _spriteRenderer;
		private Coroutine _coroutine;

		private void Awake()
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
			//StopBackgroundSwitcherEvent.OnStartStopBackgroundSwitcher.AddListener(StopSwitching);
			StopSwitching();
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
			if (!isRunning && isSwitching)
			{
				isSwitching = true;
				isRunning = true;

				//InvokeRepeating("SwitchObject", 0, _soSwitcher.SwitchInterval);
			}

			gameObject.SetActive(true);
			StartCoroutine(FadeSprite());
		}

		//Остановка включения и выключения компонента с красным фоном на объекте.
		public void StopSwitching()
		{
			isSwitching = false;
			isRunning = false;
			gameObject.SetActive(false);
			StopCoroutine(FadeSprite());
			//CancelInvoke("SwitchObject");
		}

		private IEnumerator FadeSprite()
		{
			while (true)
			{
				for (float t = 0f; t < 1; t += Time.deltaTime)
				{
					float normalizedTime = t / 1;
					float alpha = Mathf.Lerp(0.4f, 0f, normalizedTime);

					Color newColor = _spriteRenderer.color;
					newColor.a = alpha;
					_spriteRenderer.color = newColor;

					yield return null;
				}

				for (float t = 0f; t < 1; t += Time.deltaTime)
				{
					float normalizedTime = t / 1;
					float alpha = Mathf.Lerp(0f, 0.4f, normalizedTime);

					Color newColor = _spriteRenderer.color;
					newColor.a = alpha;
					_spriteRenderer.color = newColor;

					yield return null;
				}
			}
		}
	}
}