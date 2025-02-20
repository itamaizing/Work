using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static HealthComponent;

public class PsionicaMelee : MonoBehaviour
{
    public float Psionica;
    public GameObject ManaBar;
    public GameObject ManaBar2;
    public Text text;
    public float AbsorptionChance = 0.1f;

    private float timer = 0f;
    private bool isTimerActive = false;

    private void Start()
    {
        Psionica = 0f;
    }

    private void Update()
    {
        UpdateManaBars();

        text.text = "Псионика: " + Psionica.ToString();

        if (isTimerActive)
        {
            timer += Time.deltaTime;

            if (timer >= 6f)
            {
                ResetPsionicaTimer();
            }
        }
    }

    private void UpdateManaBars()
    {
        //float health = GetComponent<HealthComponent>()._currentHealth;

        if (Psionica <= 30f)
        {
            UpdateManaBar(ManaBar2, 0f);
            UpdateManaBar(ManaBar, Psionica / 30.0f);
        }
        else
        {
            UpdateManaBar(ManaBar, 1.0f);
            //float newScaleX = Psionica / (health - 30);
            //UpdateManaBar(ManaBar2, Mathf.Clamp01(newScaleX));
        }
    }

    private void UpdateManaBar(GameObject manaBar, float scaleX)
    {
        manaBar.transform.localScale = new Vector3(scaleX, 1.0f, 1.0f);
    }

    private void ResetPsionicaTimer()
    {
        Psionica = 0f;
        timer = 0f;
        isTimerActive = false;
    }

    public void MakePsionica(float damageValue)
    {
        timer = 0f;
        isTimerActive = true;

        Psionica += damageValue;
        //float health = GetComponent<HealthComponent>()._currentHealth;
        //Psionica = Mathf.Min(Psionica, health);
    }

    public void UsePsionica(float value)
    {
        Psionica -= value;
        Psionica = Mathf.Max(Psionica, 0);
    }

    public void PsionicaAbsorption(ref float modifiedDamage)
    {
        if (Psionica > 0)
        {
            float absorptionAmount = Mathf.Min(Psionica, modifiedDamage);
            UsePsionica(absorptionAmount);
            modifiedDamage = (modifiedDamage - absorptionAmount) + absorptionAmount - ((modifiedDamage - absorptionAmount) + absorptionAmount) * AbsorptionChance;
        }
    }
}