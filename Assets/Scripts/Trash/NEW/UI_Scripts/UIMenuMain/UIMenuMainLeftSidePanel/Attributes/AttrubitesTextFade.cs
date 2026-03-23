using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttrubitesTextFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI _title;
    private Color _oldColor, _newColor;

    private void Awake()
    {
        _oldColor = _title.color;
        _newColor = new Color(255, 255, 141);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
         if (_title != null)
         {
             _title.color = _newColor;
         }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_title != null)
        {
            _title.color = _oldColor;
        }
    }
}
