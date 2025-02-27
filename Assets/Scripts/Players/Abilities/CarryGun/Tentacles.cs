using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class Tentacles : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private TentacleProjectile tentaclesPrefab;
    [SerializeField] private TentacleProjectile tentaclesPreview;

    private bool _isPlacingTentacles = false;
    private Vector3 _spawnPoint = Vector3.positiveInfinity;
    private Character _target;
    private TentacleProjectile _previewInstance;
    private Coroutine _radiusUpdateCoroutine;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity && _target != null;

    protected override void ClearData()
    {
        _isPlacingTentacles = false;
        _spawnPoint = Vector3.positiveInfinity;
        _target = null;
        Hero.Move.CanMove = true;

        if (_previewInstance != null) Destroy(_previewInstance.gameObject);

        if (_radiusUpdateCoroutine != null)
        {
            StopCoroutine(_radiusUpdateCoroutine);
            _radiusUpdateCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        Vector3 mousePositionStart = GetMousePoint();

        _previewInstance = Instantiate(tentaclesPreview, mousePositionStart, Quaternion.identity);
        _previewInstance.HeroToTentacle = _player;

        _skillRender.DrawRadius(_radius);
        _radiusUpdateCoroutine = StartCoroutine(UpdateRadiusColor());

        while (_target == null)
        {
            Vector3 mousePosition = GetMousePoint();
            float distance = Vector3.Distance(mousePosition, transform.position);

            _previewInstance.transform.position = mousePosition;

            if (GetMouseButton && distance <= Radius)
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitTarget))
                {
                    if (hitTarget.collider.TryGetComponent<Character>(out Character character))
                    {
                        _target = character;
                        _previewInstance.transform.SetParent(_target.transform);
                        yield return new WaitForSeconds(1f);
                        break;
                    }
                }
            }

            yield return null;
        }

        while (true)
        {
            if (GetMouseButton)
            {
                Vector3 mousePosition = GetMousePoint();
                float distanceToTarget = Vector3.Distance(mousePosition, _target.transform.position);
                float distanceToCaster = Vector3.Distance(mousePosition, transform.position);

                if (distanceToTarget <= 3f && distanceToCaster <= Radius)
                {
                    _spawnPoint = mousePosition;
                    break;
                }
            }

            yield return null;
        }

        if (_previewInstance != null) Destroy(_previewInstance.gameObject);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null) CmdSpawnTentacles(_spawnPoint, _target);

        ClearData();
        yield return null;
    }

    private IEnumerator UpdateRadiusColor()
    {
        while (true)
        {
            bool isPreviewInsideRadius = false;

            if (_previewInstance != null)
            {
                float distanceToPreview = Vector3.Distance(transform.position, _previewInstance.transform.position);
                isPreviewInsideRadius = distanceToPreview <= _radius;
            }

            _skillRender.DrawRadiusColor(_radius, isPreviewInsideRadius ? Color.green : Color.red);
            yield return new WaitForSeconds(0.1f);
        }
    }

    [Command]
    private void CmdSpawnTentacles(Vector3 position, Character target)
    {
        TentacleProjectile tentacles = Instantiate(tentaclesPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(tentacles.gameObject, _hero.NetworkSettings.MyRoom);

        tentacles.Init(_player, target, position, target.transform.position, true, 0);

        NetworkServer.Spawn(tentacles.gameObject);
        RpcInitTentacles(tentacles.gameObject, target, position);
    }

    [ClientRpc]
    private void RpcInitTentacles(GameObject tentacleObject, Character target, Vector3 position)
    {
        tentacleObject.GetComponent<TentacleProjectile>().Init(_player, target, position, target.transform.position, true, 0);
    }
}
