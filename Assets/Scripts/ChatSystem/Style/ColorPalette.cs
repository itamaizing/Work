using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Game/Color Palette")]
public class ColorPalette : ScriptableObject
{
    [SerializeField] private UIColors _ui = new();
    [SerializeField] private ChatColors _chat = new();
    [SerializeField] private GameplayColors _gameplay = new();

    public UIColors UI => _ui;
    public ChatColors Chat => _chat;
    public GameplayColors Gameplay => _gameplay;

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
#if !UNITY_EDITOR
        public class Resources
#else
        public class ResourceColors : ISerializationCallbackReceiver
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