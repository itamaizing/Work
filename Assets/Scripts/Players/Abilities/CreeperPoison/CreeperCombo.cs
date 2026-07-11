using Mirror;
using System.Collections;
using UnityEngine;

public class CreeperCombo : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Character _player;
    [SerializeField] private SneakySpit _sneakySpit;
    [SerializeField] private BlockPassiveSkill _blockPassiveSkill;

    [Header("SneakySpit Combo")]
    [SerializeField] private int _hitsForSneakySpitActivation = 3;
    [SerializeField] private float _comboResetDelay = 1f;
    [SerializeField] private float _sneakySpitWindowDuration = 1.5f;

    private Character _currentSneakySpitTarget;
    private Coroutine _resetCoroutine;
    private Coroutine _sneakySpitWindowCoroutine;

    private bool _isSneakySpitWindowActive;

    public Character CurrentSneakySpitTarget => _currentSneakySpitTarget;

    public void RegisterDamageToTarget(Character target)
    {
        if (target == null) return;

        if (isServer) ApplySneakySpitComboEffect(target);
        else CmdRegisterDamageToTarget(target.gameObject);
    }

    [Command]
    private void CmdRegisterDamageToTarget(GameObject targetObject)
    {
        if (targetObject == null) return;

        Character target = targetObject.GetComponent<Character>();
        if (target == null) return;

        ApplySneakySpitComboEffect(target);
    }

    private void ApplySneakySpitComboEffect(Character target)
    {
        if (!isServer) return;
        if (target == null) return;
        if (_player == null || _player.CharacterState == null) return;

        if (_isSneakySpitWindowActive)
        {
            RefreshSneakySpitWindow(target);
            return;
        }

        if (_currentSneakySpitTarget != null && _currentSneakySpitTarget != target) ClearSneakySpitComboState();

        _currentSneakySpitTarget = target;

        _player.CharacterState.AddState(States.CreeperCombo, _comboResetDelay, 0f, _player.gameObject, nameof(CreeperCombo));

        RestartResetTimer();

        CreeperComboState comboState = GetCreeperComboState();
        int currentStacks = comboState != null ? comboState.CurrentStacksCount : 0;

        if (currentStacks < _hitsForSneakySpitActivation) return;

        ActivateSneakySpitWindow(target);
    }

    private void ActivateSneakySpitWindow(Character target)
    {
        ClearSneakySpitComboState();

        _isSneakySpitWindowActive = true;
        _currentSneakySpitTarget = target;

        TargetRpcStartSneakySpitWindow(connectionToClient, target.netId, _sneakySpitWindowDuration);
        RestartSneakySpitWindowTimer();
    }

    private void RefreshSneakySpitWindow(Character target)
    {
        if (target == null) return;
        if (_currentSneakySpitTarget != null && _currentSneakySpitTarget != target) return;

        _currentSneakySpitTarget = target;

        TargetRpcStartSneakySpitWindow(connectionToClient, target.netId, _sneakySpitWindowDuration);
        RestartSneakySpitWindowTimer();

        Debug.Log("SneakySpit window refreshed");
    }

    private void RestartSneakySpitWindowTimer()
    {
        if (_sneakySpitWindowCoroutine != null)
        {
            StopCoroutine(_sneakySpitWindowCoroutine);
            _sneakySpitWindowCoroutine = null;
        }

        _sneakySpitWindowCoroutine = StartCoroutine(SneakySpitWindowTimer());
    }

    private IEnumerator SneakySpitWindowTimer()
    {
        yield return new WaitForSeconds(_sneakySpitWindowDuration);

        _isSneakySpitWindowActive = false;
        _sneakySpitWindowCoroutine = null;
        _currentSneakySpitTarget = null;
    }

    private CreeperComboState GetCreeperComboState()
    {
        if (_player == null || _player.CharacterState == null) return null;
        return _player.CharacterState.GetState(States.CreeperCombo) as CreeperComboState;
    }

    private void RestartResetTimer()
    {
        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }

        _resetCoroutine = StartCoroutine(ResetTimer());
    }

    private IEnumerator ResetTimer()
    {
        yield return new WaitForSeconds(_comboResetDelay);
        ClearSneakySpitComboState();
    }

    private void ClearSneakySpitComboState()
    {
        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }

        if (_player != null && _player.CharacterState != null)
        {
            CreeperComboState comboState = GetCreeperComboState();

            if (comboState != null)
            {
                comboState.ResetStacks();
                _player.CharacterState.RemoveState(States.CreeperCombo);
            }
        }

        if (!_isSneakySpitWindowActive) _currentSneakySpitTarget = null;
    }

    public void ConsumeSneakySpitBoost()
    {
        if (isServer)
        {
            ServerCloseSneakySpitWindow();
        }
        else
        {
            CmdConsumeSneakySpitBoost();
        }
    }

    [Command]
    private void CmdConsumeSneakySpitBoost()
    {
        ServerCloseSneakySpitWindow();
    }

    [Server]
    private void ServerCloseSneakySpitWindow()
    {
        if (!_isSneakySpitWindowActive) return;

        if (_sneakySpitWindowCoroutine != null)
        {
            StopCoroutine(_sneakySpitWindowCoroutine);
            _sneakySpitWindowCoroutine = null;
        }

        _isSneakySpitWindowActive = false;
        _currentSneakySpitTarget = null;

        TargetRpcCloseSneakySpitWindow(connectionToClient);
    }

    [TargetRpc]
    private void TargetRpcCloseSneakySpitWindow(NetworkConnection targetConnection)
    {
        if (_sneakySpit == null)
            return;

        _sneakySpit.CancelBoostWindow();
    }

    [TargetRpc]
    private void TargetRpcStartSneakySpitWindow(NetworkConnection targetConnection, uint targetNetId, float duration)
    {
        if (_sneakySpit == null) return;
        if (!NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity)) return;

        Character target = identity.GetComponent<Character>();
        if (target == null) return;

        _sneakySpit.TryStartSneakySpitBoostWindow(target, duration);
    }
}