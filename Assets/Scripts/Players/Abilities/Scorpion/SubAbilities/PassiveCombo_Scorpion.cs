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
    Blade
}
public class PassiveCombo_Scorpion : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private HeroComponent _hero;
    [SerializeField] private ComboPoints_Player _comboPlayer;
    //
    [SerializeField] private Sub_LavaPool_Scorpion _poolPrefab;

    [SerializeField] private float ChargesCD = 9f;

    public SyncList<ScorpionAbility> _syncList = new SyncList<ScorpionAbility>();

    [SerializeField][ReadOnly] private List<ScorpionAbility> _usedAbilities = new List<ScorpionAbility>(); // само комбо

    public Dictionary<ScorpionAbility, int> _abilityCharges = new Dictionary<ScorpionAbility, int>() // кол-во зарядов
    {
        {ScorpionAbility.Punch, 3}, { ScorpionAbility.Kick, 3}, { ScorpionAbility.Blade, 3}
    };

    private Dictionary<ScorpionAbility, Coroutine> _abilityCooldowns = new Dictionary<ScorpionAbility, Coroutine>() // перезарядки
    {
        {ScorpionAbility.Punch, null}, { ScorpionAbility.Kick, null}, { ScorpionAbility.Blade, null}
    };

    [Header("Visuals")]
    [SerializeField] private ParticleSystem _particlesAddStack;
    [SerializeField] private ParticleSystem _particlesNoCharges;
    [SerializeField] private ParticleSystem _particlesFullCombo;
    [SerializeField] private ParticleSystem _particlesCancelCombo;

    private void Start()
    {
        
    }

    [Command]
    private void CmdSyncList(ScorpionAbility scorpionAbility)
    {
        _syncList.Add(scorpionAbility);
    }

    public void AddAbility(/*PlayerLinks playerLinks,*/Transform enemy, ScorpionAbility scorpionAbility)
    {
        if(TryAddAbility(scorpionAbility) == false) 
        {
            Debug.LogWarning($"Не хватило зарядов {scorpionAbility} для добавления в комбо");
            // можно добавить импакт
            _particlesNoCharges.Play();
            return;
        }

        if(_usedAbilities.Count != 3)
        {
            // можно добавить импакт
            _particlesAddStack.Play();
            return;
        }

        if(_usedAbilities.Distinct().Count() <= 1)
        {
            _usedAbilities.Clear();
            // можно добавить импакт
            _particlesCancelCombo.Play();
            return;
        }

        Debug.LogWarning("Проверка прошла, комбо не из 3 одинаковых способностей");

        foreach (var ability in _usedAbilities.Distinct().ToList())
        {
            Debug.LogWarning($"Отнимаю у {ability} {_usedAbilities.Count(n => n == ability)} зарядов");
            UseCharge(ability, _usedAbilities.Count(n => n == ability));
        }

        //CmdAdd();
        _particlesFullCombo.Play();

        CastDebaff(enemy, _usedAbilities[_usedAbilities.Count - 1]);

        Debug.LogWarning($"Ability: {ScorpionAbility.Punch} - Charges {_abilityCharges[ScorpionAbility.Punch]}");
        Debug.LogWarning($"Ability: {ScorpionAbility.Kick} - Charges {_abilityCharges[ScorpionAbility.Kick]}");
        Debug.LogWarning($"Ability: {ScorpionAbility.Blade} - Charges {_abilityCharges[ScorpionAbility.Blade]}");

        _usedAbilities.Clear();
    }
    private bool TryAddAbility(ScorpionAbility ability)
    {
        int counter1 = _usedAbilities.Count(n => n == ability);

        if (counter1 + 1 <= _abilityCharges[ability])
        {
            _usedAbilities.Add(ability);
            CmdSyncList(ability);
            return true;
        }

        return false;
    }
    private void UseCharge(ScorpionAbility ability, int value)
    {
        _abilityCharges[ability] -= value;
        //StartCoroutine(TimerCD(ability));
        if (_abilityCooldowns[ability] == null)
        {
            Debug.LogWarning($"Начали кд {ability}");
            _abilityCooldowns[ability] = StartCoroutine(TimerCD(ability));
        }
    }

    public void ResetCounter()
    {
        _usedAbilities.Clear();
    }

    private void CastDebaff(Transform enemy, ScorpionAbility scorpionAbility) // поменять потом transform, на просто ссылку на самого игрока
    {
        // применяем на врага дебаф 
        CmdAdd();

        switch (scorpionAbility) // доп действие в зависимости от способности, которая прокнула дебаф
        {
            case ScorpionAbility.Punch:
                Debug.Log("punch");

                enemy.GetComponent<HeroComponent>().CharacterState.CmdAddState(States.Stun, 1f, 0, GetComponentInParent<Character>().gameObject, "Punch");

                break;

            case ScorpionAbility.Kick:
                Debug.Log("Kick");
                CmdSpawnLavaPool(enemy);
                break;

            default: 
                break;
        }

        //ServerDebuff(enemy);
        //cmdBuff(enemy);
        enemy.GetComponent<CharacterState>().CmdAddState(States.ScorchedSoul, 6f, 100f, _hero.gameObject, name);
    }
    //[Server]
    //private void ServerDebuff(Transform enemy)
    //{
    //    //transform.parent.parent.GetComponent<CharacterState>().AddState(new ScorchedSoulDebuff(), 6f, 0, States.ScorchedSoul);
    //    enemy.GetComponent<CharacterState>().CmdAddState(States.ScorchedSoul, 6f, 100f);
    //}


    //[Command]
    //private void cmdBuff(Transform enemy)
    //{
    //    enemy.GetComponent<CharacterState>().AddState(new ScorchedSoulDebuff(), 6f, 100f, States.ScorchedSoul);
    //    RpcBuff(enemy);
    //}
    //[ClientRpc]
    //private void RpcBuff(Transform enemy)
    //{
    //    enemy.GetComponent<CharacterState>().AddState(new ScorchedSoulDebuff(), 6f, 0, States.ScorchedSoul);
    //}

    [Command]
    private void CmdAdd()
    {
        _comboPlayer.Add(1);

    }

    [Command]
    private void CmdSpawnLavaPool(Transform enemy)
    {
        GameObject pool = Instantiate(_poolPrefab.gameObject, enemy.transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(pool, _hero.NetworkSettings.MyRoom);

        pool.GetComponent<Sub_LavaPool_Scorpion>().Init();

        NetworkServer.Spawn(pool);
    }

    private IEnumerator TimerCD(ScorpionAbility ability)
    {   
        while (_abilityCharges[ability] < 3)
        {
            float time = 0f;

            while (time < ChargesCD)
            {
                time += Time.deltaTime;
                yield return null;
            }
            _abilityCharges[ability]++;
            Debug.LogWarning($"1 Заряд {ability} восстановился, сейчас их {_abilityCharges[ability]}");
            
        }
        _abilityCooldowns[ability] = null;
        Debug.LogWarning($"Таймер {ability} остановился");
    }
}
