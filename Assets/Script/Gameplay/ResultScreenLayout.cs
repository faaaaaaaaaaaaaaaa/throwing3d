// Pure layout rules for the post-match result screen. Kept allocation-free so SelfCheck can
// assert win/lose/campaign-complete without spinning up MonoBehaviours.
public readonly struct ResultScreenLayout
{
    public readonly string Title;
    public readonly bool ShowNext;
    public readonly bool ShowRetry;
    public readonly bool ShowRevive;
    public readonly bool ShowDoubleCoins;
    public readonly bool NextIsMenu;
    public readonly bool RetryIsCampaignRestart;
    public readonly string NextLabel;
    public readonly string RetryLabel;

    public ResultScreenLayout(
        string title,
        bool showNext,
        bool showRetry,
        bool showRevive,
        bool showDoubleCoins,
        bool nextIsMenu,
        bool retryIsCampaignRestart,
        string nextLabel,
        string retryLabel)
    {
        Title = title;
        ShowNext = showNext;
        ShowRetry = showRetry;
        ShowRevive = showRevive;
        ShowDoubleCoins = showDoubleCoins;
        NextIsMenu = nextIsMenu;
        RetryIsCampaignRestart = retryIsCampaignRestart;
        NextLabel = nextLabel;
        RetryLabel = retryLabel;
    }

    public static ResultScreenLayout For(bool won, bool campaignComplete, int coinsPerWin)
    {
        if (won && campaignComplete)
        {
            return new ResultScreenLayout(
                title: $"Campaign Complete!\n+{coinsPerWin} coins",
                showNext: true,
                showRetry: true,
                showRevive: false,
                showDoubleCoins: true,
                nextIsMenu: true,
                retryIsCampaignRestart: true,
                nextLabel: "Menu",
                retryLabel: "Retry Campaign");
        }

        if (won)
        {
            return new ResultScreenLayout(
                title: $"Victory!\n+{coinsPerWin} coins",
                showNext: true,
                showRetry: false,
                showRevive: false,
                showDoubleCoins: true,
                nextIsMenu: false,
                retryIsCampaignRestart: false,
                nextLabel: "Next",
                retryLabel: "Retry");
        }

        return new ResultScreenLayout(
            title: "Defeated...",
            showNext: false,
            showRetry: true,
            showRevive: true,
            showDoubleCoins: false,
            nextIsMenu: false,
            retryIsCampaignRestart: false,
            nextLabel: "Next",
            retryLabel: "Retry");
    }

    // Used by SelfCheck / play validation: do the four button activeSelf flags match this layout?
    public static bool MatchesVisibility(
        ResultScreenLayout layout,
        bool nextActive,
        bool retryActive,
        bool reviveActive,
        bool doubleCoinsActive)
    {
        return nextActive == layout.ShowNext
               && retryActive == layout.ShowRetry
               && reviveActive == layout.ShowRevive
               && doubleCoinsActive == layout.ShowDoubleCoins;
    }
}
