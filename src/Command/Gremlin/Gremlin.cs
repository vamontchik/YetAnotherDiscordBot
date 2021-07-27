using System;

namespace DiscordBot.Command.Gremlin
{
    internal class Gremlin
    {
        #region Singleton
        
        private Gremlin()
        {
            Attack = 0;
            Defense = 0;
            Level = 1;
        }

        private static readonly Lazy<Gremlin> Lazy = new(() => new Gremlin());

        public static Gremlin Instance => Lazy.Value;

        #endregion
        
        #region Auto-Properties
        
        private int Attack { get; set; }

        private int Defense { get; set; }
        
        private int Level { get; set; }
        
        #endregion

        private readonly object _updateLock = new();

        public void Update()
        {
            lock (_updateLock)
                UpdateStats();
        }

        private void UpdateStats()
        {
            var rand = new Random();
            Attack += rand.Next(0, 10);
            Defense += rand.Next(0, 10);
            Level += 1;
        }

        public override string ToString()
        {
            return $"Gremlin Level {Level}: Attack {Attack}, Defense: {Defense}";
        }
    }
}