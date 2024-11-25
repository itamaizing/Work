using UnityEngine;

public class ContinuationAmbush : Talent
{
    private bool _isCanApplyInvisible;
    public bool IsCanApplyInvisible { get => _isCanApplyInvisible; set => _isCanApplyInvisible = value; }

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void CanApplyInvisible(bool isCanApplyInvisible)
    {
        Debug.Log("CanApplyInvisible");
        _isCanApplyInvisible = isCanApplyInvisible;
        Invoke("CanNotApplyInvisible", 1.0f);
    }

    private void CanNotApplyInvisible()
    {
        Debug.Log("CanNotApplyInvisible");
        _isCanApplyInvisible = false;
        Debug.Log($"CanNotApplyInvisible / isCanApply = {_isCanApplyInvisible}");
    }
}
