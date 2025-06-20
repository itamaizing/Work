using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReconnaissanceFireAura : NetworkBehaviour
{
    [SerializeField] private float partialBlindnessDuration = 5f;
    [SerializeField] private float anxietyDuration = 5f;
    [SerializeField] private GameObject fireEffect;
    [SerializeField] private GameObject fireEffectDark;
    [SerializeField] private bool fireDarkTalent;
    [SerializeField] private bool partialBlindnessTalent;

    public event Action<bool> OnStateDarkTalentChanged;

    private List<Character> charactersInZone = new List<Character>();
    private Coroutine effectCoroutine;
    private bool stateDark;

    public bool FireDarkTalent { get => fireDarkTalent; set => fireDarkTalent = value; }
    public bool PartialBlindnessTalent { get => partialBlindnessTalent; set => partialBlindnessTalent = value; }
    public bool StateDark { get => stateDark; set => stateDark = value; }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Character>(out Character character) && !charactersInZone.Contains(character))
        {
            charactersInZone.Add(character);

            if (effectCoroutine == null)
            {
                effectCoroutine = StartCoroutine(ApplyPartialBlindnessPeriodically());
            }
        }

        if (other.TryGetComponent<ArrowProjectile>(out ArrowProjectile arrow) && stateDark == false)
        {
            if (arrow.ArrowDark && fireDarkTalent)
            {
                stateDark = true;
                SwitchEffectFire();
                OnStateDarkTalentChanged?.Invoke(stateDark);
            }
        }
    }

    [Server]
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Character>(out Character character))
        {
            charactersInZone.Remove(character);

            if (charactersInZone.Count == 0 && effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;
            }
        }
    }

    private IEnumerator ApplyPartialBlindnessPeriodically()
    {
        while (charactersInZone.Count > 0)
        {
            foreach (Character character in charactersInZone)
            {
                if (character != null && character.TryGetComponent<CharacterState>(out var characterState))
                {
                    if (stateDark && fireDarkTalent) characterState.AddState(States.Anxiety, anxietyDuration, 0f, gameObject, "ReconnaissanceFireAuraDark");

                    if (partialBlindnessTalent) characterState.AddState(States.PartialBlindness, partialBlindnessDuration, 0f, gameObject, "partialBlindnessTalent");
                    else characterState.AddState(States.PartialBlindness, partialBlindnessDuration, 0f, gameObject, "ReconnaissanceFireAura");
                }
            }

            yield return new WaitForSeconds(1f);
        }

        effectCoroutine = null;
    }

    public void ApplyFireWorshipperTalentEffect(bool isActive)
    {
        if (isActive)
        {
            transform.localScale += Vector3.one;

            if (fireEffect != null) fireEffect.transform.localScale += Vector3.one;

            if (fireEffectDark != null) fireEffectDark.transform.localScale += Vector3.one;
        }
    }

    public void SwitchEffectFire()
    {
        fireEffect.SetActive(false);
        fireEffectDark.SetActive(true);
    }

    internal bool TryGetComponent<T>(out T @object, object v)
    {
        throw new NotImplementedException();
    }
}
