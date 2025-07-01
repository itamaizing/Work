using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SkillCircleRanderer : MonoBehaviour
{
    [SerializeField] private DecalProjector _projector;
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _deactiveMaterial;

    private float _radius;
    private LayerMask _layerMask;
    private Transform _targetForFollow;
    private Coroutine _drawCoroutine;
    private Coroutine _followCoroutine;

    public void StartDraw(float radius, LayerMask layerMask)
    {
        _radius = radius;
        _layerMask = layerMask;

        var size = new Vector3 (_radius, _radius, 8);
        _projector.size = size;

        _drawCoroutine = StartCoroutine(DrawJob(_layerMask));
    }

    public void StartDraw(float radius, Transform target)
    {
        _radius = radius;

        var size = new Vector3(_radius, _radius, 8);
        _projector.size = size;

        _drawCoroutine = StartCoroutine(DrawJob(target));
    }

    public void StartDraw(float radius)
    {
        _radius = radius;

        var size = new Vector3(_radius, _radius, 8);
        _projector.size = size;

        _drawCoroutine = StartCoroutine(DrawJob());
    }

    public void SetTargetFollow(Transform targetForFollow)
    {
        transform.parent = targetForFollow;
    }

    public void SetFollowToMouse()
    {
        _followCoroutine = StartCoroutine(FollowToMouseJob());
    }

    public void StopFollowToMouse()
    {
        if(_followCoroutine != null)
        {
            StopCoroutine(_followCoroutine);
            _followCoroutine = null;
        }

    }

    private IEnumerator FollowToMouseJob()
    {
        while (true) 
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                transform.position = hit.point;
            }
            yield return null;
        }
    }

    private IEnumerator DrawJob(LayerMask layerMask)
    {
        while (true)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _radius, layerMask);

            if (colliders.Length == 0)
                _projector.material = _deactiveMaterial;
            else
                _projector.material = _activeMaterial;

            yield return null;
        }
    }

    private IEnumerator DrawJob(Transform target)
    {
        while (true)
        {
            var distance = Vector3.Distance(transform.position, target.position);

            if (distance > _radius)
                _projector.material = _deactiveMaterial;
            else
                _projector.material = _activeMaterial;

            yield return null;
        }
    }

    private IEnumerator DrawJob()
    {
        while (true)
        {

            yield return null;
        }
    }
}
