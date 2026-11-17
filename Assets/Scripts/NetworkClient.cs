using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// The one place that talks to the server. Calls a Supabase function and hands back its JSON.
/// Everything else in the game goes through this class.
/// </summary>
public class NetworkClient : MonoBehaviour
{
    static NetworkClient instance;

    public static NetworkClient Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("NetworkClient");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<NetworkClient>();
            }
            return instance;
        }
    }

    public int timeoutSeconds = 8;

    public void Rpc(string function, string jsonBody, Action<string> onOk, Action<string> onFail)
    {
        StartCoroutine(RpcRoutine(function, jsonBody, onOk, onFail));
    }

    IEnumerator RpcRoutine(string function, string jsonBody, Action<string> onOk, Action<string> onFail)
    {
        if (!YuccaBackendConfig.IsConfigured)
        {
            if (onFail != null) onFail("Backend is not configured.");
            yield break;
        }

        string url = YuccaBackendConfig.Url.TrimEnd('/') + "/rest/v1/rpc/" + function;
        string key = YuccaBackendConfig.AnonKey;

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jsonBody) ? "{}" : jsonBody));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("apikey", key);
            req.SetRequestHeader("Authorization", "Bearer " + key);
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                if (onFail != null) onFail(function + ": " + req.error + " " + req.downloadHandler.text);
            }
            else if (onOk != null)
            {
                onOk(req.downloadHandler.text);
            }
        }
    }

    /// <summary>Escapes a string for use inside a JSON string literal.</summary>
    public static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }
}
