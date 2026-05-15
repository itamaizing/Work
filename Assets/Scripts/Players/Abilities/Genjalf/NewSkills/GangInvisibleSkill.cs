using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GangInvisibleSkill : Skill
{
    [Header("Ability Properties")]
    [SerializeField] private List<SkinnedMeshRenderer> _playerRenderers = new();

    private bool _isInvisible = false;
    private Coroutine _manaDrainCoroutine;
    private Coroutine _exitFromInvisibleCoroutine;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("GangInvisible");
    protected override bool IsCanCast => true;

    private Resource _mana;

    private float _manaPerSecond = 10f;

    #region PrepareAndCastJob

    protected override void ClearData() { }

    public override void LoadTargetData(TargetInfo targetInfo) { }
    
    public void AnimCastInvisible()
    {
    }

    public void AnimInvisibleEnd()
    {
        AnimCastEnded();
    }   

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        if(!_mana)
            _mana = _hero.Resources[ResourceType.Mana];
        
        if (!_isInvisible)
        {
            EnteringInvisible();
        }
        else
        {
            ExitingInvisible();
        }

        yield return null;
    }

    #endregion

    #region Invisible Enter / Exit

    public void EnteringInvisible()
    {
        CmdApplyInvis(_hero.gameObject);
    }

    public void ExitingInvisible()
    {
        CmdRemoveInvisible();
    }
    
    private void OnAbilityUsed(Skill skill)
    {
        if(skill == this) return;
        
        if (_isInvisible)
        {
            ExitingInvisible();
        }
    }

    #endregion

    #region Mana Drain

    private IEnumerator ManaDrainCoroutine()
    {
        while (_isInvisible)
        {
            yield return new WaitForSeconds(1f);

            if (!_isInvisible)
                yield break;

            if (_mana.CurrentValue < _manaPerSecond)
            {
                ExitingInvisible();
                yield break;
            }
            
            _mana.CmdUse(_manaPerSecond);
        }
    }

    #endregion

    #region Exit By Input

    private IEnumerator ExitFromInvisible()
    {
        while (_isInvisible)
        {
            if (Input.GetMouseButtonDown(2))
            {
                ExitingInvisible();
                yield break;
            }

            yield return null;
        }
    }

    #endregion

    #region Transparency

    private void ApplyTransparency(List<SkinnedMeshRenderer> renderers, float alpha)
    {
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            foreach (Material mat in renderer.materials)
            {
                if (mat == null) continue;

                mat.SetFloat("_Alpha",alpha);
            }
        }
    }

    #endregion

    #region CommandMethods

    [Command]
    private void CmdApplyInvis(GameObject player)
    {
        _isInvisible = true;
        _hero.IsInvisible = true;

        RpcApplyInvis();
    }

    [Command]
    private void CmdRemoveInvisible()
    {
        _isInvisible = false;
        _hero.IsInvisible = false;

        RpcRemoveInvisible();
    }

    #endregion

    #region RpcMethods

    [ClientRpc]
    private void RpcApplyInvis()
    {
        _isInvisible = true;
        _hero.IsInvisible = true;

        _hero.GetComponent<Character>().IsInvisible = true;

        float alpha = 0.5f;
        ApplyTransparency(_playerRenderers, alpha);
        _hero.Abilities.SkillCastEnded += OnAbilityUsed;

        if (_manaDrainCoroutine != null) StopCoroutine(_manaDrainCoroutine);
        _manaDrainCoroutine = StartCoroutine(ManaDrainCoroutine());

        if (_exitFromInvisibleCoroutine != null) StopCoroutine(_exitFromInvisibleCoroutine);
        _exitFromInvisibleCoroutine = StartCoroutine(ExitFromInvisible());
    }

    [ClientRpc]
    private void RpcRemoveInvisible()
    {
        _isInvisible = false;
        _hero.IsInvisible = false;

        _hero.GetComponent<Character>().IsInvisible = false;

        ApplyTransparency(_playerRenderers, 1f);
        
        _hero.Abilities.SkillCastEnded -= OnAbilityUsed;

        if (_manaDrainCoroutine != null)
        {
            StopCoroutine(_manaDrainCoroutine);
            _manaDrainCoroutine = null;
        }
        if (_exitFromInvisibleCoroutine != null)
        {
            StopCoroutine(_exitFromInvisibleCoroutine);
            _exitFromInvisibleCoroutine = null;
        }
    }

    #endregion
}
