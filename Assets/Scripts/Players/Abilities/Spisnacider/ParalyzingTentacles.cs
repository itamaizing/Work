using UnityEngine;

public class ParalyzingTentacles : SkillCreatureCarryGun
{
    [SerializeField] private LineRenderer _line;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private float paralyzeDuration = 3f;

    protected override string AnimationTrigger => "ParalyzingTentacles";

    private void OnEnable()
    {
        if (Hero.Health != null) Hero.Health.DamageTaken += OnDamageTaken;
    }

    private void OnDisable()
    {
        if (Hero.Health != null) Hero.Health.DamageTaken -= OnDamageTaken;
    }

    private void OnDamageTaken(Damage damage, Skill skill) => TryCancel();

    protected override void ApplySkillEffect(Character target)
    {
        if (target == null)
            return;

        if (_line != null)
        {
            _line.positionCount = 2;
            _line.enabled = true;

            Vector3 start = _startPoint != null ? _startPoint.position : Hero.transform.position;
            Vector3 end = target.transform.position + Vector3.up * 0.5f;

            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
        }

        target.CharacterState.CmdAddState( States.Stun, paralyzeDuration, 0f, Hero.gameObject, Name);
    }

    protected override void ClearData()
    {
        base.ClearData();

        if (_line != null)
            _line.enabled = false;
    }
}