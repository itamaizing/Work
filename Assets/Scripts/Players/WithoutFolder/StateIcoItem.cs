using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StateIcoItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image Icon;
    public Image FadeFront;
    public Image border;
    public TextMeshProUGUI Text;

    public StateBasic StateInstance;
    public States State { get; set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StateTooltip.Instance?.RequestShow(this, StateInstance);
    }

    public void OnPointerExit(PointerEventData eventData) =>
        StateTooltip.Instance?.Hide(this);

    private void OnDisable() => StateTooltip.Instance?.Hide(this);
}
