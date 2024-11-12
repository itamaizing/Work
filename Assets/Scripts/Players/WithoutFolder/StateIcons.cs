using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StateIcons : MonoBehaviour
{
    [SerializeField] private GameObject _spawnPos;
    [SerializeField] private List<StateIcoItem> _icons;
    private List<StateIcoItem> _activeEffects = new List<StateIcoItem>();
    private bool _added = false;

    public void ActivateIco(States state, float timeToDecrease, int stack, bool canStack)
    {
        if (canStack)
        {
            foreach (var ico in _activeEffects)
            {
                if (ico.state == state)
                {
                    ico.count += stack;
                    ico.time.Add(timeToDecrease);
                    ico.Text.text = ico.count.ToString();
                    ico.Text.gameObject.SetActive(true);
                    return;
                }
            }
        }

        foreach (var ico in _icons)
        {
            if (ico.state == state)
            {
                var newIco = Instantiate(ico, _spawnPos.transform);
                newIco.time.Add(timeToDecrease);
                newIco.count = stack;
                _activeEffects.Add(newIco);

                if (timeToDecrease < 0)
                {
                    AnimateIco(newIco, true);
                }
                else
                {
                    AnimateIco(newIco);
                }

                _added = true;
                break;
            }
        }

        if (!_added)
        {
            Debug.Log("There is no stateIco " + state.ToString());
            _added = false;
        }
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
            ico.DOFillAmount(1, icoItem.time[0]).SetEase(Ease.Linear).OnComplete(() => RemoveItem(icoItem));
            icoItem.time.RemoveAt(0);
        }
        else
        {
            icoItem.Text.gameObject.SetActive(true);
            icoItem.Text.text = icoItem.count.ToString();
            icoItem.count--;
            ico.DOFillAmount(1, icoItem.time[0]).SetEase(Ease.Linear).OnComplete(() => AnimateIco(icoItem));
            icoItem.time.RemoveAt(0);
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
        Destroy(icoItem.gameObject);
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
}
