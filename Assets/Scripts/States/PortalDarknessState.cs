using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PortalDarknessState : RefreshingState
{
    public override States State => States.PortalDarkness;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    private int _maxToSpawn = 2;
    private int _spawnedCount = 0;

    public override List<StatusEffect> Effects => new List<StatusEffect>
    {
        StatusEffect.Freezing
    };

    public override Schools Schools => Schools.Water;

    private float _timer;
    private const float Interval = 1f;

    private const float BaseSpawnChance = 0.05f;
    private float _currentSpawnChance = BaseSpawnChance;

    private Character _caster;
    private MoveComponent _moveComponent;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _caster = personWhoMadeBuff;

        _moveComponent = character.GetComponent<MoveComponent>();

        _timer = 0f;
        _currentSpawnChance = BaseSpawnChance;
    }

    public override void UpdateState()
    {
        if (_caster.isServer) return;
        if (_caster == null || _caster.SpawnComponent == null) return;

        _timer += Time.deltaTime;
        if (_timer < Interval) return;

        _timer = 0f;

        bool isMoving = false;

        if (_moveComponent != null)
        {
            isMoving = _moveComponent.Rigidbody.linearVelocity.magnitude > 0.1f;
        }

        if (isMoving)
        {
            _currentSpawnChance = BaseSpawnChance;
        }
        else
        {
            _currentSpawnChance *= 2f;
            _currentSpawnChance = Mathf.Clamp(_currentSpawnChance, BaseSpawnChance, 1f);
        }

        if (Random.value <= _currentSpawnChance)
        {
            SpawnEnemyMinion();
        }
    }

    private void SpawnEnemyMinion()
    {
        if (_spawnedCount >= _maxToSpawn) return;
        _spawnedCount++;
        int enemyIndex = 0;

        Vector3 spawnPos = characterState.transform.position + Random.insideUnitSphere * 2f;
        spawnPos.y = characterState.transform.position.y;

        _caster.SpawnComponent.CmdSpawnEnemyPoint(spawnPos, Quaternion.identity, enemyIndex);
        if (_spawnedCount >= _maxToSpawn)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        _spawnedCount = 0;
    }
}