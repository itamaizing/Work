using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityRender : MonoBehaviour
{
    [SerializeField] private DrawCircle _circle;
    [SerializeField] private SpriteRenderer _iconPref;
    [SerializeField] private CircleArea _areaPref;
    [SerializeField] private BoxArea _squareAreaPref;

    private float _radius;
    private SpriteRenderer _icon;
    private CircleArea _area;
    private BoxArea _squareArea;
    private Coroutine _drawCursorAbilityIconJob;
    public void Drawn(Ability ability)
    {
        Cursor.visible = false;

        _icon = Instantiate(_iconPref);
        _icon.sprite = ability.Icon;

        _area = Instantiate(_areaPref, transform);
        _area.SetSize(ability.Area);

        _squareArea = Instantiate(_squareAreaPref, transform);
        _squareArea.SetSize(ability.CastWidth, ability.CastLength);

        _radius = ability.Radius;

        if (ability.CastLength <= 0)
        {
            _circle.Draw(_radius);
        }
        _drawCursorAbilityIconJob = StartCoroutine(DrawCoroutine());
    }

    public void StopDraw()
    {
        if (_icon == null)
            return;

        Cursor.visible = true;
        StopCoroutine(_drawCursorAbilityIconJob);

        Destroy(_icon.gameObject);
        Destroy(_area.gameObject);
        Destroy(_squareArea.gameObject);
        _circle.Clear();

        _icon = null;
        _area = null;
        _squareArea = null;
        _radius = 0;
    }

    private bool IsMouseInRadius(float radius)
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= radius;
    }

    private void RotateAtMouse(Transform transform)
    {
        Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    private IEnumerator DrawCoroutine()
    {
        while (true)
        {
            Vector3 mouse = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            _icon.transform.position = mouse;
            _area.transform.position = mouse;
            RotateAtMouse(_squareArea.transform);

            if (IsMouseInRadius(_radius))
            {
                _circle.SetColor(Color.green);
                _area.SetColor(Color.green);
            }
            else
            {
                _circle.SetColor(Color.red);
                _area.SetColor(Color.red);
            }
            if (_squareArea.IsConcernsEnemy)
            {
                _squareArea.SetColor(Color.green);
            }
            else
            {
                _squareArea.SetColor(Color.red);
            }
            yield return null;
        }
    }
}
