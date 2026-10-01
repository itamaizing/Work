using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreparationAreaManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> _preparationAreas;

    private Coroutine _disableCoroutine;
    
    public void PreparationAreasDisable(float time)
    {
        if (_disableCoroutine != null)
        {
            StopCoroutine(_disableCoroutine);
            _disableCoroutine = null;
        }

        _disableCoroutine = StartCoroutine(IPreparationAreasDisable(time));
    }

    private IEnumerator IPreparationAreasDisable(float time)
    {
        foreach (GameObject preparationArea in _preparationAreas) 
        {
            if (preparationArea != null) preparationArea.SetActive(true);
        }

        yield return new WaitForSeconds(time);

        foreach (GameObject preparationArea in _preparationAreas) 
        {
            if (preparationArea != null) preparationArea.SetActive(false);
        }

        _disableCoroutine = null;
    }
}
