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

	public Material mat;

	public Image Ico => _ico;
	public Button Plus => _plus;
	public Button Minus => _minus;


	private float _value;
	public void Init(Sprite ico, float value)
	{
	//	_ico.sprite = ico;
		_value = value;
		_text.text = value.ToString();

	//	_plus.onClick.AddListener(Add);
	//	_minus.onClick.AddListener(Remove);
	}

	public void Add()
	{
		_value *= 1.01f;
		_text.text = _value.ToString();
	}
	public void Remove()
	{
		_value /= 1.01f;
		_text.text = _value.ToString();
	}
}
