using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BaffDebaffEffectPrefab : MonoBehaviour
{
    public float Timer = 0f;

    private Transform parentTransform;
    private Transform effectParent;
    private string parentTag;

    public void StartCountdown(float time)
    {
        Timer = time;
    }


    private void Update()
    {
        if(Timer > 0)
        {
            effectParent = transform.parent;
            parentTransform = effectParent.parent;

            Timer -= Time.deltaTime;

            if (Timer <= 0)
            {
                Timer = 0;
                Destroy(gameObject);
            }

            GetComponent<TextMeshPro>().text = ((int)Timer + 1).ToString();
        }

        if (effectParent != null)
        {
            string effectTag = effectParent.gameObject.tag;
            if (parentTransform != null)
            {
                parentTag = parentTransform.gameObject.tag;
            }

            if (effectTag == "Debaff")
            {
                if (parentTag == "Enemies")
                {
                    GetComponent<TextMeshPro>().color = Color.green;
                }
                else if (parentTag == "Allies")
                {
                    GetComponent<TextMeshPro>().color = Color.red;
                }
            }
            else if (effectTag == "Baff")
            {
                if (parentTag == "Enemies")
                {
                    GetComponent<TextMeshPro>().color = Color.red;
                }
                else if (parentTag == "Allies")
                {
                    GetComponent<TextMeshPro>().color = Color.green;
                }
            }
        }
    }
}
