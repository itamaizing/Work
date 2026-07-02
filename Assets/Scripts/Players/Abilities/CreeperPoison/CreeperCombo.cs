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
        if (_currentSneakySpitTarget != null && _currentSneakySpitTarget != target) ClearSneakySpitComboState();

        _currentSneakySpitTarget = target;
        _player.CharacterState.AddState(States.CreeperCombo, _comboResetDelay, 0f, _player.gameObject, nameof(CreeperCombo));

        RestartResetTimer();

        CreeperComboState comboState = GetCreeperComboState();

        int currentStacks = comboState != null ? comboState.CurrentStacksCount : 0;

        Debug.Log($"currentStacks: {currentStacks}");

        if (currentStacks < _hitsForSneakySpitActivation) return;

        ClearSneakySpitComboState();
        TargetRpcStartSneakySpitWindow(connectionToClient, target.netId);
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

        _currentSneakySpitTarget = null;
    }

    [TargetRpc]
    private void TargetRpcStartSneakySpitWindow(NetworkConnection targetConnection, uint targetNetId)
    {
        if (_sneakySpit == null) return;
        if (!NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity)) return;

        Character target = identity.GetComponent<Character>();
        if (target == null) return;

        _sneakySpit.TryStartSneakySpitBoostWindow(target, _sneakySpitWindowDuration);
    }
}