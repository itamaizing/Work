using System;
using UnityEngine;

[Serializable]
public class ChatChannelStyle
{
    [Tooltip("Текст тега в сообщении и название пункта в выпадающем списке каналов")]
    public string Label = "";

    public Color TagColor = Color.white;

    [Tooltip("Красить ник цветом команды (Chat.Team1 / Chat.Team2). Если выключено - берётся NameColor")]
    public bool ColorNameByTeam = true;
    public Color NameColor = Color.white;

    public Color TextColor = Color.white;
}