using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class InviteController : MonoBehaviour
{
    [SerializeField] private Button _inviteButton;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private PlayerProfileMenu _profilePrefab;
    [SerializeField] private PlayerProfileMenu _player;

    private void Start()
    {
        _inviteButton.onClick.AddListener(() => DEBUGTEXT(_nameInput.text));
        _player.Init(MPNetworkManager.Instance.UserID.ToString());
    }

    private void DEBUGTEXT(string name)
    {
        int.TryParse(_nameInput.text, out int result);
        Debug.Log(name + " / " + result);
        ServerManager.Instance.GroupManager.SendInvite(result);
    }
}
