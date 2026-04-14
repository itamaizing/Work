using System.Collections;
using UnityEngine;
using Mirror;

public class AmbushPoisons : Skill
{
    [SerializeField] private CreeperInvisible _invisible;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    private const int MaxStacks = 3;
    private const float StackInterval = 3f;
    private const float ClearDelay = 3f;

    private int _currentStacks = 0;

    private Coroutine _stackRoutine;
    private Coroutine _clearRoutine;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;

    private void OnEnable()
    {
        if (_invisible != null) _invisible.OnInvisibleChanged += OnInvisibleChanged;
    }

    private void OnDisable()
    {
        if (_invisible != null) _invisible.OnInvisibleChanged -= OnInvisibleChanged;
    }

    private void OnInvisibleChanged(bool isInvisible)
    {
        if (isInvisible)
        {
            StartStacking();
            StopClearing();
        }
        else
        {
            StopStacking();
            StartClearing();
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

            Charges.SendCurrentChange(_currentStacks);

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

        Charges.SendCurrentChange(_currentStacks);
        _clearRoutine = null;
    }

    public bool TryConsumeStack(Character target)
    {
        if (_currentStacks <= 0) return false;
        if (target == null) return false;

        _currentStacks--;

        Charges.SendCurrentChange(_currentStacks);

        target.CharacterState.AddStateLogic(States.PoisonBone, 6, 0, Schools.None, Hero.gameObject, null);
        if (_creeperPoisonAura.IsFeelingPoisoning) Hero.CharacterState.AddStateLogic(States.FeelingPoisoning, 2f, 0, Schools.None, Hero.gameObject, null);


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