﻿using Mars.Web;
using System.Collections.Concurrent;
using System.Text.Json;

public class MultiGameHoster
{
    public MultiGameHoster(IMapProvider mapProvider, ILogger<MultiGameHoster> logger)
    {
        ParsedMaps = new List<Map>(mapProvider.LoadMaps());
        this.logger = logger;
    }

    public void RaiseOldGamesPurged() => OldGamesPurged?.Invoke(this, EventArgs.Empty);

    public event EventHandler OldGamesPurged;
    public ConcurrentDictionary<string, GameManager> Games { get; } = new();
    public ConcurrentDictionary<string, string> TokenMap { get; } = new();

    private string nextGame = "a";
    private object lockObject = new();
    private readonly IWebHostEnvironment hostEnvironment;
    private readonly ILogger<MultiGameHoster> logger;

    public string MakeNewGame()
    {
        lock (lockObject)
        {
            var gameId = nextGame;
            Games.TryAdd(gameId, new GameManager(ParsedMaps));

            nextGame = IncrementGameId(nextGame);
            logger.LogInformation($"New Game Created: {gameId}");
            return gameId;
        }
    }

    public static string IncrementGameId(string nextGame)
    {
        var chars = nextGame.ToCharArray();

        if (chars.All(c => c == 'z'))
        {
            return new string('a', chars.Length + 1);
        }

        var lastIndex = chars.Length - 1;
        if (chars[lastIndex] < 'z')
        {
            chars[lastIndex]++;
            return new string(chars);
        }

        chars[lastIndex--] = 'a';
        while (lastIndex >= 0)
        {
            if (chars[lastIndex] < 'z')
            {
                chars[lastIndex]++;
                break;
            }
            else
            {
                chars[lastIndex--] = 'a';
            }
        }

        return new string(chars);
    }

    public List<Map> ParsedMaps { get; private set; } = new();
}

public class SerializedLowResolutionCell
{
    public int AverageDifficulty { get; set; }
    public int LowerLeftRow { get; set; }
    public int LowerLeftColumn { get; set; }
    public int UpperRightRow { get; set; }
    public int UpperRightColumn { get; set; }

    public static SerializedLowResolutionCell FromLowResCel(LowResolutionCell lowRes)
    {
        return new SerializedLowResolutionCell
        {
            AverageDifficulty = lowRes.AverageDifficulty.Value,
            LowerLeftColumn = lowRes.LowerLeftColumn,
            LowerLeftRow = lowRes.LowerLeftRow,
            UpperRightColumn = lowRes.UpperRightColumn,
            UpperRightRow = lowRes.UpperRightRow
        };
    }
}
