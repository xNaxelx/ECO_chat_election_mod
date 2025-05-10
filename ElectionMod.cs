namespace Eco.Mods
{
    using Eco.Core.Systems;
    using Eco.Gameplay.Civics;
    using Eco.Gameplay.Civics.Elections;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Settlements;
    using Eco.Gameplay.Systems.Messaging.Chat.Commands;
    using Eco.Shared.Localization;
    using Eco.Shared.Utils;
    using System.Collections.Generic;
    using System.Linq;
    using Eco.Core.Utils;
    using Eco.Gameplay.Civics.Objects;
    using Eco.Simulation.Agents.AI;

    [ChatCommandHandler]
    public static class VotingCommands
    {
        [ChatSubCommand("Vote", "Vote for a candidate in an election.", "vote1", ChatAuthorizationLevel.User)]
        public static void Vote(User user, int electionIndex, int candidateId)
        {
            var elections = GetAllElections().ToList();
            if (electionIndex < 0 || electionIndex >= elections.Count)
            {
                user.Player.MsgLocStr($"Invalid election index {electionIndex}. Use /elections1 to list available elections.");
                return;
            }

            var (settlement, election) = elections[electionIndex];

            // Check if the user can vote based on settlement
            if (!CanUserVote(user, settlement))
            {
                user.Player.MsgLocStr($"You are not eligible to vote in election for settlement '{settlement.MarkedUpName}'.");
                return;
            }

            Result result;

            if (candidateId != -1)
            {
                result = election.Vote(new UserRunoffVote(user, new ElectionChoiceID(candidateId)));
            }
            else
            {
                user.Player.MsgLocStr($"No candidate found with ID {candidateId} in election for settlement '{settlement.MarkedUpName}'.");
                result = election.Vote(new UserRunoffVote(user));
            }

            if (result.Success)
            {
                user.Player.MsgLocStr($"Successfully voted for {candidateId} in election for settlement '{settlement.MarkedUpName}'.");
            }
            else
            {
                user.Player.MsgLocStr($"Failed to vote: {result.Message}");
            }
        }

        [ChatSubCommand("Vote", "List of all ongoing elections in your settlements", "elections1", ChatAuthorizationLevel.User)]
        public static void ListElections(User user)
        {
            var elections = GetAllElections().ToList();

            if (GetAllElections()
                .Where(e => CanUserVote(user, e.Settlement))
                .ToList()
                .Count()
                == 0)
            {
                user.Player.MsgLocStr("There are no ongoing elections you can vote in.");
                return;
            }

            var message = new LocStringBuilder();
            message.AppendLineLoc($"Ongoing elections count: {elections.Count}");

            int i = 0;
            foreach (var el in elections)
            {
                message.AppendLineLoc($"Settlement: {el.Settlement.MarkedUpName}, Election index: {i}, Election name: {el.Election.MarkedUpName})");
                foreach (var candidate in el.Election.Choices)
                {
                    message.AppendLineLoc($"CandidateName: {candidate.MarkedUpName}, ID: {candidate.ID.Id}");
                }
                i++;
            }

            user.Player.Msg(message.ToLocString());
        }

        private static bool CanUserVote(User user, Settlement settlement)
        {
            // Check if the user is a citizen of the settlement
            return settlement != null && settlement.Citizens.Contains(user);
        }

        private static IEnumerable<(Settlement Settlement, Election Election)> GetAllElections()
        {
            var electionManager = ElectionManager.Obj;
            var settlements = Registrars.Get<Settlement>().All();
            foreach (var settlement in settlements)
            {
                var elections = electionManager.CurrentElections(settlement);
                foreach (var election in elections)
                {
                    yield return (settlement, election);
                }
            }
        }
    }
}