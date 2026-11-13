using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CleanAds
{
    /// <summary>Outcome of <see cref="CleanAdsSdk.Show"/>.</summary>
    public struct CleanAdsResult
    {
        /// <summary>True when the ad finished: grant the reward (rewarded) or continue (interstitial).</summary>
        public bool Success;
        /// <summary>Why no reward: "no_fill", "closed_early", "network_error", "unsupported_platform", ...</summary>
        public string Reason;
        /// <summary>Impression id, also sent to your server-side reward callback as imp_id.</summary>
        public string ImpressionId;
    }

    /// <summary>
    /// CleanAds for Unity WebGL builds (Telegram Mini Apps and websites).
    ///
    ///   CleanAdsSdk.Init("pl_xxx", userId: player.Id, test: true);
    ///   CleanAdsSdk.Show("pl_xxx", r => { if (r.Success) GrantReward(); });
    ///
    /// In the Editor and on non-WebGL platforms nothing is shown: with test mode on, Show reports success so you
    /// can wire up your reward flow; otherwise it reports "unsupported_platform".
    /// </summary>
    public static class CleanAdsSdk
    {
        public const string Version = "1.0.0";
        public const string DefaultApiUrl = "https://api.cleanads.ai";

        static readonly Dictionary<int, Action<CleanAdsResult>> Pending = new Dictionary<int, Action<CleanAdsResult>>();
        static readonly HashSet<string> TestPlacements = new HashSet<string>();
#if UNITY_WEBGL && !UNITY_EDITOR
        static int _nextId = 1;
#endif
        static CleanAdsReceiver _receiver;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CleanAds_Init(string placementId, string userId, int test, string apiUrl);
        [DllImport("__Internal")] static extern void CleanAds_Show(int requestId, string placementId, string receiverName);
#endif

        /// <summary>Registers a placement. Call once per placement, e.g. at startup. Loads the web SDK if the page has not.</summary>
        public static void Init(string placementId, string userId = null, bool test = false, string apiUrl = DefaultApiUrl)
        {
            if (string.IsNullOrEmpty(placementId)) throw new ArgumentException("placementId is required", nameof(placementId));
            if (test) TestPlacements.Add(placementId); else TestPlacements.Remove(placementId);
            EnsureReceiver();
#if UNITY_WEBGL && !UNITY_EDITOR
            CleanAds_Init(placementId, userId ?? "", test ? 1 : 0, string.IsNullOrEmpty(apiUrl) ? DefaultApiUrl : apiUrl);
#endif
        }

        /// <summary>Requests and shows a full-screen ad for a placement registered with <see cref="Init"/>.</summary>
        public static void Show(string placementId, Action<CleanAdsResult> onDone)
        {
            if (string.IsNullOrEmpty(placementId)) throw new ArgumentException("placementId is required", nameof(placementId));
#if UNITY_WEBGL && !UNITY_EDITOR
            EnsureReceiver();
            var id = _nextId++;
            Pending[id] = onDone;
            CleanAds_Show(id, placementId, _receiver.gameObject.name);
#else
            var ok = TestPlacements.Contains(placementId);
            onDone?.Invoke(new CleanAdsResult
            {
                Success = ok,
                Reason = ok ? null : "unsupported_platform",
                ImpressionId = ok ? "imp_editor_test" : null,
            });
#endif
        }

        static void EnsureReceiver()
        {
            if (_receiver != null) return;
            var go = new GameObject("CleanAdsReceiver");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _receiver = go.AddComponent<CleanAdsReceiver>();
        }

#pragma warning disable 0649 // fields are set by JsonUtility
        [Serializable]
        class Message
        {
            public int id;
            public bool ok;
            public string reason;
            public string imp;
        }
#pragma warning restore 0649

        internal static void Deliver(string json)
        {
            Message m;
            try { m = JsonUtility.FromJson<Message>(json); }
            catch (Exception e) { Debug.LogWarning("[CleanAds] bad message: " + e.Message); return; }
            if (m == null || !Pending.TryGetValue(m.id, out var cb)) return;
            Pending.Remove(m.id);
            try
            {
                cb?.Invoke(new CleanAdsResult
                {
                    Success = m.ok,
                    Reason = string.IsNullOrEmpty(m.reason) ? null : m.reason,
                    ImpressionId = string.IsNullOrEmpty(m.imp) ? null : m.imp,
                });
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    /// <summary>Receives results from the WebGL plugin (SendMessage target). Created automatically.</summary>
    public sealed class CleanAdsReceiver : MonoBehaviour
    {
        public void OnCleanAdsResult(string json) => CleanAdsSdk.Deliver(json);
    }
}
