using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttributeItem : MonoBehaviour
{
	[SerializeField] private Image _ico;
	[SerializeField] private TextMeshProUGUI _text;
	[SerializeField] private Button _plus;
	[SerializeField] private Button _minus;

	private float _value;
	public void Init(Sprite ico, float value)
	{
		_ico.sprite = ico;
		_value = value;
		_text.text = value.ToString();
	}

	public void Add(float value)
	{
		_value += value;
		_text.text = _value.ToString();
	}
}
