using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Tentacles : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private TentacleProjectile tentaclesPrefab;
    [SerializeField] private TentacleProjectile tentaclesPreview;
    [SerializeField] private LayerMask groundLayer;

    private bool _isPlacingTentacles = false;
    private Vector3 _spawnPoint = Vector3.positiveInfinity;
    private HashSet<Character> _charactersInPreview = new HashSet<Character>();
    private Character _target;
    private TentacleProjectile _previewInstance;
    private TentacleProjectile _previewInstancePrefab;
    private Coroutine _radiusUpdateCoroutine;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("Spell");
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _spawnPoint != Vector3.positiveInfinity && _target != null;

    protected override void ClearData()
    {
        _skillRender.IsOverrideClosestTarget = false;

        _isPlacingTentacles = false;
        _spawnPoint = Vector3.positiveInfinity;
        _target = null;
        Hero.Move.CanMove = true;
        _player.Move.StopLookAt();

        if (_previewInstance != null) Destroy(_previewInstance.gameObject);
        if (_previewInstancePrefab != null) Destroy(_previewInstancePrefab.gameObject);

        if (_radiusUpdateCoroutine != null)
        {
            StopCoroutine(_radiusUpdateCoroutine);
            _radiusUpdateCoroutine = null;
        }
    }

    private bool IsValidVector(Vector3 vector)
    {
        return !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z) ||
                 float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z));
    }

    protected override IEnumerator PrepareJob()
    {
        _skillRender.IsOverrideClosestTarget = true;

        Vector3 mousePositionStart = GetMousePoint();

        _previewInstance = Instantiate(tentaclesPreview, mousePositionStart, Quaternion.identity);
        _skillRender.DrawRadius(_radius);
        _radiusUpdateCoroutine = StartCoroutine(UpdateRadiusColor());

        while (_target == null)
        {
            Vector3 mousePosition = GetMousePoint();
            float distance = Vector3.Distance(mousePosition, transform.position);

            _previewInstance.transform.position = mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitTarget))
                {
                    if (hitTarget.collider.TryGetComponent<Character>(out Character character))
                    {
                        _target = character;
                        _previewInstance.transform.SetParent(_target.transform);

                        _previewInstancePrefab = Instantiate(tentaclesPreview, _previewInstance.transform.position, Quaternion.identity);
                        _previewInstancePrefab.Tentacle.SetActive(true);
                        _previewInstancePrefab.IsPreview = false;
                        break;
                    }
                }
            }

            yield return null;
        }

        while (true)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            float sphereRadius = 0.1f;
            float maxDistance = 100f;

            if (GetMouseButton)
            {
                if (Physics.SphereCast(ray, sphereRadius, out hit, maxDistance, _obstacle))
                {
                    yield return null;
                    continue;
                }

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, TargetsLayers))
                {
                    if (hit.collider.TryGetComponent<Character>(out Character clickedCharacter))
                    {
                        Vector3 targetPosition = clickedCharacter.transform.position;

                        if (!IsValidVector(targetPosition))
                        {
                            yield return null;
                            continue;
                        }

                        _spawnPoint = targetPosition;
                        _player.Move.LookAtTransform(_target.transform);
                        Hero.Move.CanMove = false;
                        break;
                    }
                }

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
                {
                    Vector3 groundPoint = hit.point;

                    Vector3 direction = groundPoint - _previewInstance.transform.position;
                    float distanceToCaster = direction.magnitude;

                    if (distanceToCaster > _previewInstance.Radius)
                        direction = direction.normalized * _previewInstance.Radius;

                    if (_previewInstancePrefab != null)
                        _previewInstancePrefab.transform.position = _previewInstance.transform.position + direction;

                    float distanceToTarget = Vector3.Distance(_previewInstancePrefab.transform.position, transform.position);

                    if (distanceToTarget <= Radius)
                    {
                        Vector3 potentialSpawnPoint = _previewInstancePrefab.transform.position;

                        if (!IsValidVector(potentialSpawnPoint))
                        {
                            yield return null;
                            continue;
                        }

                        _spawnPoint = potentialSpawnPoint;
                        _player.Move.LookAtTransform(_target.transform);
                        Hero.Move.CanMove = false;
                        break;
                    }
                }
            }

            if (_previewInstancePrefab != null)
            {
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
                {
                    Vector3 hoverPoint = hit.point;

                    Vector3 dir = hoverPoint - _previewInstance.transform.position;
                    float distance = dir.magnitude;

                    if (distance > _previewInstance.Radius)
                        dir = dir.normalized * _previewInstance.Radius;

                    _previewInstancePrefab.transform.position = _previewInstance.transform.position + dir;
                }
            }

            yield return null;
        }

        if (_previewInstance != null) Destroy(_previewInstance.gameObject);
    }

    protected override IEnumerator CastJob()
    {
        if (!IsValidVector(_spawnPoint)) yield break;
        if (_target != null) CmdSpawnTentacles(_spawnPoint, _target);

        ClearData();
        _skillRender.StopDrawRadius();
        yield return null;
    }

    private IEnumerator UpdateRadiusColor()
    {
        while (true)
        {
            bool isPreviewInsideRadius = false;
            bool isCharacterInsidePreview = false;

            HashSet<Character> newCharactersInPreview = new HashSet<Character>();

            if (_previewInstance != null)
            {
                Collider[] hitColliders = Physics.OverlapSphere(_previewInstance.transform.position, Area + 500);

                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.TryGetComponent<Character>(out Character character) && character != _player)
                    {
                        float distanceToCharacter = Vector3.Distance(_previewInstance.transform.position, character.transform.position);

                        if (distanceToCharacter <= Area)
                        {
                            isCharacterInsidePreview = true;
                            character.SelectedCircle.SwitchClostestTarget(true);
                            character.SelectedCircle.SetColorTarget(Color.green);
                        }
                        else
                        {
                            character.SelectedCircle.SwitchClostestTarget(false);
                            character.SelectedCircle.SetColorTarget(Color.red);
                        }

                        newCharactersInPreview.Add(character);
                    }
                }

                float distanceToPreview = Vector3.Distance(transform.position, _previewInstance.transform.position);
                isPreviewInsideRadius = distanceToPreview <= _radius;
            }

            if (_previewInstancePrefab != null)
            {
                float distanceToPreview = Vector3.Distance(transform.position, _previewInstancePrefab.transform.position);
                isPreviewInsideRadius = distanceToPreview <= _radius;
            }

            _previewInstance.SetRadiusColor(isCharacterInsidePreview ? Color.green : Color.red);
            _skillRender.DrawRadiusColor(_radius, isPreviewInsideRadius ? Color.green : Color.red);

            _charactersInPreview = newCharactersInPreview;

            yield return new WaitForSeconds(0.1f);
        }
    }

    [Command]
    private void CmdSpawnTentacles(Vector3 position, Character target)
    {
        if (!IsValidVector(position)) return;

        if (target == null) return;

        TentacleProjectile tentacles = Instantiate(tentaclesPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(tentacles.gameObject, _hero.NetworkSettings.MyRoom);

        tentacles.Init(_player, target, position, target.transform.position, true, 0, this);

        NetworkServer.Spawn(tentacles.gameObject);
        RpcInitTentacles(tentacles.gameObject, target, position);

        if (_radiusUpdateCoroutine != null)
        {
            StopCoroutine(_radiusUpdateCoroutine);
            _radiusUpdateCoroutine = null;
        }
    }

    [ClientRpc]
    private void RpcInitTentacles(GameObject tentacleObject, Character target, Vector3 position)
    {
        if (!IsValidVector(position)) return;
        if (tentacleObject == null) return;

        tentacleObject.GetComponent<TentacleProjectile>().Init(_player, target, position, target.transform.position, true, 0, this);
    }
}
