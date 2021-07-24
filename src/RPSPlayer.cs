namespace DiscordBot.src
{
    class RPSPlayer
    {
        public const ulong BOT_ID = 0;
        // public const ulong INVALID_ID = ulong.MaxValue;

        private readonly RPSType _type;
        private readonly string _name;
        private readonly ulong _id;
        public RPSPlayer(RPSType type, string name, ulong id)
        {
            _type = type;
            _name = name;
            _id = id;
        }

        public RPSType Type { get => _type; }
        public string Name { get => _name; }
        public ulong Id { get => _id; }

    }
}
