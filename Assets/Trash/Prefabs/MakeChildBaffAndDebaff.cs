using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakeChildBaffAndDebaff : MonoBehaviour
{
    private MonoBehaviour[] allScripts;
    private Component _scriptObject;
    private Transform _effectsObject;
    private Transform _childObject;

    void Start()
    {
        allScripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in allScripts)
        {
            if (script != null && script != this)
            {
                _scriptObject = script;
                break;
            }
        }
    }

    void Update()
    {
        if (_effectsObject == null && transform.parent.Find("Effects"))
        {
            _effectsObject = transform.parent.Find("Effects");
            AddInEffects();
        }
        if (_scriptObject == null)
        {
            Destroy(gameObject);
        }
        if (_scriptObject == null && _childObject != null)
        {
            Destroy(_childObject.gameObject);
        }
    }
    private void AddInEffects()
    { 
        if (_effectsObject != null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                _childObject = transform.GetChild(i);
                _childObject.SetParent(_effectsObject);
            }
        }
    }
}
