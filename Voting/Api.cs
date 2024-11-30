using CounterStrikeSharp.API.Core;
using VotingAPI;

namespace Voting
{
    public class VotingAPI : IVotingAPI
    {
        Voting _plugin;

        public VotingAPI(Voting plugin)
        {  
            _plugin = plugin; 
        }

        public event Action<string>? OnVoteEnd;

        public void CreateVote(string question, List<string> chioce, int duration, bool cancellable = true, bool announceWinner = true)
        {
            _plugin.Question = question;
            _plugin.Choice = chioce;

            _plugin.VoteStart(duration, cancellable, announceWinner);
        }

        public void CancelVote()
        {
            _plugin.VoteEnd(true);
        }

        public Dictionary<string, int> GetVoteResult()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach(var data in _plugin._voteData)
            {
                result.Add(data.Key, data.Value.VoteCount);
            }

            return result;
        }

        public bool IsVoteInProgress()
        {
            return _plugin.IsVotingNow;
        }

        public void CallOnVoteEnd(string winner)
        {
            OnVoteEnd?.Invoke(winner);
        }
    }
}
