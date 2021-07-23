namespace DiscordBot.src
{
    class RPSPlayer
    {
        private readonly RPSType _type;
        private readonly string _name;
        public RPSPlayer(RPSType type, string name)
        {
            _type = type;
            _name = name;
        }

        public RPSType Type { get => _type; }
        public string Name { get => _name; }

    }
}
