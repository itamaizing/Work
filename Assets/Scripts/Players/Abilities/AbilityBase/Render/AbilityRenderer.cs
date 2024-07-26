using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityRenderer : MonoBehaviour
{
    [SerializeField] private AbilityLineRenderer _line;
    [SerializeField] private Color _colorForEnd;
    [SerializeField] private Color _colorForStart;

    private float _lineStartLength;
    private float _lineEndLength;
    private SpriteRenderer _lineStartImage;
    private SpriteRenderer _lineEndImage;

    private Coroutine _drawLineCoroutine;

    public void DrawLine(float length, LayerMask layerMask, AbilityLineRenderer line = null)
    {
        if (line == null)
            line = _line;

        _drawLineCoroutine = StartCoroutine(DrawLineJob(length, layerMask, line));
    }

    private void RotateAtMouse(Transform transform)
    {
        Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    private IEnumerator DrawLineJob(float length, LayerMask layerMask, AbilityLineRenderer line)
    {
        _lineStartImage = Instantiate(line.Start, transform);
        _lineEndImage = Instantiate(line.End, _lineStartImage.transform);

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

                _lineStartImage.size = new Vector2(_lineStartImage.size.x, distance);
                _lineEndImage.size = new Vector2(_lineEndImage.size.x, length - distance);
            }
            else
            {
                _lineStartImage.size = new Vector2(_lineStartImage.size.x, length);
                _lineEndImage.size = new Vector2(_lineEndImage.size.x, 0);
            }
            yield return null;
        }
    }
}
