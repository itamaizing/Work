using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RetributiveReckoningAnimationController : MonoBehaviour
{
    [SerializeField] private GameObject Way;

    private StateEffects _stateEffects;
    private Collider _collider;
    private Rigidbody _rigidbody;
    private NavMeshObstacle _navMeshObstacle;
    private List<GameObject> _elfParts;
    private List<Material[]> _originalMaterials = new List<Material[]>();

    private void Start()
    {
        _stateEffects = GetComponent<StateEffects>();
        _collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _navMeshObstacle = GetComponent<NavMeshObstacle>();

        if (_stateEffects == null)
        {
            Debug.LogWarning("StateEffects component is missing on this object.");
            return;
        }

        _elfParts = new List<GameObject> { gameObject };

        foreach (var part in _elfParts)
        {
            if (part.TryGetComponent<Renderer>(out var renderer))
            {
                _originalMaterials.Add(renderer.materials);
            }
            else
            {
                Debug.LogWarning($"No Renderer found on {part.name}");
                _originalMaterials.Add(null);
            }
        }
    }

    public void DisablePhysics()
    {
        if (_stateEffects == null) return;

        foreach (var part in _elfParts)
        {
            if (part.TryGetComponent<Renderer>(out var renderer))
            {
                Material[] ghostMaterials = new Material[renderer.materials.Length];
                for (int j = 0; j < ghostMaterials.Length; j++)
                {
                    ghostMaterials[j] = _stateEffects.MaterialGhost;
                }
                renderer.materials = ghostMaterials;
            }
        }

        if (_stateEffects.Weapon != null) _stateEffects.Weapon.SetActive(false);
        if (Way != null) Way.SetActive(true);

        _collider.enabled = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        _navMeshObstacle.enabled = false;
    }

    public void EnablePhysics()
    {
        if (_stateEffects == null) return;

        for (int i = 0; i < _elfParts.Count; i++)
        {
            if (_elfParts[i].TryGetComponent<Renderer>(out var renderer) && _originalMaterials[i] != null)
            {
                renderer.materials = _originalMaterials[i];
            }
        }

        if (_stateEffects.Weapon != null) _stateEffects.Weapon.SetActive(true);
        if (Way != null) Way.SetActive(false);

        _collider.enabled = true;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _navMeshObstacle.enabled = true;
    }
}
