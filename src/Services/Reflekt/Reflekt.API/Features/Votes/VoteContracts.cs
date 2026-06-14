namespace Reflekt.API.Features.Votes;

public sealed record VoteResponse(int GroupId, int VoteCount, int MyVoteCount);

public sealed record VoteDistributionEntry(int GroupId, int Count);
