using DG.Tweening;
using Pathfinding.Serialization;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StateIcons : MonoBehaviour
{
    [SerializeField] private Image _stun;
    [SerializeField] private TextMeshProUGUI _stunText;
    [SerializeField] private Image _frozen;
	[SerializeField] private TextMeshProUGUI _frozenText;
	[SerializeField] private Image _frosting;
	[SerializeField] private TextMeshProUGUI _frostingText;
	[SerializeField] private Image _blind;
    [SerializeField] private TextMeshProUGUI _blindText;
	private void Start()
	{
		_stun.gameObject.SetActive(false);
        _frozen.gameObject.SetActive(false);
        _frosting.gameObject.SetActive(false);
        _blind.gameObject.SetActive(false);
	}
	private void Update()
	{
		if(Input.GetKeyUp(KeyCode.R))
        {
            ActivateIco(States.Stun, 2, 1);
        }
		if (Input.GetKeyUp(KeyCode.E))
		{
			ActivateIco(States.Frosting, .2f, 2);
		}
		if (Input.GetKeyUp(KeyCode.W))
		{
			ActivateIco(States.Stun, 2, 2);
		}
	}
	public void ActivateIco(States state, float timeToDecrease, int stack)
    {
        switch (state)
        {
            case States.Stun:
                AnimateIco(_stun, timeToDecrease, stack);
                break;
            case States.Frozen:
                AnimateIco(_frozen, timeToDecrease, stack);
                break;
            case States.Frosting:
                AnimateIco(_frosting, timeToDecrease, stack);
                break;
            case States.Blind:
                AnimateIco(_blind, timeToDecrease, stack);
                break;
            default:
                break;
        }
    }

    private void AnimateIco(Image ico, float time, int stack)
    {
        ico.gameObject.SetActive(true);
        ico.fillAmount = 1;
        if (stack <= 1)
        {
            ico.DOFillAmount(0, time).OnComplete(() => ico.gameObject.SetActive(false));
        }
        else
        {
			ico.DOFillAmount(0, time).OnComplete(() => AnimateIco(ico, time, --stack));
		}
    }
}

public enum States
{
    Stun,
    Frozen, 
    Frosting,
    Blind
}

