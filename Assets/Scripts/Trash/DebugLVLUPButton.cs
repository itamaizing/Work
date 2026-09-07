using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DebugLVLUPButton : MonoBehaviour
{
    [SerializeField] private SelectManager _selectManager;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(AddLevel);
    }

    private void AddLevel()
    {
        if (NetworkClient.connection == null || NetworkClient.connection.identity == null) return;

        var controller = NetworkClient.connection.identity.GetComponent<NetworkComponent>();
        if (controller == null) return;

        var hero = controller.controllableUnits.OfType<HeroComponent>().FirstOrDefault();
        if (hero == null || hero.LVL == null) return;

        hero.LVL.CMDAddEXP(100);
    }
}
