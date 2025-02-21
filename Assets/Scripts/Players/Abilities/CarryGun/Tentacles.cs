using Mirror;
using System.Collections;
using UnityEngine;
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
    }

    protected override IEnumerator PrepareJob()
    {
        _isPlacingTentacles = true;
        Vector3 mousePositionStart = GetMousePoint();
        _previewInstance = Instantiate(tentaclesPreview, mousePositionStart, Quaternion.identity);

        while (float.IsPositiveInfinity(_spawnPoint.x) && !_disactive)
        {
            Vector3 mousePosition = GetMousePoint();
            float distance = Vector3.Distance(mousePosition, transform.position);

            _previewInstance.transform.position = mousePosition;

            if (GetMouseButton && distance <= Radius)
            {
                _spawnPoint = GetMousePoint();
                Hero.Move.CanMove = false;
                break;
            }

            yield return null;
        }

        while (_target == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPosition = GetMousePoint();
                float distanceTarget = Vector3.Distance(clickPosition, _spawnPoint);

                if (distanceTarget <= 3f)
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitTarget))
                    {
                        if (hitTarget.collider.TryGetComponent<Character>(out Character character))
                        {
                            _target = character;
                            break;
                        }
                    }
                }
            }

            yield return null;
        }

        if (_previewInstance != null) Destroy(_previewInstance.gameObject);

    }

    protected override IEnumerator CastJob()
    {
        if (_target != null) CmdSpawnTentacles(_spawnPoint, _target.gameObject);

        ClearData();
        yield return null;
    }

    [Command]
    private void CmdSpawnTentacles(Vector3 position, GameObject target)
    {
        TentacleProjectile tentacles = Instantiate(tentaclesPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(tentacles.gameObject, _hero.NetworkSettings.MyRoom);

        tentacles.Init(_player.gameObject, target, position, target.transform.position, true, 0);

        NetworkServer.Spawn(tentacles.gameObject);
        RpcInitTentacles(tentacles.gameObject, target);
    }

    [ClientRpc]
    private void RpcInitTentacles(GameObject tentacleObject, GameObject target)
    {
        tentacleObject.GetComponent<TentacleProjectile>().Init(_player.gameObject, target, _spawnPoint, target.transform.position, true, 0);
    }
}
