using System.Collections;
using Mirror;
using UnityEngine;

public class UnitLayerSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTeamIndexChanged))]
    public byte TeamIndex;

    private int _retries;
    private float _updateLayerDelay;

    private void OnTeamIndexChanged(byte oldTeam, byte newTeam)
    {
        if (isClient)
        {
            UpdateLayer();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateLayer();
    }

    private void UpdateLayer()
    {
        var localPlayer = LevelCharacterManager.Instance.GetHero();
        if (localPlayer == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Local player not found yet. Delaying layer update.");
            StartCoroutine(DelayedUpdateLayer());
            return;
        }

        var localSettings = localPlayer.NetworkSettings;
        if (localSettings == null)
        {
            Debug.LogError($"[{gameObject.name}] Local player '{localPlayer.name}' missing UserNetworkSettings. Check player prefab!");
            return;
        }


        int layer = (localSettings.TeamIndex == TeamIndex) 
            ? LayerMask.NameToLayer("Allies") 
            : LayerMask.NameToLayer("Enemy");

        SetLayerRecursive(gameObject, layer);
    }

    private IEnumerator DelayedUpdateLayer()
    {
        _retries = 5;
        _updateLayerDelay = 0.5f;
        while (_retries > 0)
        {
            yield return new WaitForSeconds(_updateLayerDelay);
            if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.GetComponent<UserNetworkSettings>() != null)
            {
                UpdateLayer();
                yield break;
            }
            _retries--;
            _updateLayerDelay += 0.5f;
            Debug.LogWarning($"[{gameObject.name}] Retry layer update ({_retries} left)...");
        }
        Debug.LogError($"[{gameObject.name}] Failed to update layer after retries. Local player or settings missing.");
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
