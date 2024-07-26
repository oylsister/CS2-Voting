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

        public void CreateVote(string question, List<string> chioce)
        {
            _plugin.Question = question;
            _plugin.Choice = chioce;

            _plugin.VoteStart();
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
    }
}
