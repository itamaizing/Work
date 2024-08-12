using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityRenderer : MonoBehaviour
{
    [SerializeField] private DrawCircle _circle;
    [SerializeField] private CircleArea _areaPref;
    [SerializeField] private AbilityLineRenderer _line;
    [SerializeField] private Color _colorForEnd;
    [SerializeField] private Color _colorForStart;

    private CircleArea _tempArea;
    private float _lineStartLength;
    private float _lineEndLength;
    private BoxArea _lineStartImage;
    private BoxArea _lineEndImage;

    private Coroutine _drawLineCoroutine;
    private Coroutine _drawAreaCoroutine;

    public void DrawRadius(float radius)
    {
        _circle.Draw(radius);
    }

    public void StopDrawRadius()
    {
        _circle.Clear();
    }

    public void DrawArea(float rarius, LayerMask layerMask, CircleArea area = null)
    {
        _drawAreaCoroutine = StartCoroutine(DrawAreaJob(rarius, layerMask));
    }

    public void StopDrawArea()
    {
        if (_drawAreaCoroutine != null)
            StopCoroutine(_drawAreaCoroutine);

        if(_tempArea != null)
            Destroy(_tempArea);
    }

    public void DrawLine(float length, float width, LayerMask layerMask, AbilityLineRenderer line = null)
    {
        if (line == null)
            line = _line;

        _drawLineCoroutine = StartCoroutine(DrawLineJob(length, width, layerMask, line));
    }

    public void StopDrawLine()
    {
        if (_drawLineCoroutine != null)
            StopCoroutine(_drawLineCoroutine);

        if (_lineStartImage != null)
            Destroy(_lineStartImage);

        if (_lineEndImage != null)
            Destroy(_lineEndImage);
    }

    private void RotateAtMouse(Transform transform)
    {
        Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    private IEnumerator DrawLineJob(float length, float width, LayerMask layerMask, AbilityLineRenderer line)
    {
        _lineStartImage = Instantiate(line.Start, transform);
        _lineEndImage = Instantiate(line.End, _lineStartImage.transform);

        _lineStartImage.SetColor(_colorForStart);
        _lineEndImage.SetColor(_colorForEnd);

        while (true)
        {
            RotateAtMouse(_lineStartImage.transform);

            Vector3 mouse = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            var vector = (mouse - transform.position);
            var dir = vector.normalized;

            RaycastHit2D rayHit = Physics2D.Raycast(transform.position, dir, length, layerMask);

            if (rayHit)
            {
                float distance = Vector2.Distance(rayHit.transform.position, transform.position);

                _lineStartImage.SetSize(width, distance);
                _lineEndImage.SetSize(width, length - distance);
            }
            else
            {
                _lineStartImage.SetSize(width, length);
                _lineEndImage.SetSize(width, 0);
            }
            yield return null;
        }
    }

    private IEnumerator DrawAreaJob(float radius, LayerMask layerMask)
    {
        Vector3 mouse = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);

        _tempArea = Instantiate(_areaPref, mouse, Quaternion.identity);
        _tempArea.SetSize(radius);

        while (true)
        {
            mouse = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            _tempArea.transform.position = mouse;
            yield return null;
        }
    }
}
