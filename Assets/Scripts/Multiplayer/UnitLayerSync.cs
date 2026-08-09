using System.Collections;
using Mirror;
using UnityEngine;

public class UnitLayerSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTeamIndexChanged))]
    public byte TeamIndex;

    private Coroutine _updateLayerCoroutine;

    public override void OnStartClient()
    {
        base.OnStartClient();
        RequestLayerUpdate();
    }

    public override void OnStopClient()
    {
        if (_updateLayerCoroutine != null)
        {
            StopCoroutine(_updateLayerCoroutine);
            _updateLayerCoroutine = null;
        }

        base.OnStopClient();
    }

    private void OnTeamIndexChanged(byte oldTeam, byte newTeam)
    {
        RequestLayerUpdate();
    }

    private void RequestLayerUpdate()
    {
        if (TryUpdateLayer())
            return;

        if (_updateLayerCoroutine == null)
            _updateLayerCoroutine = StartCoroutine(DelayedUpdateLayer());
    }

    private bool TryUpdateLayer()
    {
        UserNetworkSettings localSettings = GetLocalSettings();

        if (localSettings == null)
            return false;

        string layerName = localSettings.TeamIndex == TeamIndex
            ? "Allies"
            : "Enemy";

        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogError($"[{name}] —лой '{layerName}' отсутствует.");
            return true;
        }

        SetLayerRecursive(gameObject, layer);
        return true;
    }

    private UserNetworkSettings GetLocalSettings()
    {
        // ќсновной вариант Ч герой уже записан в менеджер
        if (LevelCharacterManager.Instance != null &&
            LevelCharacterManager.Instance.TryGetCurrentHero(out HeroComponent hero) &&
            hero != null)
        {
            return hero.NetworkSettings;
        }

        // «апасной вариант Ч ищем принадлежащего клиенту Character
        foreach (NetworkIdentity identity in NetworkClient.spawned.Values)
        {
            if (identity == null || !identity.isOwned)
                continue;

            if (identity.TryGetComponent(out Character character))
                return character.NetworkSettings;
        }

        return null;
    }

    private IEnumerator DelayedUpdateLayer()
    {
        const float timeout = 30f;
        float elapsedTime = 0f;

        while (elapsedTime < timeout)
        {
            if (TryUpdateLayer())
            {
                _updateLayerCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
            elapsedTime += 0.25f;
        }

        _updateLayerCoroutine = null;

        Debug.LogError(
            $"[{name}] Ћокальный герой или его UserNetworkSettings " +
            $"не найден за {timeout} секунд.");
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}