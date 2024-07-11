using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttributeItem : MonoBehaviour
{
	[SerializeField] private Image _ico;
	[SerializeField] private TextMeshProUGUI _text;

	public void Init(Sprite ico, string text)
	{
		_ico.sprite = ico;
		_text.text = text;
	}
}
