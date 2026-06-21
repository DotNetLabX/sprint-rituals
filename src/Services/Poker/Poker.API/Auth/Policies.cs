namespace Poker.API.Auth;

public static class Policies
{
    /// <summary>Authenticated Identity cookie — SM-only actions (create session, decide, skip, close).</summary>
    public const string ScrumMaster = "PokerScrumMaster";

    /// <summary>Poker guest token — authenticated voter (Google or guest).</summary>
    public const string Voter = "PokerVoter";

    /// <summary>Cookie OR Poker guest token — any session participant.</summary>
    public const string ScrumMasterOrVoter = "PokerScrumMasterOrVoter";
}
