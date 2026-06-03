using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ScorchedSoulSpreadNetwork : NetworkBehaviour
{
    [Command(requiresAuthority = false)]
    public void CmdApplySpreadDamage(List<GameObject> targets, float spreadDamage)
    {
        foreach (var target in targets)
        {
            if (target == null) continue;

            var character = target.GetComponent<Character>();
            if (character == null || character.IsDead) continue;
            
            int stacks = character.CharacterState.CheckStateStacks(States.ScorchedSoul);
            if (stacks <= 0) continue;

            float finalDamage = spreadDamage * (stacks * 0.2f);
            if (finalDamage <= 0f) continue;

            var damage = new Damage
            {
                Value = finalDamage,
                Type = DamageType.Magical,
                School = Schools.None
            };

            character.Health.TryTakeDamage(ref damage, null);
        }
    }
}
