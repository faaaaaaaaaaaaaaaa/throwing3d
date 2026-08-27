using System;
using UnityEngine;

// Central ad manager. Rewarded ads are opt-in and never gated; interstitials only fire at natural
// breaks and are frequency-capped so a new player is never hit in their first session.
//
// ponytail: ShowRewardedInternal / ShowInterstitialInternal are stubs that "succeed" instantly.
// Upgrade path: swap the two internal methods for Unity LevelPlay
// (com.unity.services.levelplay) or Google AdMob. Everything else (pacing, consent, analytics
// funnel) is real and stays.
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("Interstitial pacing (seconds)")]
    [SerializeField] private float _interstitialMinGap = 300f;   // 5 minutes between interstitials
    [SerializeField] private float _interstitialBlockFirst = 90f; // no interstitial in first 90s

    public bool ConsentGranted { get; private set; }

    private AdFrequencyCap _interstitialCap;

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
            return;
        }

        _interstitialCap = new AdFrequencyCap(_interstitialMinGap, _interstitialBlockFirst);
    }

    // Wire this to your consent dialog (UMP / ATT). Ads should respect the result.
    public void SetConsent(bool granted) => ConsentGranted = granted;

    // Opt-in reward (revive, double coins, skin trial). Always available.
    public void ShowRewarded(string placement, Action onReward)
    {
        Analytics.AdOfferShown(placement);
        Analytics.AdStarted(placement);
        ShowRewardedInternal(() =>
        {
            Analytics.AdCompleted(placement);
            Analytics.AdRewardGranted(placement);
            onReward?.Invoke();
        });
    }

    // Returns false (and shows nothing) when the frequency cap blocks it, so callers never assume
    // an ad played. Only call at natural breaks (win/lose, back to menu).
    public bool TryShowInterstitial(string placement)
    {
        float now = Time.realtimeSinceStartup;
        if (!_interstitialCap.CanShow(now)) return false;

        _interstitialCap.MarkShown(now);
        Analytics.AdStarted(placement);
        ShowInterstitialInternal(() => Analytics.AdCompleted(placement));
        return true;
    }

    private void ShowRewardedInternal(Action onComplete)
    {
        Debug.Log("[Ads] (stub) rewarded ad watched");
        onComplete?.Invoke();
    }

    private void ShowInterstitialInternal(Action onComplete)
    {
        Debug.Log("[Ads] (stub) interstitial shown");
        onComplete?.Invoke();
    }
}

// Pure, testable pacing rule: blocks the opening window and enforces a minimum gap between shows.
public class AdFrequencyCap
{
    private readonly float _minSecondsBetween;
    private readonly float _blockFirstSeconds;
    private float _lastShown = float.NegativeInfinity;

    public AdFrequencyCap(float minSecondsBetween, float blockFirstSeconds)
    {
        _minSecondsBetween = minSecondsBetween;
        _blockFirstSeconds = blockFirstSeconds;
    }

    public bool CanShow(float now)
    {
        if (now < _blockFirstSeconds) return false;
        return now - _lastShown >= _minSecondsBetween;
    }

    public void MarkShown(float now) => _lastShown = now;
}
