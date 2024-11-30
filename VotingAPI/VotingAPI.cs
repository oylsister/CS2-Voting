using CounterStrikeSharp.API.Core;

namespace VotingAPI
{
    public interface IVotingAPI
    {
        public event Action<string>? OnVoteEnd;
        public void CreateVote(string question, List<string> chioce, int duration, bool cancellable = true, bool announceWinner = true);
        public void CancelVote();
        public Dictionary<string, int> GetVoteResult();
        public bool IsVoteInProgress();
    }
}