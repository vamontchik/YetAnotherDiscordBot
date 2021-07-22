using Discord.WebSocket;
using System;
using System.Collections.Generic;

namespace DiscordBot.src
{
    enum RPSType
    {
        Rock, Paper, Scissors
    }

    static class RPSTypeExtensions
    {
        public static int Compare(this RPSType ours, RPSType theirs)
        {
            if (ours == RPSType.Rock && theirs == RPSType.Scissors || 
                ours == RPSType.Paper && theirs == RPSType.Rock || 
                ours == RPSType.Scissors && theirs == RPSType.Paper
            )
                return 1;

            if (theirs == RPSType.Rock && ours == RPSType.Scissors || 
                theirs == RPSType.Paper && ours == RPSType.Rock || 
                theirs == RPSType.Scissors && ours == RPSType.Paper
            )
                return -1;

            return 0;
        }
    }

    class RockPaperScissorsCommand : ICommand
    {
        private readonly string _userChoice;
        private readonly Dictionary<string, RPSType> _strChoices;
        private readonly Dictionary<int, RPSType> _choices;
        private readonly Random _rnd;

        private readonly ISocketMessageChannel _destChannel;

        public RockPaperScissorsCommand(string userChoice, ISocketMessageChannel messageChannel)
        {
            _rnd = new Random();
            _choices = new Dictionary<int, RPSType>
            {
                { 0, RPSType.Rock },
                { 1, RPSType.Paper },
                { 2, RPSType.Scissors }
            };
            _strChoices = new Dictionary<string, RPSType>
            {
                { "rock", RPSType.Rock },
                { "paper", RPSType.Paper },
                { "scissors", RPSType.Scissors }
            };
            _userChoice = userChoice;
            _destChannel = messageChannel;
        }

        public void Execute()
        {
            if (!ValidUserChoice(_userChoice))
            {
                SendErrorMessage();
                return;
            }

            var botChoice = _rnd.Next(Enum.GetNames(typeof(RPSType)).Length);
            var botChoiceAsType = _choices[botChoice];
            var userChoiceAsType = _strChoices[_userChoice];
            var winner = GetWinner(userChoiceAsType, botChoiceAsType);

            // BLOCK >:D
            _destChannel.SendMessageAsync($"User: {userChoiceAsType}, Bot: {botChoiceAsType}, Winner: {winner}").Wait();
        }

        private bool ValidUserChoice(string userChoice)
        {
            return _strChoices.ContainsKey(_userChoice.ToLower());
        }

        private void SendErrorMessage()
        {
            _destChannel.SendMessageAsync($"That's not a rock, paper, or scissors!").Wait();
        }

        private string GetWinner(RPSType first, RPSType second)
        {
            var compare = first.Compare(second);

            if (compare == -1)
                return second.ToString();
            else if (compare == 1)
                return first.ToString();
            else
                return "Tie!";
        }
    }
}
