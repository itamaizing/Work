using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using System;

public class TalentButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public Image ico;
	public Image border;
	public Image backLight;
	public TextMeshProUGUI talentName;
	public TextMeshProUGUI talentDescription;
	public Button button;
	public Sprite[] borders;
	public GameObject description;
	public bool isActive = false;

	private Material _mat;

	public void Init(Sprite ico, string name, string descriprion)
	{
		this.ico.sprite = ico;
		talentName.text = name;
		talentDescription.text = descriprion;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		backLight.DOFade(1, 0.2f);
		talentName.DOFade(1, 0.2f);
		description.transform.DOScale(1, 0.2f);
		Debug.Log("TEST On mouse enter");
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		backLight.DOFade(0, 0.2f);
		talentName.DOFade(0, 0.2f);
		description.transform.DOScale(0, 0.2f);
		Debug.Log("Mouse exit");
	}

	public void SwitchBorders(bool active)
	{
		if (borders.Length >= 2)
		{
			Debug.Log(active);
			_mat = Instantiate(ico.material);
			ico.material = _mat;
			_mat.SetFloat("_GrayscaleAmount", active ? 0 : 1);
			border.sprite = borders[active ? 0 : 1];
			
		}
	}
}
