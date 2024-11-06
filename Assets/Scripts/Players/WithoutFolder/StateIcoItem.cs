using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class StateIcoItem : MonoBehaviour
{
    //public private Image _ico;
    //public TextMeshProUGUI _text;

    public Image FadeFront;
    public Image border;
    public TextMeshProUGUI Text;
    public int count = 1;
    public States state;
    public List<float> time = new List<float>();
    /*private void AnimateIco(Image ico, float time, int stack)
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
    }*/
}
