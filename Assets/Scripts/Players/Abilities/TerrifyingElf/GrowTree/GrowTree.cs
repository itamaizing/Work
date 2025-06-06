using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;
using UnityEngine;
using System;

public class GrowTree : Skill
{
    [Header("GrowTree Settings")]
    [SerializeField] private Tree _treePrefab;
    [SerializeField] private MoveComponent moveComponent;
    [SerializeField] private float _moveDuration = 0.5f;

    [Header("Talents")]
    [SerializeField] private bool treeHealthTalent;
    [SerializeField] private bool treeMagicEvadeTalent;
    [SerializeField] private ObjectData treeData;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Tree _currentTree;
    private ObjectHealth _healthTree;
    private float baseHealth;
    private float baseCastStreamDuration;

    protected override bool IsCanCast =>
        !float.IsPositiveInfinity(_targetPoint.x) &&
        IsPointInRadius(Radius, _targetPoint);

    protected override int AnimTriggerCastDelay =>
        Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        baseHealth = treeData.MaxHealth;
        baseCastStreamDuration = CastStreamDuration;
    }

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
        CastSuccess += HandleSkillCanceled;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        OnSkillCanceled += HandleSkillCanceled;
        CastSuccess += HandleSkillCanceled;

        TreeHealthTalentEnter();

        Hero.Animator.speed = Hero.Animator.speed / CastStreamDuration;

        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton)
            {
                var clickedCharacter = GetClickedCharacter();
                if (clickedCharacter != null && clickedCharacter == _hero) _targetPoint = _hero.transform.position;
                else
                {
                    _targetPoint = GetMousePoint();
                    Hero.Move.CanMove = false;
                    Hero.Move.LookAtPosition(_targetPoint);
                    if (!IsPointInRadius(Radius, _targetPoint)) _targetPoint = Vector3.positiveInfinity;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_treePrefab == null) yield break;

        moveComponent.CanMove = false;

        yield return new WaitForSeconds(CastStreamDuration / 3);

        var clickedCharacter = GetClickedCharacter();
        if (clickedCharacter != null && clickedCharacter == _hero) CmdSpawnTreeAndTeleport(_hero.transform.position);
        else CmdSpawnTree(_targetPoint);
    }

    #region Canceling a skill
    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Animator.speed = 1;
            Hero.Move.CanMove = true;
            Hero.Move.StopLookAt();
        }

        TreeHealthTalentExit();

        if (_healthTree != null)
        {
            _healthTree.ServerStopFillHP();
            _healthTree = null;
        }
    }
    #endregion

    #region Auxiliary methods
    private Character GetClickedCharacter()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent(out Character character))
            {
                return character;
            }
        }
        return null;
    }
    #endregion

    #region [Command] / Spawn
    [Command]
    private void CmdSetMaxHealth(float maxHealth)
    {
        treeData.MaxHealth = maxHealth;
    }

    [Command]
    private void CmdSpawnTree(Vector3 position)
    {
        var tree = Instantiate(_treePrefab, position, Quaternion.identity);
        _currentTree = tree;

        NetworkServer.Spawn(_currentTree.gameObject);
        SceneManager.MoveGameObjectToScene(_currentTree.gameObject, Hero.NetworkSettings.MyRoom);

        _healthTree = tree.GetComponent<ObjectHealth>();
        if (_healthTree != null)
        {
            _healthTree.InitializeObject(treeData);

            if (treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, 1f);

            if (treeMagicEvadeTalent) _healthTree.SetMagicEvade(100);
        }
    }

    [Command]
    private void CmdSpawnTreeAndTeleport(Vector3 position)
    {
        var tree = Instantiate(_treePrefab, position, Quaternion.identity);
        _currentTree = tree;
        NetworkServer.Spawn(_currentTree.gameObject);
        SceneManager.MoveGameObjectToScene(_currentTree.gameObject, Hero.NetworkSettings.MyRoom);

        RpcTeleportToTree(_currentTree.gameObject);

        _healthTree = _currentTree.GetComponent<ObjectHealth>();
        if (_healthTree != null)
        {
            _healthTree.InitializeObject(treeData);

            if (treeData.MinEndurance) _healthTree.ServerStartFillHP(_healthTree.ObjectData.MaxHealth, 1f);

            if (treeMagicEvadeTalent) _healthTree.SetMagicEvade(100);
        }
    }

    [ClientRpc]
    private void RpcTeleportToTree(GameObject tree)
    {
        if (tree != null)
        {
            Vector3 topOfTree = tree.transform.position + Vector3.up * 2f;
            moveComponent.TeleportToPositionSmooth(topOfTree, _moveDuration);
        }
    }
    #endregion

    #region Talent for doubling HP
    public void treeHealthTalentActive(bool value)
    {
        treeHealthTalent = value;
    }

    private void TreeHealthTalentEnter()
    {
        if (treeHealthTalent)
        {
            treeData.MaxHealth *= 2;
            CastStreamDuration *= 2;

            CmdSetMaxHealth(treeData.MaxHealth);
        }
    }

    private void TreeHealthTalentExit()
    {
        treeData.MaxHealth = baseHealth;
        CastStreamDuration = baseCastStreamDuration;

        CmdSetMaxHealth(treeData.MaxHealth);
    }
    #endregion

    #region Talent for Magical abbilities evade

    public void treeMagicEvadeTalentActive(bool value)
    {
        treeMagicEvadeTalent = value;
    }

    #endregion

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }
}
