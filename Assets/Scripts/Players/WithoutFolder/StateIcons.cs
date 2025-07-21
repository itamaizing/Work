using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
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
    public void ActivateIco(States state, float timeToDecrease, int stack, bool canStack, int maxStackValue = 1)
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            var ico = _activeEffects[i];
            if (ico.state == state)
            {
                ico.FadeFront.DOKill();

                if (canStack)
                {
                    ico.maxStack = maxStackValue;
                    ico.count = Mathf.Min(ico.count + stack, ico.maxStack);
                }
                else ico.count = 1;

                StartProgress(ico, timeToDecrease);
                RefreshText(ico);
                MoveIcoToEnd(i);
                return;
            }
        }

        foreach (var ico in _icons) //instatiating new ico
        {
            if (ico.state == state)
            {
                var newIco = Instantiate(ico, _spawnPos.transform);
                newIco.count = stack;
                newIco.maxStack = maxStackValue;

                StartProgress(newIco, timeToDecrease);
                RefreshText(newIco);

                _activeEffects.Add(newIco);
                MoveIcoToEnd(_activeEffects.Count - 1);
                return;
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

    private void StartProgress(StateIcoItem ico, float duration)
    {
        ico.currentDuration = duration;
        ico.FadeFront.DOKill();
        ico.FadeFront.fillAmount = 0f;

        ico.FadeFront.DOFillAmount(1f, duration).SetEase(Ease.Linear).OnComplete(() => RemoveOrRestart(ico));
    }

    private void RemoveOrRestart(StateIcoItem ico)
    {
        if (--ico.count > 0)
        {
            RefreshText(ico);
            StartProgress(ico, ico.currentDuration);
        }
        else
        {
            _activeEffects.Remove(ico);
            Destroy(ico.gameObject);
        }
    }

    private void RefreshText(StateIcoItem ico)
    {
        if (ico.count > 1)
        {
            ico.Text.text = ico.count.ToString();
            ico.Text.gameObject.SetActive(true);
        }
        else ico.Text.gameObject.SetActive(false);
    }

    private void AnimateIco(StateIcoItem icoItem, bool isAnimationNotNeed = false)
    {
        if (isAnimationNotNeed) return;

        Image ico = icoItem.FadeFront;
        ico.fillAmount = 0;
        if (icoItem.count == 1)
        {
            icoItem.Text.gameObject.SetActive(false);
            icoItem.count--;
            ico.DOFillAmount(1, icoItem.time[0]).SetEase(Ease.Linear);
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
        if (_activeEffects.Count > 0)
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].state == state)
                {
                    StateIcoItem icoItem = _activeEffects[i];
                    _activeEffects.Remove(icoItem);
                    Destroy(icoItem.gameObject);
                }
            }
    }

    public void RemoveIconCount()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].count > 0)
            {
                _activeEffects[i].count -= 1;
                _activeEffects[i].Text.text = _activeEffects[i].count.ToString();
                break;
            }
        }
    }

    public void DeactivateIcon()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].FadeFront.fillAmount = 0;
            Destroy(_activeEffects[i].gameObject);
            _activeEffects.RemoveAt(i);
            break;
        }
    }
    private void MoveIcoToEnd(int index)
    {
        if (index < 0 || index >= _activeEffects.Count) return;

        var ico = _activeEffects[index];
        _activeEffects.RemoveAt(index);
        _activeEffects.Add(ico);

        // ��������� ������� �����������
        ico.transform.SetAsLastSibling();
    }
}
/*
    public void RemoveIconCount()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].count > 0)
            {
                _activeEffects[i].count -= 1;
                _activeEffects[i].Text.text = _activeEffects[i].count.ToString();
                break;
            }
        }
    }

    public void DeactivateIcon()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].FadeFront.fillAmount = 0;
            Destroy(_activeEffects[i].gameObject);
            _activeEffects.RemoveAt(i);
            break;
        }
    }

   
}*/