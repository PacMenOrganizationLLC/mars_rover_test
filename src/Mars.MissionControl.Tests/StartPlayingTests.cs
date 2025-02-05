﻿namespace Mars.MissionControl.Tests;

public class StartPlayingTests
{
	[Test]
	public void StartPlaying()
	{
		var scenario = new GameScenario(height: 7, width: 7, players: 1);
		scenario.Game.PlayGame();
		scenario.Game.GameState.Should().Be(GameState.Playing);
	}

	[Test]
	public void Player1CanMoveAndGetBackBoardDetails()
	{
		var scenario = new GameScenario(height: 7, width: 7, players: 1);
		scenario.Game.PlayGame();
		var direction = scenario.Players[0].PlayerLocation switch
		{
			{ Y: 0 } => Direction.Right,
			{ X: 0 } => Direction.Forward,
			_ => Direction.Forward
		};
		var moveResult = scenario.Game.MovePerseverance(scenario.Players[0].Token, direction);
		moveResult.Should().NotBeNull();
	}

	[Test]
	public void PlayerCannotLeaveTheBoard()
	{
		var scenario = new GameScenario(height: 5, width: 5, players: 1);
		scenario.Game.PlayGame();
		var player = scenario.Players[0];
		var moveCount = 0;
		while (moveCount++ <= 6)
		{
			var moveResult = scenario.Game.MovePerseverance(player.Token, Direction.Forward);
			if (moveResult.Message == GameMessages.MovedOutOfBounds)
			{
				Assert.Pass("You were blocked from moving out of bounds");
				return;
			}
		}

		Assert.Fail("You were never blocked from moving out of bounds");
	}

	[TestCase(Orientation.North, Direction.Right, Orientation.East)]
	[TestCase(Orientation.East, Direction.Right, Orientation.South)]
	[TestCase(Orientation.South, Direction.Right, Orientation.West)]
	[TestCase(Orientation.West, Direction.Right, Orientation.North)]
	[TestCase(Orientation.North, Direction.Left, Orientation.West)]
	[TestCase(Orientation.West, Direction.Left, Orientation.South)]
	[TestCase(Orientation.South, Direction.Left, Orientation.East)]
	[TestCase(Orientation.East, Direction.Left, Orientation.North)]
	public void Turn(Orientation orig, Direction direction, Orientation final)
	{
		orig.Turn(direction).Should().Be(final);
	}

	[Test]
	public void WhenAPlayerMovesItsBatteryIsReducedByTheCellDifficultyAmount()
	{
		var scenario = new GameScenario(height: 7, width: 7, players: 1);
		scenario.Game.PlayGame();
		string log = "";

		var token = scenario.Players[0].Token;
		var origLocation = scenario.Players[0].PlayerLocation;
		for (int i = 0; i < 4; i++)
		{
			var moveResult = scenario.Game.MovePerseverance(token, Direction.Forward);
			var newLocation = moveResult.Location;
			if (newLocation == origLocation)//never moved
			{
				log += $"\nno-op, didn't move: battery={moveResult.BatteryLevel}";
			}
			else
			{
				var expectedBattery = scenario.Players[0].BatteryLevel - scenario.Game.Board[newLocation].Difficulty.Value;
				log += string.Format("\n{0} battery level - {1} - difficulty = {2} new battery",
					scenario.Players[0].BatteryLevel, scenario.Game.Board[newLocation].Difficulty.Value, expectedBattery);
				moveResult.BatteryLevel.Should().Be(expectedBattery, log);
				return;
			}

			//couldn't go forward? try turning.
			var result = scenario.Game.MovePerseverance(token, Direction.Right);
			log += $"\nturned right: battery={result.BatteryLevel}";
		}

		Assert.Fail("Apparently you never moved.");
	}

	[TestCase(Orientation.North, 5, 5, 5, 6)]
	[TestCase(Orientation.East, 5, 5, 6, 5)]
	[TestCase(Orientation.South, 5, 5, 5, 4)]
	[TestCase(Orientation.West, 5, 5, 4, 5)]
	public void GetCellInFront(Orientation orientation, int startRow, int startCol, int endRow, int endCol)
	{
		var player = new Player("P1") with { Orientation = orientation, PerseveranceLocation = new Location(startRow, startCol) };
		var front = player.CellInFront();
		front.Should().Be(new Location(endRow, endCol));
	}

	[TestCase(Orientation.North, 5, 5, 5, 4)]
	[TestCase(Orientation.East, 5, 5, 4, 5)]
	[TestCase(Orientation.South, 5, 5, 5, 6)]
	[TestCase(Orientation.West, 5, 5, 6, 5)]
	public void GetCellInBack(Orientation orientation, int startRow, int startCol, int endRow, int endCol)
	{
		var player = new Player("P1") with { Orientation = orientation, PerseveranceLocation = new Location(startRow, startCol) };
		var back = player.CellInBack();
		back.Should().Be(new Location(endRow, endCol));
	}

	[Test]
	public void IngenuityCanMove1space()
	{
		var game = Helpers.CreateGame(20, 20);
		game.PlayGame();
		var playerInfo = game.Join("P1");
		var destination = new Location(playerInfo.PlayerLocation.X + 1, playerInfo.PlayerLocation.Y + 1);
		var moveResult = game.MoveIngenuity(playerInfo.Token, 0, destination);
		moveResult.Location.Should().Be(destination);
	}

	[Test]
	public void IngenuityCannotMove3space()
	{
		var game = Helpers.CreateGame(20, 20);
		game.PlayGame();
		var playerInfo = game.Join("P1");
		var destination = new Location(playerInfo.PlayerLocation.X + 3, playerInfo.PlayerLocation.Y + 3);

		var moveResult = game.MoveIngenuity(playerInfo.Token, 0, destination);

		moveResult.Message.Should().BeOneOf(GameMessages.MovedOutOfBounds, GameMessages.IngenuityTooFar);
	}
}
