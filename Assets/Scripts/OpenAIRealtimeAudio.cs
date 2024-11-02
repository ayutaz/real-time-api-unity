using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MikeSchweitzer.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// OpenAIのrealtime apiを使用して、音声データを送信し、テキストと音声を受信するサンプル
/// </summary>
public class OpenAIRealtimeAudio : MonoBehaviour
{
    /// <summary>
    /// OpenAI APIキー
    /// </summary>
    [SerializeField] private string apiKey = "YOUR_OPENAI_API_KEY";

    /// <summary>
    /// 使用するモデルの名前
    /// </summary>
    private const string ModelName = "gpt-4o-realtime-preview-2024-10-01";


    /// <summary>
    /// テキストを表示するTextMeshProUGUI
    /// </summary>
    [SerializeField] private TextMeshProUGUI assistantText;

    /// <summary>
    /// 音声を再生するAudioSource
    /// </summary>
    private AudioSource _audioSource;

    /// <summary>
    /// WebSocketConnectionのインスタンス
    /// </summary>
    private WebSocketConnection _connection;

    /// <summary>
    /// 使用するマイクの名前
    /// </summary>
    private string _microphone;

    /// <summary>
    /// マイクから取得した音声データを格納するAudioClip
    /// </summary>
    private AudioClip _audioClip;

    /// <summary>
    /// マイクから取得した最後のサンプル位置
    /// </summary>
    private int _lastSamplePosition = 0;

    /// <summary>
    /// 接続状態を示すフラグ
    /// </summary>
    private bool _isConnected = false; // 接続状態を示すフラグ

