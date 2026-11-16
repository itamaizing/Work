using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    public static TargetSelector Instance { get; private set; }

    public Character InspectedTarget { get; private set; }

    private int _inspectionLayers;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _inspectionLayers = LayerMask.GetMask("Allies", "Enemy", "ControlledObjects", "Damageble", "Default", "Ground", "Obstecls");
    }

    private void OnEnable()
    {
        InputHandler.OnWorldTargetClick += HandleWorldClick;
    }

    private void OnDisable()
    {
        InputHandler.OnWorldTargetClick -= HandleWorldClick;
        SetTarget(null);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleWorldClick()
    {
        if (Input.GetKey(KeyCode.LeftAlt))
            return;

        var camera = Camera.main;
        if (camera == null) return;

        var ray = camera.ScreenPointToRay(Input.mousePosition);
        var character = Physics.Raycast(ray, out var hit, Mathf.Infinity, _inspectionLayers, QueryTriggerInteraction.Ignore)
            ? hit.collider.GetComponentInParent<Character>()
            : null;
        if (character != null && character == Character.Local) return;
        SetTarget(character);
    }

    public void SetTarget(Character character)
    {
        if (character == null || character == Character.Local || character.IsDead) character = null;
        if (ReferenceEquals(InspectedTarget, character)) return;

        if (InspectedTarget != null)
            InspectedTarget.Died -= OnInspectedTargetDied;

        InspectedTarget = character;

        if (InspectedTarget != null)
            InspectedTarget.Died += OnInspectedTargetDied;
    }

    private void OnInspectedTargetDied(Character character) => SetTarget(null);
}
