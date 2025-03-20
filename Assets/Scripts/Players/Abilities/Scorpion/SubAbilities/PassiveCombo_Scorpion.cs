using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;
using UnityEngine.SceneManagement;

public class PassiveCombo_Scorpion : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private HeroComponent _hero;
    [SerializeField] private ComboPoints_Player _comboPlayer;

    [SerializeField] private Sub_LavaPool_Scorpion _poolPrefab;

    [Header("Skills Reference")]
    [SerializeField] private List<Skill> _skills = new();

    [Header("Combo Settings")]
    private List<Skill> _usedSkills = new();
    private Character _currentTarget;
    [SerializeField] private float _comboTimeout = 1f;
    private Coroutine _comboTimerCoroutine;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem _particlesAddStack;
    [SerializeField] private ParticleSystem _particlesNoCharges;
    [SerializeField] private ParticleSystem _particlesFullCombo;
    [SerializeField] private ParticleSystem _particlesCancelCombo;

    #region Add Skill (Комбо механика)

    public void AddSkill(Character enemy, Skill skill)
    {
        if (enemy == null || skill == null)
        {
            Debug.LogWarning("Цель или скилл пустые");
            return;
        }

        if (_currentTarget == null)
            _currentTarget = enemy;

        if (_currentTarget != enemy)
        {
            Debug.Log("Цель изменилась. Сброс комбо.");
            ResetCounter();
            _currentTarget = enemy;
        }

        if (!TryAddSkill(skill))
        {
            Debug.LogWarning($"Нет зарядов у {skill.name}");
            _particlesNoCharges?.Play();
            return;
        }

        if (_usedSkills.Count != 3)
        {
            Debug.Log($"Добавили {skill.name} в связку");
            _particlesAddStack?.Play();
            StartOrRestartComboTimer();
            return;
        }

        if (_usedSkills.Distinct().Count() <= 1)
        {
            Debug.LogWarning("Связка из одного типа скиллов! Комбо отменено.");
            _usedSkills.Clear();
            _particlesCancelCombo?.Play();
            return;
        }

        foreach (var uniqueSkill in _usedSkills.Distinct().ToList())
        {
            int count = _usedSkills.Count(s => s == uniqueSkill);
            Debug.Log($"Успешная связка! {uniqueSkill.name} используется {count} раз. Списываем заряды...");
            UseCharges(uniqueSkill, count);
        }

        _particlesFullCombo?.Play();

        CastDebuff(enemy.transform, _usedSkills.Last());
        ApplyComboState(enemy);

        AddComboPoint();

        ResetCounter();
    }

    private bool TryAddSkill(Skill skill)
    {
        int availableCharges = skill.Chargers;
        int currentUsage = _usedSkills.Count(s => s == skill);

        Debug.Log($"Проверка добавления {skill.name}. Зарядов доступно: {availableCharges}, уже использовано в серии: {currentUsage}");

        if (currentUsage + 1 <= availableCharges)
        {
            _usedSkills.Add(skill);
            StartOrRestartComboTimer();
            return true;
        }

        Debug.LogWarning($"Нет доступных зарядов для {skill.name}. {currentUsage + 1}/{availableCharges}");
        return false;
    }

    [ClientRpc]
    private void UseCharges(Skill skill, int amount)
    {
        if (skill == null) return;

        for (int i = 0; i < amount; i++)
        {
            bool success = skill.TryUseCharge();
            Debug.Log($"Попытка списать заряд {i + 1}/{amount} у {skill.name}. Успех: {success}. Осталось зарядов: {skill.Chargers}");
        }
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

        Debug.Log("Таймаут комбо! Сброс связки.");
        ResetCounter();
    }

    public void ResetCounter()
    {
        if (_comboTimerCoroutine != null)
        {
            StopCoroutine(_comboTimerCoroutine);
            _comboTimerCoroutine = null;
        }

        Debug.Log("Сброс текущей серии комбо");
        _usedSkills.Clear();
        _currentTarget = null;
    }

    #endregion

    #region Debuff и ComboState

    private void CastDebuff(Transform enemy, Skill lastSkillUsed)
    {
        if (enemy == null || lastSkillUsed == null) return;

        if (lastSkillUsed == GetSkillByName("Punch"))
        {
            Debug.Log("Debuff: Stun");
            //enemy.GetComponent<CharacterState>()?.AddState(States.Stun, 1f, 0, _hero.gameObject, "Punch");
        }
        else if (lastSkillUsed == GetSkillByName("Kick"))
        {
            Debug.Log("Lava Pool");
            SpawnLavaPool(enemy);
        }
        else if (lastSkillUsed == GetSkillByName("ChainBlade"))
        {
            Debug.Log("ChainBlade Effect");
        }

        //enemy.GetComponent<CharacterState>()?.AddState(States.ScorchedSoul, 6f, 100f, _hero.gameObject, nameof(PassiveCombo_Scorpion));
    }

    private void ApplyComboState(Character enemy)
    {
        var consumeCombo = _hero.GetComponent<ConsumeCombo_Scorpion>();
        if (consumeCombo == null)
        {
            Debug.LogWarning("ConsumeCombo_Scorpion не найден!");
            return;
        }

        Debug.Log("Применение состояния ComboState к цели");
        consumeCombo.ApplyComboEffect(enemy.transform);
    }

    #endregion

    #region Network Commands

    private void AddComboPoint()
    {
        Debug.Log("Добавлен 1 очко комбо игроку");
        _comboPlayer.Add(1);
    }

    private void SpawnLavaPool(Transform enemy)
    {
        GameObject pool = Instantiate(_poolPrefab.gameObject, enemy.transform.position, Quaternion.identity);
        pool.transform.rotation *= Quaternion.Euler(90f, 0f, 0f);

        SceneManager.MoveGameObjectToScene(pool, _hero.NetworkSettings.MyRoom);

        pool.GetComponent<Sub_LavaPool_Scorpion>().Init();
        NetworkServer.Spawn(pool);
    }

    #endregion

    #region Вспомогательные методы

    private Skill GetSkillByName(string name)
    {
        return _skills.FirstOrDefault(s => s != null && s.name == name);
    }

    #endregion
}
