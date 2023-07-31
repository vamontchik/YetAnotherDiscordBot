using System;

namespace DiscordBot.Command.Gremlin;

internal class Gremlin
{
    private Gremlin()
    {
        Attack = 0;
        Defense = 0;
        Level = 0;
    }

    private static readonly Lazy<Gremlin> Lazy = new(() => new Gremlin());

    public static Gremlin Instance => Lazy.Value;

    private int Attack { get; set; }

    private int Defense { get; set; }

    private int Level { get; set; }

    private static readonly object UpdateLock = new();

    public void Update()
    {
        lock (UpdateLock)
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
        lock (UpdateLock)
            return $"Gremlin Level {Level}: Attack {Attack}, Defense: {Defense}";
    }
}