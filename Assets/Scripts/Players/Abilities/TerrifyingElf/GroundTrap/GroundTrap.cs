using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GroundTrap : Skill
{
    [SerializeField] private Trap trap;
    [SerializeField] private GameObject previewTrap;
    [SerializeField] private HeroComponent heroComponent;

    private bool _isPlacingTrap = false;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private bool _isStartPointPlaced = false;
    private float _trapAngle = 0f;

    protected override bool IsCanCast => !_isPlacingTrap || (_isPlacingTrap && _isStartPointPlaced);

    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _hero.Animator.speed = CastDeley * 2;
        _isPlacingTrap = true;
        _isStartPointPlaced = false;
        previewTrap.SetActive(true);
        Hero.Move.CanMove = false;

        while (!_isStartPointPlaced)
        {
            Vector3 mousePosition = GetMousePoint();
            float distance = Vector3.Distance(mousePosition, transform.position);

            if (distance >= 2 && distance <= Radius)
            {
                previewTrap.transform.position = mousePosition;

                Vector3 directionToHero = mousePosition - transform.position;
                _trapAngle = Mathf.Atan2(directionToHero.x, directionToHero.z) * Mathf.Rad2Deg;

                previewTrap.transform.rotation = Quaternion.Euler(-90, _trapAngle, 0);

                if (GetMouseButton)
                {
                    _startPosition = mousePosition;
                    PlaceStartPoint();

                    _hero.Animator.SetTrigger("ShotCastDelayAnimTrigger");
                    _hero.NetworkAnimator.SetTrigger("ShotCastDelayAnimTrigger");
                    yield return new WaitForSeconds(CastDeley);
                }
            }

            yield return null;
        }

        while (true)
        {
            Vector3 mousePositionSecond = GetMousePoint();
            _endPosition = mousePositionSecond;

            Vector3 direction = _endPosition - _startPosition;
            _trapAngle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg + 90;

            previewTrap.transform.rotation = Quaternion.Euler(-90, -_trapAngle, 0);

            float distanceBetweenPoints = Vector3.Distance(_endPosition, _startPosition);

            if (distanceBetweenPoints <= 2 && GetMouseButton)
            {
                _endPosition = mousePositionSecond;
                break;
            }

            yield return null;
        }
    }


    protected override IEnumerator CastJob()
    {
        PlaceTrap();
        _hero.Animator.speed = 1;
        yield return null;
    }

    private void PlaceStartPoint()
    {
        _isStartPointPlaced = true;
        Hero.Move.LookAtTransform(previewTrap.transform);
        previewTrap.transform.position = _startPosition;
    }

    private void PlaceTrap()
    {
        _isPlacingTrap = false;
        _isStartPointPlaced = false;
        previewTrap.SetActive(false);
        Hero.Move.StopLookAt();
        Hero.Move.CanMove = true;

        CmdSpawnTrap(_trapAngle, _startPosition, _endPosition);
    }

    [Command]
    private void CmdSpawnTrap(float angle, Vector3 startPosition, Vector3 endPosition)
    {
        Trap trapInstance = Instantiate(trap, startPosition, Quaternion.Euler(-90, -angle, 0));
        SceneManager.MoveGameObjectToScene(trapInstance.gameObject, Hero.NetworkSettings.MyRoom);
        trapInstance.Init(heroComponent, this, startPosition, endPosition);
        NetworkServer.Spawn(trapInstance.gameObject);

        RpcInitTrap(trapInstance.gameObject, startPosition, endPosition);
    }

    [ClientRpc]
    private void RpcInitTrap(GameObject trapObject, Vector3 startPosition, Vector3 endPosition)
    {
        trapObject.GetComponent<Trap>().Init(heroComponent, this, startPosition, endPosition);
    }

    protected override void ClearData()
    {
        _isPlacingTrap = false;
        _isStartPointPlaced = false;
        previewTrap.SetActive(false);
        Hero.Move.CanMove = true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }
}