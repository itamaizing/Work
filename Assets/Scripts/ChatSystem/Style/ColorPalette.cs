using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Game/Color Palette")]
public class ColorPalette : ScriptableObject
{
    [SerializeField] private UIColors _ui = new();
    [SerializeField] private ChatColors _chat = new();
    [SerializeField] private GameplayColors _gameplay = new();
    [SerializeField] private DescriptionColors _descriptions = new();

    public UIColors UI => _ui;
    public ChatColors Chat => _chat;
    public GameplayColors Gameplay => _gameplay;
    public DescriptionColors Descriptions => _descriptions;

    public bool TryGetDescriptionColor(string name, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(name)) return false;
        switch (name.Trim().ToLowerInvariant())
        {
            case "y": color = UI.YellowAccent; break;
            case "b": color = Color.blue; break;
            case "air": color = Descriptions.Air; break;
            case "earth": color = Descriptions.Earth; break;
            case "fire": color = Descriptions.Fire; break;
            case "water": color = Descriptions.Water; break;
            case "phys":
            case "physical": color = Descriptions.Physical; break;
            case "light": color = Descriptions.Light; break;
            case "dark": color = Descriptions.Dark; break;
            case "discipline": color = Descriptions.Discipline; break;
            case "health": color = Descriptions.Health; break;
            case "mana": color = Descriptions.Mana; break;
            case "energy": color = Descriptions.Energy; break;
            case "rune": color = Descriptions.Rune; break;
            case "evocd":
            case "cdenergy":
            case "cooldownenergy": color = Descriptions.CooldownEnergy; break;
            case "psionic": color = Descriptions.Psionic; break;
            case "attackingpsi": color = Descriptions.AttackingPsi; break;
            case "skill": color = Descriptions.SkillLink; break;
            case "state": color = Descriptions.StateLink; break;
            default: return false;
        }
        return true;
    }

    [Serializable]
    public class DescriptionColors
    {
        [Header("Schools")]
        public Color Air = new Color32(227, 219, 148, 255);
        public Color Earth = new Color32(222, 146, 79, 255);
        public Color Fire = new Color32(232, 90, 63, 255);
        public Color Water = new Color32(63, 178, 232, 255);
        public Color Physical = Color.white;
        public Color Light = new Color32(255, 216, 54, 255);
        public Color Dark = new Color32(151, 57, 227, 255);
        public Color Discipline = new Color32(106, 156, 222, 255);

        [Header("Resources")]
        public Color Health = new Color32(20, 189, 79, 255);
        public Color Mana = new Color32(46, 130, 232, 255);
        public Color Energy = new Color32(240, 234, 101, 255);
        public Color Rune = new Color32(106, 156, 222, 255);
        public Color CooldownEnergy = new Color32(151, 57, 227, 255);
        public Color Psionic = new Color32(151, 57, 227, 255);
        public Color AttackingPsi = new Color32(232, 90, 63, 255);

        [Header("Links")]
        public Color SkillLink = new Color32(255, 216, 54, 255);
        public Color StateLink = new Color32(255, 216, 54, 255);
    }

    [Serializable]
    public class UIColors
    {
        public Color White = Color.white;
        public Color YellowAccent = new Color32(255, 214, 79, 255);
    }

    [Serializable]
    public class ChatColors
    {
        [Header("Common")]
        public Color Time = new Color32(170, 170, 170, 255);
        public Color Team1 = new Color32(90, 150, 255, 255);
        public Color Team2 = new Color32(255, 100, 100, 255);

        [Header("Chat menu")]
        [Tooltip("Ник по умолчанию")]
        public Color Name = Color.white;
        [Tooltip("Ник друга")]
        public Color Friend = new Color32(139, 226, 139, 255);
        public Color SystemMessage = new Color32(153, 153, 153, 255);
        [Tooltip("Отправляется / получатель не в сети")]
        public Color Pending = new Color32(136, 136, 136, 255);
        public Color Error = new Color32(229, 115, 115, 255);

        [Header("Channels")]
        public ChatChannelStyle All = new ChatChannelStyle
        {
            Label = "Общий",
            TagColor = Color.white,
            ColorNameByTeam = true,
            TextColor = Color.white
        };

        public ChatChannelStyle Team = new ChatChannelStyle
        {
            Label = "Союзники",
            TagColor = new Color32(90, 200, 120, 255),
            ColorNameByTeam = true,
            TextColor = Color.white
        };

        public ChatChannelStyle Group = new ChatChannelStyle
        {
            Label = "Группа",
            TagColor = new Color32(100, 170, 255, 255),
            ColorNameByTeam = true,
            TextColor = new Color32(200, 225, 255, 255)
        };

        [Tooltip("Каналы, созданные игроками. В теге сообщения показывается название канала")]
        public ChatChannelStyle Custom = new ChatChannelStyle
        {
            Label = "Канал",
            TagColor = new Color32(255, 214, 79, 255),
            ColorNameByTeam = true,
            TextColor = Color.white
        };

        public ChatChannelStyle Private = new ChatChannelStyle
        {
            Label = "Лично",
            TagColor = new Color32(255, 140, 220, 255),
            ColorNameByTeam = false,
            NameColor = new Color32(255, 140, 220, 255),
            TextColor = new Color32(255, 140, 220, 255)
        };

        public ChatChannelStyle GetStyle(ChatChannel channel) => channel switch
        {
            ChatChannel.All => All,
            ChatChannel.Team => Team,
            ChatChannel.Group => Group,
            _ => Private
        };
    }

    [Serializable]
    public class GameplayColors
    {
        [Serializable]
        public class ResourceColors
#if UNITY_EDITOR
            : ISerializationCallbackReceiver
#endif
        {
            [Serializable]
            public struct ResourceStyle
            {
                [SerializeField] private Color _bar;
                [SerializeField] private Color _textLink;

                public Color? Bar => (_bar == default) ? null : _bar;
                public Color? TextLink => (_bar == default) ? null : _textLink;

            }

            [SerializeField] private Dictionary<ResourceType, ResourceStyle> _resources = new();
            public ResourceStyle this[ResourceType type] => _resources[type];

#if UNITY_EDITOR
            public ResourceColors()
            {
                FillTypes();
            }
            public void OnAfterDeserialize() { }

            public void OnBeforeSerialize()
            {
                FillTypes();
            }

            private void FillTypes()
            {
                foreach (ResourceType res in Enum.GetValues(typeof(ResourceType)))
                {
                    _resources.TryAdd(res, new ResourceStyle());
                }
            }
#endif
        }
        [SerializeField] private ResourceColors _resources;
        public ResourceColors Resources => _resources;
    }
}
