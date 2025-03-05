using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackingPsionicEnergy : Energy
{
    [SerializeField] private Slider attackingPsionicsSlider;

    private const float _maxAttackingPsiEnergy = 30f;
    private const float _timeAttackingPsiEnergy = 6f;

    private float _remainingTime;
    private bool _isAttackingPsiActive = false;

    private Coroutine _attackingPsiEnergyCoroutine;

    public float MaxAttackingPsiEnergy => _maxAttackingPsiEnergy;
    public bool IsAttackingPsiEnergy => _isAttackingPsiActive;

    private void Start()
    {
        MaxValue = _maxAttackingPsiEnergy;
        UpdateAttackingEnergyBar();
    }

    private void Update()
    {
        UpdateAttackingEnergyBar();
    }

    public void ReceiveAttackingEnergy(float transferAmount)
    {
        _remainingTime = _timeAttackingPsiEnergy;

        Add(transferAmount);
        CurrentValue = Mathf.Min(CurrentValue, _maxAttackingPsiEnergy);

        RpcAttackingPsiEnergyChanged(true, CurrentValue);
        UpdateAttackingEnergyBar();

        if (_attackingPsiEnergyCoroutine != null)
        {
            StopCoroutine(_attackingPsiEnergyCoroutine);
        }
        _attackingPsiEnergyCoroutine = StartCoroutine(AttackingPsiEnergyJob());
    }

    private IEnumerator AttackingPsiEnergyJob()
    {
        while (_remainingTime > 0)
        {
            _remainingTime -= Time.deltaTime;
            yield return null;
        }

        CurrentValue = 0;
        _isAttackingPsiActive = false;

        RpcAttackingPsiEnergyChanged(false, 0f);
        UpdateAttackingEnergyBar();
    }

    private void UpdateAttackingEnergyBar()
    {
        attackingPsionicsSlider.value = CurrentValue / _maxAttackingPsiEnergy;
    }

    [ClientRpc]
    private void RpcAttackingPsiEnergyChanged(bool isActive, float energyValue)
    {
        _isAttackingPsiActive = isActive;
        UpdateAttackingEnergyBar();
    }
}
