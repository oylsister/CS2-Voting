namespace VotingAPI
{
    public interface IVotingAPI
    {
        public event Action OnVoteEnd;
        public void CreateVote(string question, List<string> chioce, int duration);
        public void CancelVote();
        public Dictionary<string, int> GetVoteResult();
    }
}