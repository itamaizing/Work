using UnityEngine;

public class TimeBackTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private TimeBackSkill _timeBackSkill;
    public override void Enter()
    {
        _skillManager.ActivateSkill(_timeBackSkill);
        _timeBackSkill.StartRecording();
    }

    public override void Exit()
    {
        _timeBackSkill.StopRecording();
        _skillManager.DeactivateSkill(_timeBackSkill);
    }
}
