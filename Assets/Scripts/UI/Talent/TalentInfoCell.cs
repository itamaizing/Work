using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TalentInfoCell : MonoBehaviour
{
    [SerializeField] private TMP_Text _textDescription;
    [SerializeField] private Image _image;

    private LayoutElement _separatorLayout;
    private bool _hasLineGeometry;
    private Vector2 _lineSizeDelta;
    private Vector2 _linePosition;

    public TMP_Text TextDescription { get => _textDescription; }

    public void Show(GameDescriptions.DescriptionRow row)
    {
        _textDescription.text = row.Text;
        _textDescription.enabled = !row.IsLineBreak;
        _image.gameObject.SetActive(row.IsLineBreak);
        ApplyLinePadding(row.IsLineBreak ? row.LeftPadding : 0f, row.IsLineBreak ? row.RightPadding : 0f);

        if (row.IsLineBreak && _separatorLayout == null)
        {
            _separatorLayout = gameObject.AddComponent<LayoutElement>();
            float height = Mathf.Max(3f, _image.rectTransform.rect.height) + 8f;
            _separatorLayout.minHeight = height;
            _separatorLayout.preferredHeight = height;
            _separatorLayout.flexibleHeight = 0f;
        }

        if (_separatorLayout != null)
            _separatorLayout.enabled = row.IsLineBreak;
    }

    public void ShowDividingLine()
    {
        ApplyLinePadding(0f, 0f);
        _image.gameObject.SetActive(true);
    }

    private void ApplyLinePadding(float left, float right)
    {
        var rect = _image.rectTransform;
        if (!_hasLineGeometry)
        {
            _lineSizeDelta = rect.sizeDelta;
            _linePosition = rect.anchoredPosition;
            _hasLineGeometry = true;
        }

        float width = Mathf.Max(0f, rect.rect.width - rect.sizeDelta.x + _lineSizeDelta.x);
        float total = left + right;
        if (total > width && total > 0f)
        {
            float scale = width / total;
            left *= scale;
            right *= scale;
        }

        rect.sizeDelta = new Vector2(_lineSizeDelta.x - left - right, _lineSizeDelta.y);
        rect.anchoredPosition = new Vector2(
            _linePosition.x + left * (1f - rect.pivot.x) - right * rect.pivot.x,
            _linePosition.y);
    }

    public void HideDividingLine()
    {
        _image.gameObject.SetActive(false);
    }
}
