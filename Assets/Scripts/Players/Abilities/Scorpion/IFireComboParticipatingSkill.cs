using UnityEngine;

public interface IFireComboParticipatingSkill
{
    bool IsAoe { get; }
    public void OnFinalComboSkill(GameObject target);

    public void OnTargetHasComboPoint(GameObject target,float comboPoints);
}
