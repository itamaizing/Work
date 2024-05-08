using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityRender : MonoBehaviour
{
    [SerializeField] private DrawCircle _circle;
    [SerializeField] private SpriteRenderer _iconPref;
    [SerializeField] private SpriteRenderer _areaPref;
    [SerializeField] private SpriteRenderer _squareAreaPref;

    private SpriteRenderer _icon;
    private SpriteRenderer _area;
    private SpriteRenderer _squareArea;
    private Coroutine _drawCursorAbilityIconJob;
    public void Drawn(Ability ability)
    {
        Cursor.visible = false;

        _icon = Instantiate(_iconPref);
        _icon.sprite = ability.Icon;

        _area = Instantiate(_areaPref);
        _area.size = new Vector2(ability.Area, ability.Area);

        _squareArea = Instantiate(_squareAreaPref, transform);
        _squareArea.size = new Vector2(ability.CastWidth, ability.CastLength);

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
        Destroy(_squareArea);

        _icon = null;
        _area = null;
        _squareArea = null;
    }

    private void RotateAtMouse(Transform transform)
    {
        Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    private IEnumerator DrawCoroutine()
    {
        while (Input.GetMouseButtonDown(0) == false)
        {
            Vector3 mouse = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            _icon.transform.position = mouse;
            _area.transform.position = mouse;
            RotateAtMouse(_squareArea.transform);

            yield return null;
        }
        StopDraw();
    }
}
