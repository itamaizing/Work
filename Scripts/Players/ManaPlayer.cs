using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ManaPlayer : MonoBehaviour
{
    public float Mana = 1000;
    public GameObject ManaBar;
    public Transform DamageSpawn;
    public TextMeshPro PrefabText;

    public void AddMana(float manaValue)
    {
        Mana += manaValue;

        float newScaleX = Mana / 1000.0f;
        ManaBar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        if (manaValue > 0 && manaValue < 1)
        {
            manaValue = 1;
        }

        manaValue = (int)manaValue;
        PrefabText.text = "+" + manaValue.ToString();
        PrefabText.GetComponent<DamagePrefab>().StartColor = new Color(0, 0, 1, 1);
        PrefabText.GetComponent<DamagePrefab>().EndColor = new Color(0, 0, 1, 0.5f);
        TextMeshPro newPrefab = Instantiate(PrefabText, DamageSpawn.position, Quaternion.identity);
        newPrefab.transform.parent = transform;

        if (Mana <= 0)
        {
            Mana = 0;
        }

        if (Mana >= 1000)
        {
            Mana = 1000;
        }
    }
    public void UseMana(float manaValue)
    {
        Mana -= manaValue;

        float newScaleX = Mana / 1000.0f;
        ManaBar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        if (Mana <= 0)
        {
            Mana = 0;
        }
        if (Mana >= 1000)
        {
            Mana = 1000;
        }
    }
}
