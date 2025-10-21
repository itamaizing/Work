using Mirror;
using UnityEngine;

public class Mucus : NetworkBehaviour
{
    [SerializeField] private float fadeDuration = 9f;

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PsionicEnergySkill>(out var psiSkill))
        {
            if (psiSkill.Hero != null && psiSkill.Hero.TryGetComponent<CharacterState>(out var state))
            {
                if (state.CheckForState(States.HealingSlime))
                {
                    var healingSlime = (HealingSlime)state.GetState(States.HealingSlime);
                    if (healingSlime != null)
                    {
                        healingSlime.duration = 999f;
                        healingSlime.ResetDecreasePhase();
                    }
                }
                else state.AddState(States.HealingSlime, 999f, 0f, this.gameObject, "Mucus");
            }
        }
    }

    [Server]
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PsionicEnergySkill>(out var psiSkill))
        {
            if (psiSkill.Hero != null && psiSkill.Hero.TryGetComponent<CharacterState>(out var state))
            {
                var healingSlime = (HealingSlime)state.GetState(States.HealingSlime);
                if (healingSlime != null)
                {
                    healingSlime.duration = fadeDuration;
                    healingSlime.BeginDecreasePhase();
                }
            }
        }
    }
}
