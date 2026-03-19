using System.Collections;
using UnityEngine;
using Mirror;

public class AmbushPoisons : Skill
{
    private const int MaxStacks = 3;
    private const float StackInterval = 3f;
    private const float ClearDelay = 3f;

    private int _currentStacks = 0;

    private Coroutine _stackRoutine;
    private Coroutine _clearRoutine;

    private CreeperInvisible _invisible;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;

    private void Start()
    {
        _invisible = GetComponent<CreeperInvisible>();
        StartCoroutine(StateWatcher());
    }

    private IEnumerator StateWatcher()
    {
        WaitForSeconds delay = new WaitForSeconds(0.1f);

        while (true)
        {
            if (!isServer)
            {
                yield return delay;
                continue;
            }

            bool isInvis = _invisible != null && _invisible.IsInvisible;

            if (isInvis)
            {
                StartStacking();
                StopClearing();
            }
            else
            {
                StopStacking();
                StartClearing();
            }

            yield return delay;
        }
    }

    private void StartStacking()
    {
        if (_stackRoutine != null) return;
        if (_currentStacks >= MaxStacks) return;

        _stackRoutine = StartCoroutine(StackRoutine());
    }

    private void StopStacking()
    {
        if (_stackRoutine == null) return;

        StopCoroutine(_stackRoutine);
        _stackRoutine = null;
    }

    private IEnumerator StackRoutine()
    {
        while (_currentStacks < MaxStacks)
        {
            yield return new WaitForSeconds(StackInterval);

            if (_invisible == null || !_invisible.IsInvisible) break;

            _currentStacks++;

            if (_currentStacks >= MaxStacks)
            {
                _stackRoutine = null;
                yield break;
            }
        }

        _stackRoutine = null;
    }

    private void StartClearing()
    {
        if (_clearRoutine != null) return;

        _clearRoutine = StartCoroutine(ClearRoutine());
    }

    private void StopClearing()
    {
        if (_clearRoutine == null) return;

        StopCoroutine(_clearRoutine);
        _clearRoutine = null;
    }

    private IEnumerator ClearRoutine()
    {
        yield return new WaitForSeconds(ClearDelay);

        _currentStacks = 0;

        _clearRoutine = null;
    }

    public bool TryConsumeStack(Character target)
    {
        if (!isServer) return false;
        if (_currentStacks <= 0) return false;
        if (target == null) return false;

        _currentStacks--;

        target.CharacterState.AddState(States.PoisonBone, 6f, 1, Hero.gameObject, this.name);

        if (_invisible != null && _invisible.IsInvisible)
        {
            StartStacking();
        }

        return true;
    }

    public int GetStacks() => _currentStacks;

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callback) => null;
    protected override IEnumerator CastJob() => null;
    protected override void ClearData() { }
    public override void LoadTargetData(TargetInfo targetInfo) { }
}