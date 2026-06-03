using UnityEngine;

public interface IFireComboParticipatingSkill
{
    public void OnFinalComboSkill(GameObject target);

    public void OnTargetHasComboPoint(GameObject target,float comboPoints);
}
