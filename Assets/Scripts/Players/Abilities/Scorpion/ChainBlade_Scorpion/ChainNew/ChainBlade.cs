using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class ChainBlade : Skill
{
    [SerializeField] [Range(0, 100)] private float _minDamage = 3f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 5f;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;

    [SerializeField] private ChainArrow chainArrowPrefab;
    [SerializeField] private HeroComponent playerLinks;

    private ChainArrow _chainArrowPrefab;
    private Vector3 _clickPoint;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _clickPoint != Vector3.zero;

    public float DamageRange => Random.Range(_minDamage, _maxDamage);
    public PassiveCombo_Scorpion ComboCounter { get => _comboCounter; set => _comboCounter = value; }

    protected override IEnumerator PrepareJob()
    {
        while (true)
        {
            if (GetMouseButton)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    _clickPoint = hit.point;

                    if (IsPointInRadius(Radius, _clickPoint))
                    {
                        Hero.Move.LookAtPosition(_clickPoint);
                        Hero.Move.CanMove = false;
                        break;
                    }
                }
            }

            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        CmdSpawnChainArrow(_clickPoint);
        yield return null;
    }

    protected override void ClearData()
    {
        _clickPoint = Vector3.zero;
    }

    [Command]
    private void CmdSpawnChainArrow(Vector3 clickPoint)
    {

        Vector3 direction = (clickPoint - playerLinks.transform.position).normalized;
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
        Vector3 targetPoint = playerLinks.transform.position + flatDirection * Radius;

        var arrow = Instantiate(chainArrowPrefab, playerLinks.transform.position, Quaternion.identity);
        _chainArrowPrefab = arrow;
        arrow.Init(playerLinks, 0, false, this);

        NetworkServer.Spawn(arrow.gameObject);
        SceneManager.MoveGameObjectToScene(arrow.gameObject, _hero.NetworkSettings.MyRoom);

        arrow.InitArrow(targetPoint, Hero.transform, Radius, DamageRange);
        RpcInitArrow(arrow.gameObject, targetPoint);
    }

    [ClientRpc]
    private void RpcInitArrow(GameObject arrowObj, Vector3 targetPoint)
    {
        if (arrowObj == null) return;

        var arrow = arrowObj.GetComponent<ChainArrow>();
        arrow.Init(playerLinks, 0, false, this);
        arrow.InitArrow(targetPoint, Hero.transform, Radius, DamageRange);
    }
}
