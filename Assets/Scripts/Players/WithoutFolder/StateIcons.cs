using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StateIcons : MonoBehaviour
{
   /* [SerializeField] private StateIcoItem _stun;
    [SerializeField] private StateIcoItem _frozen;
    [SerializeField] private StateIcoItem _frosting;
    [SerializeField] private StateIcoItem _blind;*/
    [SerializeField] private GameObject _spawnPos;

    [SerializeField] private List<StateIcoItem> _icons;
    private List<StateIcoItem> _activeEffects = new List<StateIcoItem>();
    private bool _added = false;

	/*private void Update()
	{
        //for test

		if(Input.GetKeyUp(KeyCode.R))
        {
            ActivateIco(States.Stun, 2, 1);
        }
		if (Input.GetKeyUp(KeyCode.E))
		{
			ActivateIco(States.Blind, 2f, 2);
		}
		if (Input.GetKeyUp(KeyCode.W))
		{
			ActivateIco(States.Stun, 6, 1);
		}
		if (Input.GetKeyUp(KeyCode.T))
		{
			ActivateIco(States.Frozen, 2, 4);
		}
		if (Input.GetKeyUp(KeyCode.Y))
		{
			ActivateIco(States.Frosting, 2, 6);
		}
	}*/
	public void ActivateIco(States state, float timeToDecrease, int stack)
    {
        foreach(var ico in _activeEffects)//if we already have the same ico
        {
            if(ico.state == state)
            {
                ico.count+= stack;
                ico.time.Add(timeToDecrease);
				ico.Text.text = ico.count.ToString();
				ico.Text.gameObject.SetActive(true);
				//AnimateIco(ico);
				return;
            }
        }
        foreach(var ico in _icons) //instatiating new ico
        {
            if(ico.state == state)
            {
                var newIco = Instantiate(ico, _spawnPos.transform);
                newIco.time.Add(timeToDecrease);
                newIco.count = stack;
                _activeEffects.Add(newIco);
                AnimateIco(newIco);
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

    private void AnimateIco(StateIcoItem icoItem)
    {
        Image ico = icoItem.FadeFront;
        ico.fillAmount = 0;
        if (icoItem.count == 1)
        {
			icoItem.Text.gameObject.SetActive(false);
            icoItem.count--;
			ico.DOFillAmount(1, icoItem.time[0]).SetEase(Ease.Linear).OnComplete(() => RemoveItem(icoItem));
			icoItem.time.Remove(icoItem.time[0]);
		}
        else
        {
            icoItem.Text.gameObject.SetActive(true);
			icoItem.Text.text = icoItem.count.ToString();
            icoItem.count--;
			ico.DOFillAmount(1, icoItem.time[0]).SetEase(Ease.Linear).OnComplete(() => AnimateIco(icoItem));
            icoItem.time.Remove(icoItem.time[0]);
		}
    }

    private void RemoveItem(StateIcoItem icoItem)
    {
        if (icoItem.count > 0)
        {
            AnimateIco(icoItem);
            return;
        }
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

