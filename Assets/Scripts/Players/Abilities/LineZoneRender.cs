using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class LineZoneRender : MonoBehaviour 
{
    [SerializeField] private LineRenderer _lineRenderer;

    private Skill _skill;
    private Coroutine _lineDrawCoroutine;

    public void StartDraw(Skill skill)
    {
        _skill = skill;
        _skill.ClickPoint += SetPoint;

        _lineDrawCoroutine = _skill.StartCoroutine(DrawJob());
    }

    public void StopDraw()
    {
        if(_skill != null)
        {
            _skill.StopCoroutine(_lineDrawCoroutine);

            _lineRenderer.positionCount = 0;
            _lineRenderer.SetPositions(new Vector3[0]);
            _skill.ClickPoint -= SetPoint;
            _skill = null;
        }
    }

    private void SetPoint(Vector3 point)
    {

        _lineRenderer.positionCount = _lineRenderer.positionCount + 1;
        _lineRenderer.SetPosition(_lineRenderer.positionCount - 2, point);
    }

    private IEnumerator DrawJob()
    {
        _lineRenderer.positionCount = _lineRenderer.positionCount + 1;
        yield return null;

        while (true)
        {
            if (Input.GetMouseButtonDown(0))
                SetPoint(_skill.GetMousePoint() + Vector3.up / 10);

            _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, _skill.GetMousePoint() + Vector3.up / 10);
            yield return null;
        }
    }
}
