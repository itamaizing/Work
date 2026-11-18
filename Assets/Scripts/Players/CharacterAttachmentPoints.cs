using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum AttachmentPointType
{
    Head = 0,
    LeftHand = 1,
    RightHand = 2,
    Feet = 3,
    Torso = 4
}

[DisallowMultipleComponent]
[AddComponentMenu("Characters/Character Attachment Points")]
public sealed class CharacterAttachmentPoints : MonoBehaviour
{
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _rightHand;
    [Tooltip("Between the feet at the base of the model; follows the character root.")]
    [SerializeField] private Transform _feet;
    [SerializeField] private Transform _torso;

    public Transform Head => _head;
    public Transform LeftHand => _leftHand;
    public Transform RightHand => _rightHand;
    public Transform Feet => _feet;
    public Transform Torso => _torso;
    
    public bool TryGetPoint(AttachmentPointType type, out Transform point)
    {
        point = type switch
        {
            AttachmentPointType.Head => _head,
            AttachmentPointType.LeftHand => _leftHand,
            AttachmentPointType.RightHand => _rightHand,
            AttachmentPointType.Feet => _feet,
            AttachmentPointType.Torso => _torso,
            _ => null
        };
        return point != null;
    }

#if UNITY_EDITOR
    [ContextMenu("Set Up Missing Humanoid Points")]
    public void SetUpMissingHumanoidPoints()
    {
        if (Application.isPlaying) return;
        if (EditorUtility.IsPersistent(this))
        {
            Debug.LogWarning("Open the character in Prefab Mode before setting up attachment points.", this);
            return;
        }

        Character character = GetComponent<Character>();
        Animator animator = character != null ? character.Animator : null;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null || animator.avatar == null || !animator.isHuman || !animator.avatar.isValid)
        {
            Debug.LogWarning("Assign attachment points manually for characters without a valid Humanoid rig.", this);
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Set up character attachment points");
        Undo.RecordObject(this, "Assign character attachment points");

        if (_head == null) _head = CreatePoint(animator.GetBoneTransform(HumanBodyBones.Head), "Attachment_Head");
        if (_leftHand == null) _leftHand = CreatePoint(animator.GetBoneTransform(HumanBodyBones.LeftHand), "Attachment_LeftHand");
        if (_rightHand == null) _rightHand = CreatePoint(animator.GetBoneTransform(HumanBodyBones.RightHand), "Attachment_RightHand");
        if (_torso == null)
        {
            Transform chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
            _torso = CreatePoint(chest, "Attachment_Torso");
        }

        if (_feet == null)
        {
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot != null && rightFoot != null)
            {
                Transform existing = transform.Find("Attachment_Feet");
                _feet = existing != null ? existing : CreatePoint(transform, "Attachment_Feet");
                if (existing == null)
                {
                    Vector3 left = transform.InverseTransformPoint(leftFoot.position);
                    Vector3 right = transform.InverseTransformPoint(rightFoot.position);
                    _feet.localPosition = new Vector3((left.x + right.x) * 0.5f,
                        Mathf.Min(left.y, right.y), (left.z + right.z) * 0.5f);
                }
            }
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        EditorUtility.SetDirty(this);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static Transform CreatePoint(Transform parent, string name)
    {
        if (parent == null) return null;
        Transform existing = parent.Find(name);
        if (existing != null) return existing;

        var point = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(point, "Create character attachment point");
        Undo.SetTransformParent(point.transform, parent, "Parent character attachment point");
        point.transform.localPosition = Vector3.zero;
        point.transform.localRotation = Quaternion.identity;
        point.transform.localScale = Vector3.one;
        return point.transform;
    }
#endif
}
