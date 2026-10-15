using UnityEngine;
using NativeWebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

public class WebSocketClient : MonoBehaviour
{
    private const float ReconnectBaseDelay = 1f;
    private const float ReconnectMaxDelay = 15f;

    public static WebSocketClient Instance { get; private set; }
    public bool IsConnected { get => _isConnected; set => _isConnected = value; }

    private WebSocket _webSocket;
    private bool _isConnected = false;

    private bool _hasConnectedOnce;
    private bool _intentionalClose;
    private bool _connecting;
    private bool _reconnectPending;
    private float _nextReconnectAt;
    private int _reconnectAttempt;

    private const float PingInterval = 10f;
    private const float DeadTimeout = 35f;
    private float _nextPingAt;
    private float _lastReceivedAt;

    public event Action Connected;

    public event Action Reconnected;

    public event Action Disconnected;

    public event Action<Dictionary<string, string>, string> MessageReceived;

    public int PlayerId { get; set; } = -1;

    private void Awake()
    {
        Application.runInBackground = true;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateSocket();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL  || UNITY_EDITOR
        if (_webSocket != null)
        {
            _webSocket.DispatchMessageQueue();
        }
#endif

        if (_reconnectPending && !_connecting && !_intentionalClose && Time.unscaledTime >= _nextReconnectAt)
        {
            _reconnectPending = false;
            _ = TryReconnect();
        }

        if (_isConnected)
        {
            if (Time.unscaledTime >= _nextPingAt)
            {
                _nextPingAt = Time.unscaledTime + PingInterval;
                SendMessageToServer("{\"type\":\"ping\"}");
            }

            if (Time.unscaledTime - _lastReceivedAt > DeadTimeout)
            {
                Debug.LogError("Сервер не отвечает, пересоздаю соединение");
                var dead = _webSocket;
                DetachSocket();
                _isConnected = false;
                _connecting = false;
                if (dead != null) { try { _ = dead.Close(); } catch { } }
                Disconnected?.Invoke();
                ScheduleReconnect();
            }
        }
    }

    private async void OnDestroy()
    {
        if (Instance != this) return;

        _intentionalClose = true;
        _reconnectPending = false;

        if (_webSocket != null)
        {
            var socket = _webSocket;
            DetachSocket();
            await socket.Close();
        }
    }

    private void CreateSocket()
    {
        DetachSocket();

        _webSocket = new WebSocket(URLLibrary.WebSocket);
        _webSocket.OnMessage += OnWebSocketMessage;
        _webSocket.OnOpen += OnOpen;
        _webSocket.OnError += OnError;
        _webSocket.OnClose += OnClose;
    }

    private void DetachSocket()
    {
        if (_webSocket == null) return;

        _webSocket.OnMessage -= OnWebSocketMessage;
        _webSocket.OnOpen -= OnOpen;
        _webSocket.OnError -= OnError;
        _webSocket.OnClose -= OnClose;
        _webSocket = null;
    }

    public async Task Connect()
    {
        _intentionalClose = false;
        _reconnectPending = false;

        if (_webSocket == null ||
            (_webSocket.State != WebSocketState.Open && _webSocket.State != WebSocketState.Connecting))
            CreateSocket();

        _connecting = true;
        try
        {
            await _webSocket.Connect();
        }
        finally
        {
            _connecting = false;
        }
    }

    public async Task Disconnect()
    {
        _intentionalClose = true;
        _reconnectPending = false;

        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            await _webSocket.Close();
    }

    public void SendMessageToServer(string message)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            _webSocket.SendText(message);
        else
            Debug.LogWarning($"WebSocket: сообщение не отправлено, сокет не открыт ({(_webSocket == null ? "null" : _webSocket.State.ToString())}): {message}");
    }

    private void OnWebSocketMessage(byte[] bytes)
    {
        _lastReceivedAt = Time.unscaledTime;
        string json = Encoding.UTF8.GetString(bytes);
        Dictionary<string, string> data;
        try
        {
            data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
        catch
        {
            data = new Dictionary<string, string>();
        }
        
        var handlers = MessageReceived;
        if (handlers == null) return;

        foreach (Action<Dictionary<string, string>, string> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(data, json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void OnOpen()
    {
        _lastReceivedAt = Time.unscaledTime;
        _nextPingAt = Time.unscaledTime + PingInterval;

        Debug.Log("connected websocket");
        _isConnected = true;
        _reconnectPending = false;
        _reconnectAttempt = 0;

        if (PlayerId >= 0)
        {
            SendMessageToServer(JsonConvert.SerializeObject(new
            {
                type = "authorization",
                playerId = PlayerId
            }));
        }

        bool isReconnect = _hasConnectedOnce;
        _hasConnectedOnce = true;

        Connected?.Invoke();
        if (isReconnect)
            Reconnected?.Invoke();
    }

    private void OnError(string errorMsg)
    {
        Debug.LogError($"Ошибка коннекта сокета: {errorMsg}");
    }

    private void OnClose(WebSocketCloseCode closeCode)
    {
        Debug.Log(closeCode);
        _isConnected = false;
        Disconnected?.Invoke();

        if (_intentionalClose) return;

        int code = (int)closeCode;
        if (code == 1008 || code == 1003)
        {
            Debug.LogError($"Сервер закрыл соединение с кодом {code}, автопереподключение отключено");
            return;
        }

        ScheduleReconnect();
    }

    private void ScheduleReconnect()
    {
        if (_reconnectPending || _intentionalClose) return;

        float delay = Mathf.Min(ReconnectMaxDelay, ReconnectBaseDelay * Mathf.Pow(2f, _reconnectAttempt));
        _reconnectAttempt++;
        _nextReconnectAt = Time.unscaledTime + delay;
        _reconnectPending = true;
        Debug.LogError($"Переподключение к серверу через {delay:0.#}с (попытка {_reconnectAttempt})");
    }

    private async Task TryReconnect()
    {
        if (_isConnected || _connecting || _intentionalClose) return;

        CreateSocket();
        _connecting = true;
        try
        {
            await _webSocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"Reconnect error: {e.Message}");
            if (!_isConnected) ScheduleReconnect();
        }
        finally
        {
            _connecting = false;
        }
    }
}