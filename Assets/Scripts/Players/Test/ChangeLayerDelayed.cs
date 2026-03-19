using System.Collections;
using UnityEngine;

public class ChangeParentLayerDelayed : MonoBehaviour
{
    [SerializeField] private float _delay = 0.5f;
    [SerializeField] private LayerMask _targetLayer;

    private Coroutine _changeCoroutine;

    private void Start() => StartChangeLayer();

    public void StartChangeLayer()
    {
        if (_changeCoroutine != null)
            StopCoroutine(_changeCoroutine);

        _changeCoroutine = StartCoroutine(ChangeLayerRoutine());
    }

    private IEnumerator ChangeLayerRoutine()
    {
        yield return new WaitForSeconds(_delay);
        transform.gameObject.layer = LayerMaskToLayer(_targetLayer);
    }

    private int LayerMaskToLayer(LayerMask mask)
    {
        int layer = 0;
        int value = mask.value;

        while (value > 1)
        {
            value >>= 1;
            layer++;
        }

        return layer;
    }
}