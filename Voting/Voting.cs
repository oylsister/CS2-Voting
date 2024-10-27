using System.Xml.Schema;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using VotingAPI;

namespace Voting
{
    public class Voting : BasePlugin
    {
        public override string ModuleName => "Voting Module";
        public override string ModuleAuthor => "Oylsister";
        public override string ModuleVersion => "1.4";
        public override string ModuleDescription => "Voting API for CounterStrikeSharp";

        public bool IsVotingNow = false;
        public Dictionary<string, VoteData> _voteData = new Dictionary<string, VoteData>();
        public Dictionary<CCSPlayerController, string> _clientChoice = new Dictionary<CCSPlayerController, string>();

        public string Question = null!;
        public List<string> Choice = null!;
        public bool Cancellable = true;
        public string Winner = null!;
        public int WinnerVote;

        public CounterStrikeSharp.API.Modules.Timers.Timer? _timer;
        public CounterStrikeSharp.API.Modules.Timers.Timer? _countdownTimer;
        public int _countdown;

        public static PluginCapability<IVotingAPI> APICapability = new("voting");

        VotingAPI? API { get; set; } = null!;

        public override void Load(bool hotReload)
        {
            API = new VotingAPI(this);

            Capabilities.RegisterPluginCapability(APICapability, () => API);

            AddCommand("css_vote", "Create Vote for player", VoteCommand);
            AddCommand("css_revote", "Revote Command", ReVoteCommand);
            AddCommand("css_cancelvote", "Cancel Vote Command", CancelVoteCommand);
        }

        [RequiresPermissions("@css/vote")]
        private void VoteCommand(CCSPlayerController? client, CommandInfo info)
        {
            if (IsVotingNow)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Voting] {ChatColors.White}Vote is now in progress!");
                return;
            }

            Question = info.GetArg(1);

            Choice = new List<string>();

            if (Choice.Count > 0)
                Choice.Clear();

            if (info.ArgCount < 3)
            {
                Choice.Add("Yes");
                Choice.Add("No");
            }

            else
            {
                for (int i = 2; i < info.ArgCount; i++)
                {
                    Choice.Add(info.GetArg(i));
                }
            }

