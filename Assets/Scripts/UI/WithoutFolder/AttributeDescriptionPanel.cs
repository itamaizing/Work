using DG.Tweening;
using TMPro;
using UnityEngine;

public class AttributeDescriptionPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _description;

    public void ShowDesciption(string attribute)
    {
        gameObject.transform.DOScale(1, .2f);
        _description.text = attribute;
    }

    public void HideDescription()
    {
        _description.text = "";
        gameObject.transform.DOScale(0, .2f);
    }
}
