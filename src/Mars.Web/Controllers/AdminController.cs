using Mars.Web.Components;
using Microsoft.Extensions.Options;

using System.Text.Json;

namespace Mars.Web.Controllers;

/// <summary>
/// Administrator options for the game
/// </summary>
[ApiController]
[Route("[controller]")]
public class AdminController : ControllerBase
{
	private readonly GameConfig gameConfig;
	private readonly ILogger<AdminController> logger;
	private readonly MultiGameHoster gameHoster;
	private bool isMakingNewGame = false;

	public AdminController(IOptions<GameConfig> gameConfigOptions, ILogger<AdminController> logger, MultiGameHoster gameHoster)
	{
		this.gameConfig = gameConfigOptions.Value;
		this.logger = logger;
		this.gameHoster = gameHoster;
	}

	/// <summary>
	/// Allow an admin to begin game play without needing to interact with the game UI
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	[HttpPost("[action]")]
	[ProducesResponseType(typeof(string), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public IActionResult StartGame(StartGameRequest request, [FromQuery] string sessionId)
	{
		if (request.Password != gameConfig.Password)
			return Problem("Invalid password", statusCode: 400, title: "Cannot start game with invalid password.");

		if (gameHoster.Games.TryGetValue(sessionId, out var gameManager))
		{
			try
			{
				var gamePlayOptions = new GamePlayOptions
				{
					RechargePointsPerSecond = request.RechargePointsPerSecond,
				};
				logger.LogInformation("Starting game play via admin api");

				gameManager.Game.PlayGame(gamePlayOptions);
				return Ok("Game started OK");
			}
			catch (Exception ex)
			{
				return Problem(ex.Message, statusCode: 400, title: "Error starting game");
			}
		}

		return Problem("Invalid GameID", statusCode: 400, title: "Invalid Game ID");
	}

	[HttpPost("startGame")]
	[ProducesResponseType(typeof(string), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public IActionResult StartGame([FromQuery] string password, [FromQuery] string gameID, [FromQuery] int rechargePointsPerSecond)
	{
		if (password != gameConfig.Password)
			return Problem("Invalid password", statusCode: 400, title: "Cannot start game with invalid password.");

		if (gameHoster.Games.TryGetValue(gameID, out var gameManager))
		{
			try
			{
				var gamePlayOptions = new GamePlayOptions
				{
					RechargePointsPerSecond = rechargePointsPerSecond,
				};
				logger.LogInformation("Starting game play via admin api");

				gameManager.Game.PlayGame(gamePlayOptions);
				return Ok("Game started OK");
			}
			catch (Exception ex)
			{
				return Problem(ex.Message, statusCode: 400, title: "Error starting game");
			}
		}

		return Problem("Invalid GameID", statusCode: 400, title: "Invalid Game ID");
	}

	[HttpPost("createSession")]
	public string MakeNewGame()
	{
		if (!isMakingNewGame)
		{
			isMakingNewGame = true;
			string gameId = gameHoster.MakeNewGame();
			isMakingNewGame = false;

			// Creating a JSON object
			var jsonObject = new
			{
				GameId = gameId,
				GameUrl = "https://marswebpacmen.azurewebsites.net/game/"+gameId
			};

			// Converting JSON object to string
			string jsonString = JsonSerializer.Serialize(jsonObject);

			return jsonString;
		}
		else
		{
			Thread.Sleep(5000);
			return MakeNewGame();
		}
	}


	[HttpGet("config")]
	public ActionResult<List<GameConfigTemplate>> getGameConfigTemplate()
	{
		var sampleData = new List<GameConfigTemplate>
		{
			new("RechargePointsPerSecond", "20"),
			new("Password", "password")
		};

		return Ok(sampleData);
	}

	public class GameConfigTemplate
	{
		public string Key { get; set; }
		public string Value { get; set; }
		public GameConfigTemplate(string key, string value)
		{
			Key = key;
			Value = value;
		}
	}
}

/// <summary>
/// Configuration options that can change per game instance
/// </summary>
public class StartGameRequest
{
	/// <summary>
	/// How many battery points each player receives every second
	/// </summary>
	public int RechargePointsPerSecond { get; set; }
	/// <summary>
	/// Password required to create a new game or begin game play
	/// </summary>
	public string? Password { get; set; }
	/// <summary>
	/// Which game instance you are starting/connecting to
	/// </summary>
//	public required string GameID { get; set; }
}
