using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ManaPlayer : MonoBehaviour
{
    [SerializeField][Range(0,100)] private float _manaRegenerationValue = 10;
    [SerializeField][Range(0, 100)] private float _manaRegenerationDelay = 3;
    [SerializeField] private float MaxMana;
    private WaitForSeconds _waitForRegenMana;

    public float Mana = 1000;
    public GameObject ManaBar;
    public Transform DamageSpawn;
    public TextMeshPro PrefabText;
    private void Start()
    {
        _waitForRegenMana = new WaitForSeconds(_manaRegenerationDelay);
        StartCoroutine(RegenirateMana());
    }
    public void AddMana(float manaValue)
    {
        Mana += manaValue;

        float newScaleX = Mana / MaxMana;
        ManaBar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        /*if (manaValue > 0 && manaValue < 1)
        {
            manaValue = 1;
        }
        */
        PrefabText.text = "+" + manaValue.ToString();
        PrefabText.GetComponent<DamagePrefab>().StartColor = new Color(0, 0, 1, 1);
        PrefabText.GetComponent<DamagePrefab>().EndColor = new Color(0, 0, 1, 0.5f);
        TextMeshPro newPrefab = Instantiate(PrefabText, DamageSpawn.position, Quaternion.identity);
        newPrefab.transform.SetParent(transform);

        if (Mana <= 0)
        {
            Mana = 0;
        }

        if (Mana >= MaxMana)
        {
            Mana = MaxMana;
        }
    }
    public void UseMana(float manaValue)
    {
        Mana -= manaValue;

        float newScaleX = Mana / MaxMana;
        ManaBar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);

        if (Mana <= 0)
        {
            Mana = 0;
        }
        if (Mana >= MaxMana)
        {
            Mana = MaxMana;
        }
    }

    private IEnumerator RegenirateMana()
    {
        while (true)
        {
            yield return _waitForRegenMana;
            this.AddMana(_manaRegenerationValue);
        }
    }
}
