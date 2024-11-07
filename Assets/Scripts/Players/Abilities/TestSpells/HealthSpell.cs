using Mirror;
using System.Collections;
using UnityEngine;

public class HealthSpell : Skill
{
    [SerializeField] private Character _playerLinks;
    private Character _target;

    [SerializeField] private float healAmount = 50f;

    protected override bool IsCanCast => IsHaveCharge && _target != null;

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            ApplyHealthSpell(_target.gameObject);
            TryUseCharge();
        }
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    private void ApplyHealthSpell(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            Heal(targetCharacter);
        }
    }

    private void Heal(Character character)
    {
        Heal heal = new Heal
        {
            Value = healAmount
        };

        character.Health.Heal(ref heal, sourceName: "HealthSpell", skill: this);
    }
}
