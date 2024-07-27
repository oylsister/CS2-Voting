using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using VotingAPI;

namespace VotingTest
{
    public class VotingTest : BasePlugin
    {
        public override string ModuleName => "Voting Test";
        public override string ModuleAuthor => "Oylsister";
        public override string ModuleVersion => "1.0";
        public override string ModuleDescription => "Testing VotingAPI";

        public List<string> _maps = new List<string>();

        public IVotingAPI? VotingAPI { get; set; }

        public static PluginCapability<IVotingAPI> Capability { get; } = new("voting");

        public override void Load(bool hotReload)
        {
            AddCommand("css_mapvote", "Map Vote Command", MapVoteCommand);
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            VotingAPI = Capability.Get()!;

            VotingAPI.OnVoteEnd += OnVoteEnd;
        }

        public void MapVoteCommand(CCSPlayerController? client, CommandInfo info)
        {
            _maps.Clear();

            _maps.Add("de_dust2");
            _maps.Add("ze_icecap_escape_p");
            _maps.Add("cs_office");

            VotingAPI!.CreateVote("Vote Next Map", _maps, 20);
        }

        public void OnVoteEnd()
        {
            var votelist = VotingAPI!.GetVoteResult();

            foreach (var map in votelist)
            {
                Server.PrintToChatAll($"{map.Key} get {map.Value}");
            }
        }
    }
}
