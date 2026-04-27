using System;
using UnityEngine;
using System.Collections;
using Mirror;
using Random = UnityEngine.Random;

public class InstantFlashBooster : SkillTalentHandler
{
    private FlashOfLight _flash;
    private readonly float _duration;
    private readonly float _chance;

    private bool _enabled;
    private Coroutine _currentFlashCoroutine;

    public InstantFlashBooster(NetworkBehaviour owner, float duration = 5f, float chance = 10f)
        : base(owner)
    {
        _duration = duration;
        _chance = chance;
    }

    public void Inject(FlashOfLight flash) => _flash = flash;

    public override void Enable(bool value) => _enabled = value;

    public void TryApply()
    {
        if (!_enabled || _flash == null || !Owner.isOwned) 
            return;

        if (Random.Range(0, 100) >= _chance) 
            return;

        if (_currentFlashCoroutine != null)
            return;

        _currentFlashCoroutine = StartCoroutine(InstantFlashJob());
    }

    private IEnumerator InstantFlashJob()
    {
        _flash.EnableSkillBoost();

        yield return new WaitForSeconds(_duration);

        _flash.DisableSkillBoost();
        _currentFlashCoroutine = null;
    }
}