            VoteStart();
        }

        [RequiresPermissions("@css/vote")]
        private void CancelVoteCommand(CCSPlayerController? client, CommandInfo info)
        {
            if (!IsVotingNow)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Voting] {ChatColors.White}There is currently has no vote now!");
                return;
            }

            if (!Cancellable)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Voting] {ChatColors.White}This vote is not cancellable!");
                return;
            }

            VoteEnd(true);
        }

        public void ReVoteCommand(CCSPlayerController? client, CommandInfo info)
        {
            if (!IsVotingNow)
            {
                info.ReplyToCommand($" {ChatColors.Green}[Voting] {ChatColors.White}There is currently has no vote now!");
                return;
            }

            if(client == null)
            {
                info.ReplyToCommand("[Voting] You can't vote for this one!");
                return;
            }

            if(_clientChoice.ContainsKey(client))
                _clientChoice.Remove(client);

            foreach(var data in _voteData)
            {
                if (data.Value.Voter.Contains(client))
                {
                    data.Value.Voter.Remove(client);
                    data.Value.VoteCount--;
                    break;
                }
            }

            CreateVoteMenu(client);
        }

        public void VoteStart(int duration = 20, bool cancellable = true)
        {
            if (IsVotingNow)
            {
                Logger.LogError("Cannot start vote because there is already a vote in progress!");
                return;
            }

            Cancellable = cancellable;

            IsVotingNow = true;

            RegisterListener<Listeners.OnTick>(OnGameFrame);

            _voteData.Clear();
            _clientChoice.Clear();

            for (int i = 0; i < Choice.Count; i++)
            {
                if (_voteData.ContainsKey(Choice[i]))
                {
                    Server.PrintToChatAll($" {ChatColors.Green}[Voting]{ChatColors.White} The vote is cancelled because there is a duplicated answers in votes!");
                    break;
                }
                _voteData.Add(Choice[i], new());
            }

            _timer = AddTimer(duration, () => VoteEnd());

            _countdown = duration;

            _countdownTimer = AddTimer(1f, () => {

                if(_countdown < 0)
                {
                    if(_countdownTimer != null)
                        _countdownTimer.Kill();

                    return;
                }

                _countdown--;
            }, TimerFlags.REPEAT);

            foreach (var client in Utilities.GetPlayers())
            {
                CreateVoteMenu(client);
            }
        }

        public void CreateVoteMenu(CCSPlayerController client)
        {
            var menu = new ChatMenu($"{ChatColors.Green}**** {ChatColors.White}Vote: {Question} {ChatColors.Green}****");

            for (int i = 0; i < Choice.Count; i++)
            {
                menu.AddMenuOption(Choice[i], CreateVoteMenuHandler);
            }

            MenuManager.OpenChatMenu(client, menu);

            _clientChoice.Add(client, string.Empty);
        }

        public void CreateVoteMenuHandler(CCSPlayerController client, ChatMenuOption option)
        {
            if(!IsVotingNow)
            {
                client.PrintToChat($" {ChatColors.Green}[Voting]{ChatColors.White} There is no vote in progress now!");
                return;
            }

            if (_voteData.ContainsKey(option.Text))
            {
                _voteData[option.Text].Voter.Add(client);
                _voteData[option.Text].VoteCount++;
                _clientChoice[client] = option.Text;

                client.PrintToChat($" {ChatColors.Green}[Voting]{ChatColors.White} You have vote for {ChatColors.Olive}{option.Text}");
            }
        }

        public void OnGameFrame()
        {
            if (!IsVotingNow)
                return;

            ShowVoteProgress();
        }

        public void ShowVoteProgress()
        {
            var message = $"Q: {Question}<br>Voting in Progress ({_countdown} Secs left.)";

            var item = 3;

            if (_voteData.Count < 3)
                item = 2;

            var topVote = _voteData.OrderByDescending(entry => entry.Value.VoteCount).Take(item).ToDictionary(pair => pair.Key, pair => pair.Value.VoteCount);

            Winner = topVote.First().Key;
            WinnerVote = topVote.First().Value;

            foreach (var entry in topVote)
            {
                message += $"<br>{entry.Key} - ({entry.Value})";
            }

            foreach (var client in Utilities.GetPlayers())
            {
                if (_clientChoice.ContainsKey(client))
                {
                    if (_clientChoice[client] == string.Empty || string.IsNullOrEmpty(_clientChoice[client]) || string.IsNullOrWhiteSpace(_clientChoice[client]))
                        ShowClientChoice(client);

                    else
                    {
                        var newMessage = $"{message} <br><font size=\"4\">You have voted: {_clientChoice[client]}</font>";
                        client.PrintToCenterHtml(newMessage);
                    }  
                }
                else
                    client.PrintToCenterHtml(message);
            }
        }

        public void ShowClientChoice(CCSPlayerController client)
        {
            var message = $"Q: {Question}<br>Vote Now! ({_countdown} Secs left.)";
            int choice = 1;

            foreach (var option in _voteData)
            {
                message += $"<br>!{choice} " + option.Key + $" [{option.Value.VoteCount}]";
                choice++;
            }

            client.PrintToCenterHtml(message);
        }

        public void VoteEnd(bool cancel = false)
        {
            IsVotingNow = false;
            Cancellable = true;

            RemoveListener<Listeners.OnTick>(OnGameFrame);

            if(_countdownTimer != null)
            {
                _countdownTimer.Kill();
            }

            if(_timer != null)
            {
                _timer.Kill();
            }

            if (cancel)
            {
                Server.PrintToChatAll($" {ChatColors.Green}[Voting]{ChatColors.White} Current vote has been cancelled.");
                return;
            }

            API!.CallOnVoteEnd();

            Server.PrintToChatAll($" {ChatColors.Green}[Voting] {ChatColors.Olive}{Winner} {ChatColors.White}is winning with {ChatColors.Olive}{WinnerVote} {ChatColors.White}votes!");
        }
    }
}

public class VoteData
{
    private List<CCSPlayerController> _voter;
    private int _count;

    public VoteData()
    {
        _voter = new List<CCSPlayerController>();
        _count = 0;
    }

    public List<CCSPlayerController> Voter
    {
        get { return _voter; }
        set { _voter = value; }
    }

    public int VoteCount
    {
        get { return _count; }
        set { _count = value; }
    }
}