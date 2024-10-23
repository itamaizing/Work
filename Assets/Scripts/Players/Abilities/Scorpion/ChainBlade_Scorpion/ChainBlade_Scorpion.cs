using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ChainbladeType
{
    Default,
    Hook
}
public class ChainBlade_Scorpion : Skill
{
    [Header("Ability settings")]
    [SerializeField][Range(0, 100)] private float _minDamage = 3f;
    [SerializeField][Range(0, 100)] private float _maxDamage = 5f;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private NetworkIdentity _playerIdentity;

    [SerializeField] private float _range;
    [SerializeField] private ChainController _chainPrefab;
    private ChainController _chain;

    [SerializeField] private GameObject _projectilePrefab;
    private GameObject _projectile;
    private BladeProjectile _blade;

    private GameObject enemy;
    private bool bladeDestroyed = false;
    private ChainbladeType _type;

    private GameObject _tempTarget;
    private MoveComponent _tempTargetMove;

    public float DamageRange => Random.Range(_minDamage, _maxDamage);

    protected override bool IsCanCast
    {
        get
        {
            return true;
        }
    }

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    private IEnumerator PullEnemy(GameObject enemy)
    {
        float distance = Vector2.Distance(transform.position, enemy.transform.position);
        enemy.GetComponent<MoveComponent>().CanMove = false;

        while (distance >= 2.5f)
        {
            Debug.Log("Pulling");
            //enemy.transform.position = Vector2.MoveTowards(enemy.transform.position, transform.position, 10f * Time.deltaTime);

            Pull(enemy, (/*enemy.transform.position - transform.position*/transform.position - enemy.transform.position).normalized * 10f * Time.deltaTime);
            distance = Vector2.Distance(transform.position, enemy.transform.position);

            yield return null;
        }
        enemy.GetComponent<Character>().Move.CanMove = true;
        Destroy(_chain.gameObject);
    }

    private IEnumerator ReturnBlade(Transform bladeTransform, GameObject chainGameObject)
    {
        _hero.Move.CanMove = true;
        //_blade._rb.isKinematic = true;
        //_blade._rb.velocity = (transform.position - _projectile.transform.position).normalized * 20f;

        while (Vector2.Distance(transform.position, bladeTransform.position) > 2.9f)
        {
            /*_blade*/bladeTransform.GetComponent<BladeProjectile>()._rb.velocity = (transform.position - bladeTransform.position).normalized * 20f;
            yield return null;
        }
          
        Destroy(chainGameObject);
        Destroy(bladeTransform.gameObject);
    }

    [Command]
    private void CmdCreateProjectile(float maxDistance, Vector2 direction, GameObject parent, ChainbladeType type)
    {
        //blade spawn
        _projectile = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(_projectile, _hero.NetworkSettings.MyRoom);

        _blade = _projectile.GetComponent<BladeProjectile>();
        _blade.Init(maxDistance, direction, parent, type);

        NetworkServer.Spawn(_projectile);

        //Damage
        _projectile.GetComponent<BladeProjectile>().OnHit.AddListener(target =>
        {
            if (target == null)
                return;

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(DamageRange),
                Type = DamageType,
                Range = AttackRangeType,
            };

            //CmdApplyDamage(damage, target.gameObject);
            DealDamage(damage, target.gameObject);
        });

        //Hook
        if (type == ChainbladeType.Hook)
        {
            _hero.Move.CanMove = false;
            _projectile.GetComponent<BladeProjectile>().OnHit.AddListener(target =>
            {
                if (target != null)
                {
                    enemy = target;
                    _chain.targetID = enemy.GetComponent<NetworkIdentity>().netId;
                    NetworkServer.Destroy(_blade.gameObject);
                    StartCoroutine(PullEnemy(enemy));
                }
                else
                {
                    StartCoroutine(ReturnBlade(_projectile.transform, _chain.gameObject));
                }
                bladeDestroyed = true;
            });

            //chain spawn
            GameObject item = Instantiate(_chainPrefab.gameObject);
            SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);
            _chain = item.GetComponent<ChainController>();
            
            NetworkServer.Spawn(item);
            _chain.targetID = _blade.netId;
            _chain.parentID = _playerLinks.GetComponent<NetworkIdentity>().netId;
        }
    }

    [Command]
    private void CmdThrowBlade(Vector2 direction)
    {
        _projectile.GetComponent<BladeProjectile>().ThrowBlade(direction);
    }

    private void ResetValue()
    {
        bladeDestroyed = false;
        _hero.Move.CanMove = true;
    }

    protected override IEnumerator PrepareJob()
    {
        while(true)
        {
            if (GetMouseButton)
            {
                break;
            }
            yield return null;
        }

        if (_playerLinks.Stamina.CurrentValue >= 40)
        {
            _type = ChainbladeType.Hook;
            _skillEnergyCosts[0].resourceCost = 40;
        }
        else
        {
            _type = ChainbladeType.Default;
            _skillEnergyCosts[0].resourceCost = 10;
        }

        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        Vector2 dir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;

        CmdCreateProjectile(8f, dir, this.gameObject, _type); // <--- event hit is here

        CmdThrowBlade(dir);

        yield return null;
    }

    protected override void ClearData()
    {
        
    }

    private void  DealDamage(Damage damage, GameObject hp)
    {
        if (_tempTargetForDamage != hp.transform)
        {
            _tempTargetForDamage = hp.transform;
            _tempHPForDamage = hp.GetComponent<Health>();
        }
        _tempHPForDamage.TryTakeDamage(ref damage, this);
    }


    private void Pull(GameObject gameObject, Vector2 force) // called in [command]
    {
        if (_tempTarget != gameObject)
        {
            _tempTarget = gameObject;
            _tempTargetMove = gameObject.GetComponent<MoveComponent>();
        }
        _tempTargetMove.TargetRpcAddTransformPosition(force);
    }
}
