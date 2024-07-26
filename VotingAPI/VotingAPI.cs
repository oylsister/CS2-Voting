namespace VotingAPI
{
    public interface IVotingAPI
    {
        public void CreateVote(string question, List<string> chioce);
        public void CancelVote();
        public Dictionary<string, int> GetVoteResult();
    }
}