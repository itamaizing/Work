using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireBreath_Scorpion : Skill, ICanConsumeComboPoints
{
    [Header("Ability settings")]
    [SerializeField] private FireBreath_Prefab _conePrefab;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private string tagEnemies;
    [Header("Size")]

    [Header("Damage")]
    [SerializeField] private float _damage;
    [SerializeField] private float _damageRate;
    [SerializeField] private int _damageScalePerTick = 2;
    [SerializeField] private float _damagePercentStart;
    [SerializeField] private float _damagePercentEnd;

    private List<Health> _enemies = new List<Health>();
    private Dictionary<Health, int> _enemiesDikt = new Dictionary<Health, int>();

    
    [SerializeField] private FireBreath_Prefab _fireBreath;
    [SerializeField] private GameObject _fireObj;

    public ConsumeCombo_Scorpion Notifier { get; set; }
    public int ConsumedAmount { get; set; }

    protected override bool IsCanCast { get { return true; } }

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    private void Follow()
    {
        StartCoroutine(RotCor());
    }
    private IEnumerator RotCor()
    {
        Vector3 dir;
        float angle;
        while (_fireBreath != null)
        {
            dir = (Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position)).normalized;
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if(_fireBreath != null)
                CmdRotateWithSpeed(angle);
            yield return null;
        }
    }
    [Command]
    private void CmdCreateFireBreath(float angle)
    {
        var item = Instantiate(_prefab, transform.position, Quaternion.Euler(0, 0, angle - 90));
        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        item.transform.SetParent(transform);

        NetworkServer.Spawn(item/*, connectionToClient*/);

        SyncFire(item);
        _fireBreath = item.GetComponent<FireBreath_Prefab>();
        
        Destroy(_fireBreath.gameObject, /*_streamingDuration*/ CastStreamDuration);
    }

    [TargetRpc]
    public void SyncFire(GameObject obj)  
    { 
        _fireBreath = obj.GetComponent<FireBreath_Prefab>();

        Follow();
        Debug.Log("FireBreath присвоен");
    }

    [Command]
    private void CmdRotateWithSpeed(float angle)
    {
        if( _fireBreath != null )
            _fireBreath.transform.rotation = Quaternion.RotateTowards(_fireBreath.transform.rotation, Quaternion.Euler(0, 0, angle - 90), /*10f **/ 30f * Time.deltaTime);
        Debug.Log($"angle: {angle}");
    }

    [Command]
    private void CmdRotate(float angle)
    {
        if (_fireBreath != null)
        {
            _fireBreath.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
    }
    private IEnumerator FireBreath()
    {
        Hero.Move.CanMove = false;
        //PlayerMove.CanMove = false;

        float time = 0;
        _isCanCancle = false;
        //IsCanCancle = false;
        //PayCost();
        float damageValue = _damage;
        int damageCounter = 1;
        while (time < /*StreamingDuration*/ CastStreamDuration)
        {
            time += Time.deltaTime;

            if (time / damageCounter < _damageRate)
            {
                yield return null;
                continue;

            }
            _enemies.Clear();
            //_enemiesDikt.Clear();
            Debug.Log(_fireBreath);
            foreach (var item in _fireBreath._collisions)
            {
                if (item.TryGetComponent<Health>(out Health enemy) && item.transform.CompareTag(tagEnemies))
                {
                    _enemies.Add(enemy);
                    if (!_enemiesDikt.Keys.Contains(enemy))
                    {
                        _enemiesDikt.Add(enemy, 1);
                    }
                }
            }

            DoDamage(damageValue);
            damageCounter++;
            yield return null;
        }

        Hero.Move.CanMove = true;
        //PlayerMove.CanMove = true;
        ResetValue();
    }

    //protected override void Cancel()
    //{
        
    //}
    private void ResetValue()
    {
        _enemiesDikt.Clear();
        _fireBreath = null;
        //if (_fireBreath != null)
        //    Destroy(_fireBreath.gameObject);
    }
    private void CreateFireBreath()
    {
        _fireBreath = Instantiate(_conePrefab, transform);
    }

    private void DoDamage(float damageValue)
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            foreach (var flame in _fireBreath._flames)
            {
                if (!Physics2D.Raycast(transform.position, flame.transform.TransformDirection(Vector2.up), 4f, layerMask))
                {
                    float scale = CompareDistance(_enemies[i].transform);
                    int damageScale = _enemiesDikt[_enemies[i]];
                    //_enemies[i].CmdTryTakeDamage(damageValue * scale * damageScale, DamageType.Magical, AttackRangeType.RangeAttack);

                    Damage damage = new Damage
                    {
                        Value = Buff.Damage.GetBuffedValue(damageValue * scale * damageScale),
                        Type = DamageType,
                        Range = AttackRangeType,
                    };
                    CmdApplyDamage(damage, _enemies[i].gameObject);
                    _enemiesDikt[_enemies[i]] *= 2;
                    break;
                }
            }         
        }
    }

    private float CompareDistance(Transform enemy)
    {
        float scale;
        float distance;
        distance = Vector2.Distance(transform.position, enemy.transform.position);
        if (distance < 2.1f)
            return 1f;
        else
        {
            float distanceNormalised = (distance - 2f) / (4f - 2f);
            scale = Mathf.Lerp(1f, 0.7f, distanceNormalised);
           
            return scale;
        }
    }

    public void TryUpgradeByConsumingCombo(int amount)
    {
        if (!Notifier.IsActive)
        {
            ConsumedAmount = 0;
            return;
        }

        ConsumedAmount = Notifier.PayComboPoints(Mathf.Clamp(amount, 0, Notifier.AvailablePoints));
    }

    protected override IEnumerator PrepareJob()
    {
        while (true)
        {
            if (GetMouseButton)
            {
                break;
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        CmdCreateFireBreath(angle);
        StartCoroutine(FireBreath());

        yield return null;
    }

    protected override void ClearData()
    {
        _enemiesDikt.Clear();
        //_fireBreath = null;
    }
}
