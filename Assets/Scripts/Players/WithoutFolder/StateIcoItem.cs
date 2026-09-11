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

    public AbstractCharacterState StateInstance;
    public States State { get; set; }

    public string ResolvedTooltipName { get; set; }
    public string ResolvedTooltipDescription { get; set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"Tooltip.Instance is null: {StateTooltip.Instance == null}");
        StateTooltip.Instance?.RequestShow(ResolvedTooltipName, ResolvedTooltipDescription);
    }

    public void OnPointerExit(PointerEventData eventData) =>
        StateTooltip.Instance?.Hide();
}
