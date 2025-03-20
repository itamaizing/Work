using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;
using UnityEngine.SceneManagement;

public enum ScorpionAbility
{
    Punch,
    Kick,
    Blade,
    ChainBlade
}

public class PassiveCombo_Scorpion : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private HeroComponent _hero;
    [SerializeField] private ComboPoints_Player _comboPlayer;

    [SerializeField] private Sub_LavaPool_Scorpion _poolPrefab;

    [Header("Skills Reference")]
    [SerializeField] private Skill PunchSkill;
    [SerializeField] private Skill KickSkill;
    [SerializeField] private Skill BladeSkill;
    [SerializeField] private Skill ChainBladeSkill;

    [Header("Combo Settings")]
    private List<ScorpionAbility> _usedAbilities = new List<ScorpionAbility>();
    private Character _currentTarget;
    [SerializeField] private float _comboTimeout = 1f;
    private Coroutine _comboTimerCoroutine;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem _particlesAddStack;
    [SerializeField] private ParticleSystem _particlesNoCharges;
    [SerializeField] private ParticleSystem _particlesFullCombo;
    [SerializeField] private ParticleSystem _particlesCancelCombo;

    #region Add Ability (Комбо механика)

    public void AddAbility(Character enemy, ScorpionAbility scorpionAbility)
    {
        if (enemy == null) return;

        if (_currentTarget == null)
            _currentTarget = enemy;

        if (_currentTarget != enemy)
        {
            Debug.Log("Цель изменилась. Сброс комбо.");
            ResetCounter();
            _currentTarget = enemy;
        }

        if (!TryAddAbility(scorpionAbility))
        {
            Debug.LogWarning($"Нет зарядов {scorpionAbility}");
            _particlesNoCharges?.Play();
            return;
        }

        if (_usedAbilities.Count != 3)
        {
            _particlesAddStack?.Play();
            StartOrRestartComboTimer();
            return;
        }

        if (_usedAbilities.Distinct().Count() <= 1)
        {
            Debug.LogWarning("Комбо из одинаковых способностей — отмена");
            _usedAbilities.Clear();
            _particlesCancelCombo?.Play();
            return;
        }

        Debug.Log("Успешное комбо!");

        foreach (var ability in _usedAbilities.Distinct().ToList())
        {
            int count = _usedAbilities.Count(n => n == ability);
            UseCharge(ability, count);
        }

        _particlesFullCombo?.Play();

        CastDebuff(enemy.transform, _usedAbilities.Last());
        ApplyComboState(enemy);

        CmdAdd();

        ResetCounter();
    }

    private bool TryAddAbility(ScorpionAbility ability)
    {
        Skill skill = GetSkillByAbility(ability);
        if (skill == null) return false;

        int availableCharges = skill.Chargers;
        int currentUsage = _usedAbilities.Count(n => n == ability);

        if (currentUsage + 1 <= availableCharges)
        {
            _usedAbilities.Add(ability);
            StartOrRestartComboTimer();
            return true;
        }

        return false;
    }

    private void UseCharge(ScorpionAbility ability, int amount)
    {
        Skill skill = GetSkillByAbility(ability);
        if (skill == null) return;

        for (int i = 0; i < amount; i++)
        {
            if (!skill.TryUseCharge())
            {
                Debug.LogWarning($"Не удалось использовать заряд {ability}");
                break;
            }
        }
    }

    private Skill GetSkillByAbility(ScorpionAbility ability)
    {
        return ability switch
        {
            ScorpionAbility.Punch => PunchSkill,
            ScorpionAbility.Kick => KickSkill,
            ScorpionAbility.Blade => BladeSkill,
            ScorpionAbility.ChainBlade => ChainBladeSkill,
            _ => null
        };
    }

    #endregion

    #region Combo Timer

    private void StartOrRestartComboTimer()
    {
        if (_comboTimerCoroutine != null)
            StopCoroutine(_comboTimerCoroutine);

        _comboTimerCoroutine = StartCoroutine(ComboTimerCoroutine());
    }

    private IEnumerator ComboTimerCoroutine()
    {
        float timer = _comboTimeout;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("Таймаут комбо");
        ResetCounter();
    }

    public void ResetCounter()
    {
        if (_comboTimerCoroutine != null)
        {
            StopCoroutine(_comboTimerCoroutine);
            _comboTimerCoroutine = null;
        }

        _usedAbilities.Clear();
        _currentTarget = null;
    }

    #endregion

    #region Debuff и ComboState

    private void CastDebuff(Transform enemy, ScorpionAbility scorpionAbility)
    {
        if (enemy == null) return;

        switch (scorpionAbility)
        {
            case ScorpionAbility.Punch:
                Debug.Log("Debuff: Stun");
                enemy.GetComponent<HeroComponent>()?.CharacterState
                    .CmdAddState(States.Stun, 1f, 0, _hero.gameObject, "Punch");
                break;

            case ScorpionAbility.Kick:
                Debug.Log("Lava Pool");
                CmdSpawnLavaPool(enemy);
                break;

            case ScorpionAbility.ChainBlade:
                Debug.Log("ChainBlade Effect");
                break;
        }

        enemy.GetComponent<CharacterState>()
            ?.CmdAddState(States.ScorchedSoul, 6f, 100f, _hero.gameObject, nameof(PassiveCombo_Scorpion));
    }

    private void ApplyComboState(Character enemy)
    {
        var consumeCombo = _hero.GetComponent<ConsumeCombo_Scorpion>();
        if (consumeCombo == null)
        {
            Debug.LogWarning("ConsumeCombo_Scorpion не найден!");
            return;
        }

        consumeCombo.ApplyComboEffect(enemy.transform);
    }

    #endregion

    #region Network Commands

    [Command]
    private void CmdAdd()
    {
        _comboPlayer.Add(1);
    }

    [Command]
    private void CmdSpawnLavaPool(Transform enemy)
    {
        GameObject pool = Instantiate(_poolPrefab.gameObject, enemy.transform.position, Quaternion.identity);
        pool.transform.rotation *= Quaternion.Euler(90f, 0f, 0f);
        SceneManager.MoveGameObjectToScene(pool, _hero.NetworkSettings.MyRoom);

        pool.GetComponent<Sub_LavaPool_Scorpion>().Init();
        NetworkServer.Spawn(pool);
    }

    #endregion
}
