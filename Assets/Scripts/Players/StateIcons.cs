using DG.Tweening;
using Pathfinding.Serialization;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StateIcons : MonoBehaviour
{
    [SerializeField] private StateIcoItem _stun;
    [SerializeField] private StateIcoItem _frozen;
    [SerializeField] private StateIcoItem _frosting;
    [SerializeField] private StateIcoItem _blind;
    [SerializeField] private GameObject _spawnPos;

    [SerializeField] private List<StateIcoItem> _icons;
    private List<StateIcoItem> _activeEffects = new List<StateIcoItem>();
    private bool _added = false;

	private void Update()
	{
        //for test

		if(Input.GetKeyUp(KeyCode.R))
        {
            ActivateIco(States.Stun, 2, 1);
        }
		if (Input.GetKeyUp(KeyCode.E))
		{
			ActivateIco(States.Blind, .2f, 2);
		}
		if (Input.GetKeyUp(KeyCode.W))
		{
			ActivateIco(States.Stun, 2, 2);
		}
	}
	public void ActivateIco(States state, float timeToDecrease, int stack)
    {
        foreach(var ico in _icons) 
        {
            if(ico.state == state)
            {
                var newIco = Instantiate(ico, _spawnPos.transform);
                _activeEffects.Add(newIco);
                AnimateIco(newIco, timeToDecrease, stack);
                _added = true;
            }
        }
        if (!_added)
        {
            Debug.Log("There is no stateIco " + state.ToString());
            _added = false;
        }
       /* switch (state)
        {
            case States.Stun:
                var stun = Instantiate(_stun, _spawnPos.transform);
                _activeEffects.Add(stun);
                AnimateIco(stun, timeToDecrease, stack);
                break;
            case States.Frozen:
				var frozen = Instantiate(_frozen, _spawnPos.transform);
                _activeEffects.Add(frozen);
				AnimateIco(frozen, timeToDecrease, stack);
                break;
            case States.Frosting:
				var frosting = Instantiate(_frozen, _spawnPos.transform);
                _activeEffects.Add(frosting);
				AnimateIco(frosting, timeToDecrease, stack);
                break;
            case States.Blind:
				var blind = Instantiate(_frozen, _spawnPos.transform);
                _activeEffects.Add(blind);
				AnimateIco(blind, timeToDecrease, stack);
                break;
            default:
                break;
        }*/
    }

    private void AnimateIco(StateIcoItem icoItem, float time, int stack)
    {
        Image ico = icoItem.Ico;
        ico.fillAmount = 1;
        if (stack == 1)
        {
			icoItem.Text.gameObject.SetActive(false);
			ico.DOFillAmount(0, time).OnComplete(() => RemoveItem(icoItem));
        }
        else
        {
            icoItem.Text.gameObject.SetActive(true);
			icoItem.Text.text = stack.ToString();
			ico.DOFillAmount(0, time).OnComplete(() => AnimateIco(icoItem, time, --stack));
		}
    }

    private void RemoveItem(StateIcoItem icoItem)
    {
        _activeEffects.Remove(icoItem);
        //yield return new WaitForSeconds(0.1f);
        Destroy(icoItem.gameObject);
    }

    //removing item before it ends
	public void RemoveItemByState(States state)
	{
		foreach(var item in _activeEffects)
        {
            if(item.state == state)
            {
				_activeEffects.Remove(item);
                Destroy(item.gameObject);
			}
        }
	}
}

public enum States
{
    Default,
    Stun,
    Frozen, 
    Frosting,
    Blind,
    Invisible
}

