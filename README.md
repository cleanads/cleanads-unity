# CleanAds for Unity WebGL

Rewarded and interstitial ads for **Unity WebGL games** running as **Telegram Mini Apps** or on the web, from
[CleanAds](https://cleanads.ai). Publishers keep 70% of every billed dollar, paid out in TON or USDT.

- Thin C# API over the CleanAds web SDK, which loads automatically. No other dependencies.
- In Telegram, the user's signed `initData` is sent with every ad request so impressions can be verified.
- Unity 2021.3 or newer, WebGL build target.

Building a native Android or iOS game instead? Use the
[Android](https://github.com/cleanads/cleanads-android) or [iOS](https://github.com/cleanads/cleanads-ios) SDK.

## Install

**Window › Package Manager › + › Add package from git URL…** and paste:

```
https://github.com/cleanads/cleanads-unity.git#1.0.0
```

Get a placement id (`pl_...`) by creating a free publisher account at
[cleanads.ai/signup](https://cleanads.ai/signup?as=publisher). Each new app receives test ads right away.

## Rewarded ad

```csharp
using CleanAds;
using UnityEngine;

public class WatchAdButton : MonoBehaviour
{
    const string Placement = "pl_xxx";

    void Start()
    {
        // test: true serves free sample ads while you integrate. Remove it before release.
        CleanAdsSdk.Init(Placement, userId: PlayerProfile.Id, test: true);
    }

    public void OnClick()
    {
        CleanAdsSdk.Show(Placement, result =>
        {
            if (result.Success) GrantReward();
            else Debug.Log("No reward: " + result.Reason); // "no_fill", "closed_early", ...
        });
    }
}
```

The same call shows interstitials: the format comes from the placement. The callback runs on Unity's main thread.

## In the Editor

Ads only render in a WebGL build in the browser. In the Editor and on other platforms `Show` does not display
anything: with `test: true` it reports success (`ImpressionId = "imp_editor_test"`) so you can test your reward
flow, otherwise it reports `"unsupported_platform"`.

## Rewards of value

`result.Success` is fine for coins in a casual game. For anything of value, set a **reward callback URL** in the
publisher dashboard. CleanAds then calls your server once per completed rewarded view with an HMAC signature.
Grant the reward there, once per `imp_id`. See [server-side rewards](https://cleanads.ai/docs#callback).

## Result reasons

`no_fill`, `closed_early`, `network_error`, `media_error`, `media_timeout`, `already_showing`,
`not_initialized` (call `Init` first), `sdk_load_failed`, `unsupported_platform`, and the server reasons
`unknown_placement`, `app_not_active`, `placement_paused`, `invalid_init_data`, `rate_limited`.

Always continue the game when an ad fails.

## Links

- Docs: <https://cleanads.ai/docs#unity>
- Support: hello@cleanads.ai

MIT License