    /// <summary>
    /// 音声データのバッファ
    /// </summary>
    private readonly List<byte> _audioBuffer = new List<byte>();

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _connection = gameObject.AddComponent<WebSocketConnection>();
    }

    /// <summary>
    /// オブジェクトが有効になったときに呼び出される
    /// </summary>
    private void Start()
    {
        // マイクの初期化
        if (Microphone.devices.Length > 0)
        {
            _microphone = Microphone.devices[0];
            _audioClip = Microphone.Start(_microphone, true, 1, 24000);
        }
        else
        {
            Debug.LogError("マイクが接続されていません");
            return;
        }

        // イベントの購読
        _connection.MessageReceived += OnMessageReceived;
        _connection.ErrorMessageReceived += OnErrorMessageReceived;

        // 接続開始
        ConnectToRealtimeAPI().Forget();
    }

    /// <summary>
    ///  Realtime APIに接続
    /// </summary>
    private async UniTaskVoid ConnectToRealtimeAPI()
    {
        string url = $"wss://api.openai.com/v1/realtime?model={ModelName}";

        // ヘッダーの設定
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {apiKey}" },
            { "OpenAI-Beta", "realtime=v1" }
        };

        // 接続設定の作成
        _connection.DesiredConfig = new WebSocketConfig
        {
            Url = url,
            Headers = headers,
            MaxReceiveBytes = 1024 * 1024 * 5, // 5MBに設定（必要に応じて調整）
            MaxSendBytes = 1024 * 1024 * 5, // 5MBに設定（必要に応じて調整）
        };

        _connection.Connect();

        // 接続が確立されるまで待機
        await UniTask.WaitUntil(() => _connection.State == WebSocketState.Connected);

        Debug.Log("Connected to Realtime API");

        _isConnected = true; // 接続フラグを設定
    }

    private void Update()
    {
        // 接続が確立されるまで音声データの送信を停止
        if (!_isConnected)
        {
            return;
        }

        // マイクから音声データを取得して送信
        if (Microphone.IsRecording(_microphone))
        {
            int currentPosition = Microphone.GetPosition(_microphone);

            if (currentPosition < _lastSamplePosition)
            {
                // ループした場合
                _lastSamplePosition = 0;
            }

            int sampleLength = currentPosition - _lastSamplePosition;

            if (sampleLength > 0)
            {
                float[] samples = new float[sampleLength];
                _audioClip.GetData(samples, _lastSamplePosition);

                // 更新
                _lastSamplePosition = currentPosition;

                // 音声データを送信
                SendAudioData(samples);
            }
        }
    }

    /// <summary>
    /// 音声データを送信
    /// </summary>
    /// <param name="audioData"></param>
    private void SendAudioData(float[] audioData)
    {
        if (_connection.State != WebSocketState.Connected)
        {
            // 接続が確立されていない場合は送信しない
            return;
        }

        byte[] pcmData = FloatToPCM16(audioData);
        string base64Audio = Convert.ToBase64String(pcmData);

        var eventMessage = new
        {
            type = "input_audio_buffer.append",
            audio = base64Audio
        };

        string jsonMessage = JsonConvert.SerializeObject(eventMessage);
        _connection.AddOutgoingMessage(jsonMessage);
    }

    /// <summary>
    /// メッセージを受信
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="message"></param>
    private void OnMessageReceived(WebSocketConnection connection, WebSocketMessage message)
    {
        // 非メインスレッドから呼び出される可能性があるため、メインスレッドで処理を行う
        UniTask.Post(() => ProcessMessage(message));
    }

    /// <summary>
    /// エラーメッセージを受信
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="errorMessage"></param>
    private void OnErrorMessageReceived(WebSocketConnection connection, string errorMessage)
    {
        // エラーメッセージをメインスレッドでログ出力
        UniTask.Post(() => Debug.LogError($"WebSocket Error: {errorMessage}"));
    }

    /// <summary>
    /// メッセージを処理
    /// </summary>
    /// <param name="message"></param>
    private void ProcessMessage(WebSocketMessage message)
    {
        // メッセージをパース
        JObject json;
        try
        {
            json = JObject.Parse(message.String);
        }
        catch (Exception ex)
        {
            Debug.LogError($"JSONのパースに失敗しました: {ex.Message}");
            return;
        }

        string messageType = (string)json["type"];

        switch (messageType)
        {
            case "session.created":
            {
                Debug.Log("Session created");
                break;
            }
            case "response.created":
            {
                Debug.Log("Response created");
                break;
            }
            case "rate_limits.updated":
            {
                Debug.Log("Rate limits updated");
                // 必要であれば rate_limits 情報を取得して使用します
                break;
            }
            case "conversation.item.created":
            {
                Debug.Log("Conversation item created");
                // 必要であれば item 情報を取得して使用します
                break;
            }
            case "response.output_item.added":
            {
                Debug.Log("Response output item added");
                // 必要であれば output_item 情報を取得して使用します
                break;
            }
            case "response.output_item.done":
            {
                Debug.Log("Response output item done");
                break;
            }
            case "response.content_part.added":
            {
                Debug.Log("Response content part added");
                break;
            }
            case "response.content_part.done":
            {
                Debug.Log("Response content part done");
                break;
            }
            case "response.text.delta":
            {
                // テキストを更新
                assistantText.text += (string)json["delta"];

                break;
            }
            case "response.text.done":
            {
                assistantText.text = (string)json["text"];
                Debug.Log($"Assistant says: {(string)json["text"]}");

                break;
            }
            case "response.audio_transcript.delta":
            {
                // 音声の転写の増分を取得
                var deltaText = (string)json["delta"];
                assistantText.text += deltaText;


                break;
            }
            case "response.audio_transcript.done":
            {
                var transcript = (string)json["text"];
                assistantText.text = transcript;

                Debug.Log($"Audio transcript done: {transcript}");
                break;
            }
            case "response.audio.delta":
            {
                // 音声の増分を取得
                string deltaBase64 = (string)json["delta"];
                byte[] deltaBytes = Convert.FromBase64String(deltaBase64);
                _audioBuffer.AddRange(deltaBytes);
                break;
            }
            case "response.audio.done":
            {
                // 音声の最終データが到着
                // バッファに溜めた音声データを再生
                PlayAudioFromBytes(_audioBuffer.ToArray());

                // バッファをクリア
                _audioBuffer.Clear();
                break;
            }
            case "response.done":
            {
                Debug.Log("Response done");
                break;
            }
            case "input_audio_buffer.speech_started":
            {
                Debug.Log("Speech started");
                break;
            }
            case "input_audio_buffer.speech_stopped":
            {
                Debug.Log("Speech stopped");
                break;
            }
            case "input_audio_buffer.committed":
            {
                Debug.Log("Input audio buffer committed");
                break;
            }
            case "error":
            {
                string errorMessage = (string)json["error"]?["message"];
                Debug.LogError($"サーバーからのエラー: {errorMessage}");
                break;
            }
            default:
            {
                Debug.LogWarning($"未処理のイベントタイプ: {messageType}");
                break;
            }
        }
    }

    /// <summary>
    /// 音声データを再生
    /// </summary>
    /// <param name="audioBytes"></param>
    private void PlayAudioFromBytes(byte[] audioBytes)
    {
        if (audioBytes == null || audioBytes.Length == 0)
            return;

        float[] floatData = PCM16ToFloat(audioBytes);

        AudioClip clip = AudioClip.Create("Response", floatData.Length, 1, 24000, false);
        clip.SetData(floatData, 0);
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    /// <summary>
    /// float配列をPCM16に変換
    /// </summary>
    /// <param name="floatData"></param>
    /// <returns></returns>
    private static byte[] FloatToPCM16(float[] floatData)
    {
        int length = floatData.Length;
        byte[] bytesData = new byte[length * sizeof(short)];

        for (int i = 0; i < length; i++)
        {
            float sample = floatData[i];
            if (sample < -1.0f) sample = -1.0f;
            if (sample > 1.0f) sample = 1.0f;

            short value = (short)(sample * short.MaxValue);
            bytesData[i * 2] = (byte)(value & 0x00ff);
            bytesData[i * 2 + 1] = (byte)((value & 0xff00) >> 8);
        }

        return bytesData;
    }

    /// <summary>
    /// PCM16をfloat配列に変換
    /// </summary>
    /// <param name="pcmData"></param>
    /// <returns></returns>
    private static float[] PCM16ToFloat(byte[] pcmData)
    {
        int length = pcmData.Length / 2;
        float[] floatData = new float[length];

        for (int i = 0; i < length; i++)
        {
            short value = BitConverter.ToInt16(pcmData, i * 2);
            floatData[i] = value / (float)short.MaxValue;
        }

        return floatData;
    }

    /// <summary>
    ///  オブジェクトが破棄されたときに呼び出される
    /// </summary>
    private void OnDestroy()
    {
        // イベントの購読解除
        if (_connection != null)
        {
            _connection.MessageReceived -= OnMessageReceived;
            _connection.ErrorMessageReceived -= OnErrorMessageReceived;
            _connection.Disconnect();
        }
    }
}