// CleanAds WebGL bridge: forwards Init/Show to the CleanAds web SDK (window.CleanAds) and reports results back
// to the CleanAdsReceiver GameObject with SendMessage. Loads https://api.cleanads.ai/sdk/v1.js if the page has not.
var CleanAdsPlugin = {
  $CleanAdsState: { controllers: {}, loading: null },

  $CleanAdsLoad__deps: ["$CleanAdsState"],
  $CleanAdsLoad: function (apiUrl) {
    if (window.CleanAds) return Promise.resolve();
    if (CleanAdsState.loading) return CleanAdsState.loading;
    CleanAdsState.loading = new Promise(function (resolve, reject) {
      var s = document.createElement("script");
      s.src = apiUrl.replace(/\/+$/, "") + "/sdk/v1.js";
      s.async = true;
      s.onload = function () { resolve(); };
      s.onerror = function () { CleanAdsState.loading = null; reject(new Error("sdk_load_failed")); };
      document.head.appendChild(s);
    });
    return CleanAdsState.loading;
  },

  $CleanAdsSend: function (receiver, payload) {
    var json = JSON.stringify(payload);
    if (typeof SendMessage === "function") SendMessage(receiver, "OnCleanAdsResult", json);
    else if (typeof Module !== "undefined" && typeof Module.SendMessage === "function") Module.SendMessage(receiver, "OnCleanAdsResult", json);
    else console.warn("[CleanAds] SendMessage is not available");
  },

  CleanAds_Init__deps: ["$CleanAdsState", "$CleanAdsLoad"],
  CleanAds_Init: function (placementPtr, userPtr, test, apiPtr) {
    var placementId = UTF8ToString(placementPtr);
    var userId = UTF8ToString(userPtr);
    var apiUrl = UTF8ToString(apiPtr) || "https://api.cleanads.ai";
    CleanAdsState.controllers[placementId] = CleanAdsLoad(apiUrl).then(function () {
      return window.CleanAds.init({ placementId: placementId, userId: userId || undefined, test: test === 1, apiUrl: apiUrl });
    });
    CleanAdsState.controllers[placementId].catch(function () {});
  },

  CleanAds_Show__deps: ["$CleanAdsState", "$CleanAdsSend"],
  CleanAds_Show: function (requestId, placementPtr, receiverPtr) {
    var placementId = UTF8ToString(placementPtr);
    var receiver = UTF8ToString(receiverPtr);
    var ctl = CleanAdsState.controllers[placementId];
    if (!ctl) {
      CleanAdsSend(receiver, { id: requestId, ok: false, reason: "not_initialized" });
      return;
    }
    ctl.then(function (c) { return c.show(); }).then(
      function (r) { CleanAdsSend(receiver, { id: requestId, ok: true, reason: "", imp: (r && r.impressionId) || "" }); },
      function (e) { CleanAdsSend(receiver, { id: requestId, ok: false, reason: (e && (e.reason || e.message)) || "error", imp: "" }); }
    );
  },
};

autoAddDeps(CleanAdsPlugin, "$CleanAdsState");
mergeInto(LibraryManager.library, CleanAdsPlugin);
