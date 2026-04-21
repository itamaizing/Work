using UnityEngine;
using NativeWebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    private WebSocket _webSocket;
    private bool _isConnected = false;

    public event Action Connected;
    public event Action Disconnected;
    public event Action<Dictionary<string, string>> MessageReceived;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _webSocket = new WebSocket(URLLibrary.WebSocket);

        _webSocket.OnMessage += OnWebSocketMessage;
        _webSocket.OnOpen += OnOpen;
        _webSocket.OnError += OnError;
        _webSocket.OnClose += OnClose;
    }

    private void Update()
    {
#if !UNITY_WEBGL  || UNITY_EDITOR
        if (_webSocket != null)
        {
            _webSocket.DispatchMessageQueue();
        }
#endif
    }

    private async void OnDestroy()
    {
        if (_webSocket != null)
        {
            await _webSocket.Close();

            _webSocket.OnMessage -= OnWebSocketMessage;
            _webSocket.OnOpen -= OnOpen;
            _webSocket.OnError -= OnError;
            _webSocket.OnClose -= OnClose;
        }
    }

    public async Task Connect()
    {
        await _webSocket.Connect();

        Debug.Log("TEST");
    }

    public void SendMessageToServer(string message)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            _webSocket.SendText(message);
    }

    private void OnWebSocketMessage(byte[] bytes)
    {
        string json = Encoding.UTF8.GetString(bytes);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        MessageReceived?.Invoke(data);
    }

    private void OnOpen()
    {
        Debug.Log("connected websocket");
        _isConnected = true;
        Connected?.Invoke();
    }

    private void OnError(string errorMsg)
    {
        Debug.LogError($"Ошибка коннекта сокета: {errorMsg}");
    }

    private void OnClose(WebSocketCloseCode closeCode)
    {
        Debug.Log(closeCode);
        _isConnected = false;
    }

    private async Task Reconnect()
    {
        if (!_isConnected)
        {
            await Connect();
        }
    }
}
