using System;
using System.Diagnostics;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model.Entities;
using SabberStoneBasicAI.AIAgents;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneBasicAI.PartialObservation
{
	class POGameHandler
	{
		private AbstractAgent player1;
		private AbstractAgent player2;

		private GameConfig gameConfig;
		private bool setupHeroes = true;

		private GameStats gameStats;
		private static readonly Random Rnd = new Random();
		private bool repeatDraws = false;
		private int maxTurns = 50;


		public POGameHandler(GameConfig gameConfig, AbstractAgent player1, AbstractAgent player2, bool setupHeroes = true, bool repeatDraws = false)
		{
			this.gameConfig = gameConfig;
			this.setupHeroes = setupHeroes;
			this.player1 = player1;
			player1.InitializeAgent();

			this.player2 = player2;
			player2.InitializeAgent();

			gameStats = new GameStats();
		}

		public bool PlayGame(bool addToGameStats = true, bool debug = false)
		{
			SabberStoneCore.Model.Game game = new SabberStoneCore.Model.Game(gameConfig, setupHeroes);
			//var game = new Game(gameConfig, setupHeroes);
			player1.InitializeGame();
			player2.InitializeGame();

			AbstractAgent currentAgent;
			Stopwatch currentStopwatch;
			POGame poGame;
			PlayerTask playertask = null;
			Stopwatch[] watches = new[] { new Stopwatch(), new Stopwatch() };
			bool printGame = false;

			game.StartGame();
#if DEBUG
			try
			{
#endif
				while (game.State != State.COMPLETE && game.State != State.INVALID)
				{
					//if (debug)
					//Console.WriteLine("Turn " + game.Turn);
					if (printGame)
					{
						//Console.WriteLine(MCGS.SabberHelper.SabberUtils.PrintGame(game));
						printGame = false;
					}

					if (game.Turn >= maxTurns)
						break;

					currentAgent = game.CurrentPlayer == game.Player1 ? player1 : player2;
					Controller currentPlayer = game.CurrentPlayer;
					currentStopwatch = game.CurrentPlayer == game.Player1 ? watches[0] : watches[1];
					poGame = new POGame(game, debug);

					currentStopwatch.Start();
					playertask = currentAgent.GetMove(poGame);
					currentStopwatch.Stop();

					game.CurrentPlayer.Game = game;
					game.CurrentOpponent.Game = game;

					if (debug)
					{
						//Console.WriteLine(playertask);
					}

					if (playertask.PlayerTaskType == PlayerTaskType.END_TURN)
						printGame = true;

					game.Process(playertask);
				}
#if DEBUG
			}
			catch (Exception e)
			//Current Player loses if he throws an exception
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
				game.State = State.COMPLETE;
				game.CurrentPlayer.PlayState = PlayState.CONCEDED;
				game.CurrentOpponent.PlayState = PlayState.WON;

				if (addToGameStats && game.State != State.INVALID)
					gameStats.registerException(game, e);
			}
#endif

			if (game.State == State.INVALID || (game.Turn >= maxTurns && repeatDraws))
				return false;

			if (addToGameStats)
				gameStats.addGame(game, watches);

			player1.FinalizeGame();
			player2.FinalizeGame();
			return true;
		}

		public void PlayGames(int nr_of_games, bool addResultToGameStats = true, bool debug = false)
		{
			//ProgressBar pb = new ProgressBar(nr_of_games, 30, debug);

			for (int i = 0; i < nr_of_games; i++)
			{
				if (!PlayGame(addResultToGameStats, debug))
					i -= 1;     // invalid game
								//pb.Update(i);
								//Console.WriteLine("play game " + i + " out of " + nr_of_games);
			}
		}

		public GameStats getGameStats()
		{
			return gameStats;
		}
	}

}
