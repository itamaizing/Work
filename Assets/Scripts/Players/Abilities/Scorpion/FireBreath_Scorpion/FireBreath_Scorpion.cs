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

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

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
        var item = Instantiate(_prefab, transform.position, Quaternion.Euler(0, angle, 0));
        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        item.transform.SetParent(transform);

        NetworkServer.Spawn(item);

        _fireBreath = item.GetComponent<FireBreath_Prefab>();

        SyncFire(item);

        Destroy(_fireBreath.gameObject, CastStreamDuration);
    }

    [TargetRpc]
    public void SyncFire(GameObject obj)  
    { 
        _fireBreath = obj.GetComponent<FireBreath_Prefab>();

        Follow();
        Debug.Log("FireBreath ��������");
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
        _isCanCancle = false;

        float time = 0;
        int damageCounter = 1;

        while (time < CastStreamDuration)
        {
            time += Time.deltaTime;

            if (time / damageCounter < _damageRate)
            {
                yield return null;
                continue;
            }

            _enemies.Clear();

            Collider[] hitColliders = Physics.OverlapSphere(_fireBreath.transform.position, Radius, layerMask);

            foreach (var collider in hitColliders)
            {
                if (collider.TryGetComponent<Health>(out Health enemy) && enemy.CompareTag(tagEnemies))
                {
                    Vector3 directionToEnemy = (enemy.transform.position - _fireBreath.transform.position).normalized;
                    if (!Physics.Raycast(_fireBreath.transform.position, directionToEnemy, Vector3.Distance(_fireBreath.transform.position, enemy.transform.position), layerMask))
                    {
                        _enemies.Add(enemy);
                        if (!_enemiesDikt.ContainsKey(enemy))
                        {
                            _enemiesDikt[enemy] = 1;
                        }
                    }
                }
            }

            DoDamage(_damage);
            damageCounter++;
            yield return null;
        }

        Hero.Move.CanMove = true;
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

    private void DoDamage(float baseDamage)
    {
        if (_fireBreath == null || _enemies.Count == 0)
            return;

        foreach (var enemy in _enemies)
        {
            if (enemy == null) continue;

            float distanceMultiplier = CompareDistance(enemy.transform);
            int damageScale = _enemiesDikt.ContainsKey(enemy) ? _enemiesDikt[enemy] : 1;

            float finalDamageValue = Buff.Damage.GetBuffedValue(baseDamage * distanceMultiplier * damageScale);

            Damage damage = new Damage
            {
                Value = finalDamageValue,
                Type = DamageType,
            };

            CmdApplyDamage(damage, enemy.gameObject);

            if (_enemiesDikt.ContainsKey(enemy))
            {
                _enemiesDikt[enemy] *= 2;
            }
            else
            {
                _enemiesDikt[enemy] = 2;
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
