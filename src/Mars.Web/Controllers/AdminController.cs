using Mars.Web.Components;
using Microsoft.Extensions.Options;

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
	public IActionResult StartGame(StartGameRequest request)
	{
		if (request.Password != gameConfig.Password)
			return Problem("Invalid password", statusCode: 400, title: "Cannot start game with invalid password.");

		if (gameHoster.Games.TryGetValue(request.GameID, out var gameManager))
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

	[HttpPost("createSession")]
	public string MakeNewGame()
	{
		if (isMakingNewGame == false)
		{
			isMakingNewGame = true;
			string gameId = gameHoster.MakeNewGame();
			isMakingNewGame = false;
			return gameId;
		}
		else
		{
			Thread.Sleep(10000);
			return MakeNewGame();
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
	public required string GameID { get; set; }
}
