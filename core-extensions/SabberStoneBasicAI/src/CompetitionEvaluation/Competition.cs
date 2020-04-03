using SabberStoneBasicAI.AIAgents;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SabberStoneCore.Config;
using SabberStoneBasicAI.PartialObservation;
using System.Collections.Concurrent;

namespace SabberStoneBasicAI.CompetitionEvaluation
{

	public class Matchup
	{
		public Agent agent_player_1 { get; internal set; }
		public Agent agent_player_2 { get; internal set; }

		public Matchup(Agent player1, Agent player2)
		{
			agent_player_1 = player1;
			agent_player_2 = player2;
		}
	};

	public class MatchupResult
	{
		public int WinsPlayer1 { get; private set; }
		public int WinsPlayer2 { get; private set; }
		public int ExceptionsPlayer1 { get; private set; }
		public int ExceptionsPlayer2 { get; private set; }
		public int GamesPlayed {get; private set; }

		public MatchupResult(int WinsPlayer1 = 0, int WinsPlayer2 = 0, int ExceptionsPlayer1 = 0, int ExceptionsPlayer2 = 0)
		{
			this.WinsPlayer1 = 0;
			this.WinsPlayer2 = 0;
			this.ExceptionsPlayer1 = 0;
			this.ExceptionsPlayer2 = 0;
			GamesPlayed = WinsPlayer1 + WinsPlayer2 + ExceptionsPlayer1 + ExceptionsPlayer2;
		}

		public void addResult(bool player1won, bool player2won, bool exceptionplayer1, bool exceptionplayer2)
		{
			if (player1won == player2won)
				throw new Exception($"both players have the same winning status");
			if (exceptionplayer1 && exceptionplayer2)
				throw new Exception($"both players had an exception");
			if (player1won && exceptionplayer1)
				throw new Exception($"Player 1 cannot win and throw an exception at the same time");
			if (player2won && exceptionplayer2)
				throw new Exception($"Player 2 cannot win and throw an exception at the same time");

			if (player1won)
			{
				WinsPlayer1 += 1;
				if (exceptionplayer2) ExceptionsPlayer2 += 1;
			} else
			{
				WinsPlayer2 += 1;
				if (exceptionplayer1) ExceptionsPlayer1 += 1;
			}

			GamesPlayed += 1;
		}

	}

	public class Deck
	{
		public List<Card> cards { get; internal set; }
		public CardClass cardclass { get; internal set; }
		public string deckname { get; internal set; }

		public Deck(List<Card> cards, CardClass cardclass, string deckname)
		{
			this.cards = cards;
			this.cardclass = cardclass;
			this.deckname = deckname;
		}
	}

	public class Agent
	{
		public System.Type AgentClass { get; internal set; }
		public string AgentAuthor { get; internal set; }

		public Agent(System.Type agentClass, string agentAuthor)
		{
			this.AgentClass = agentClass;
			this.AgentAuthor = agentAuthor;
		}
	}
	public struct EvaluationTask
	{
		public int game_number { get; private set; }
		public int idx_player_1 { get; private set; }
		public int idx_player_2 { get; private set; }
		public int idx_deck_1 { get; private set; }
		public int idx_deck_2 { get; private set; }

		public EvaluationTask(int game_number, int idx_player_1, int idx_player_2, int idx_deck_1, int idx_deck_2)
		{
			this.game_number = game_number;
			this.idx_player_1 = idx_player_1;
			this.idx_player_2 = idx_player_2;
			this.idx_deck_1 = idx_deck_1;
			this.idx_deck_2 = idx_deck_2;
		}
	}


	public class RoundRobinCompetition
	{
		Agent[] agents;
		Deck[] decks;
		MatchupResult[,,,] results;
		ConcurrentQueue<EvaluationTask> tasks;
		string CompetitionType = "RoundRobin_DeckPlaying";

		public RoundRobinCompetition(Agent[] agents, Deck[] decks, string resultfile)
		{
			//store agents and decks
			this.agents = agents;
			this.decks = decks;


			if (Directory.Exists(resultfile))
				results = LoadPreviousResults(resultfile);
			else
			{
				InitializeResults();
			}
		}

		private void InitializeResults()
		{
			results = new MatchupResult[agents.Length, agents.Length, decks.Length, decks.Length];
			for (int player_1 = 0; player_1 < results.GetLength(0); player_1++)
			{
				for (int player_2 = 0; player_2 < results.GetLength(1); player_2++)
				{
					if (player_1 == player_2)
						continue;

					for (int deck_1 = 0; deck_1 < results.GetLength(2); deck_1++)
					{
						for (int deck_2 = 0; deck_2 < results.GetLength(3); deck_2++)
						{
							results[player_1, player_2, deck_1, deck_2] = new MatchupResult();
						}
					}
				}
			}
		}

		public void CreateTasks(int games_per_matchup = 10)
		{
			tasks = new ConcurrentQueue<EvaluationTask>();

			// process matchups equally to allow for a continuous evaluation until convergence
			for (int nr_of_game = 0; nr_of_game < games_per_matchup; nr_of_game++)
			{
				for (int player_1 = 0; player_1 < results.GetLength(0); player_1++)
				{
					for (int player_2 = 0; player_2 < results.GetLength(1); player_2++)
					{
						if (player_1 == player_2)
							continue;

						for (int deck_1 = 0; deck_1 < results.GetLength(2); deck_1++)
						{
							for (int deck_2 = 0; deck_2 < results.GetLength(3); deck_2++)
							{
								if (results[player_1, player_2, deck_1, deck_2].GamesPlayed <= nr_of_game)
									tasks.Enqueue(new EvaluationTask(nr_of_game, player_1, player_2, deck_1, deck_2));
							}
						}
					}
				}
			}
		}

		public MatchupResult[,,,] LoadPreviousResults(string resultfile)
		{
			//Check if file signature is matching with the agent and decklist and load results
			using (StreamReader sr = File.OpenText(resultfile))
			{
				string s;
				string CompetitionName = sr.ReadLine();
				if (CompetitionName != this.CompetitionType)
					throw new Exception($"this resultfile does not correspond to the current competition type ({CompetitionName}!={CompetitionType})");
				sr.ReadLine(); // skip empty line

				if ((s= sr.ReadLine()) == "Agents")
				{
					int agent_idx = 0;
					while ((s = sr.ReadLine()) != "")
					{
						if (s != agents[agent_idx].AgentAuthor)
							throw new Exception($"Agent with index {agent_idx} not matching competition agents ({s}!={agents[agent_idx].AgentAuthor})");
					}
				} else
				{
					throw new Exception($"Expected 'Agents' but found {s}");
				}
				sr.ReadLine(); // skip empty line


				if ((s = sr.ReadLine()) == "Decks")
				{
					int deck_idx = 0;
					while ((s = sr.ReadLine()) != "")
					{
						if (s != decks[deck_idx].deckname)
							throw new Exception($"Deck with index {deck_idx} not matching competition decks ({s}!={decks[deck_idx].deckname})");
					}
				}
				else
				{
					throw new Exception($"Expected 'Decks' but found {s}");
				}
				sr.ReadLine(); // skip empty line

				if ((s = sr.ReadLine()) == "MatchResults")
				{
					while ((s = sr.ReadLine()) != null)
					{
						string[] line = s.Split(";");
						throw new Exception($"Loading Match Results is not implemented yet!");
					}
				}
				else
				{
					throw new Exception($"Expected 'Match Results' but found {s}");
				}

			}
			return new MatchupResult[agents.Length, agents.Length, decks.Length, decks.Length];
		}

		public void startEvaluation(int nr_of_threads = 1)
		{
			//create overall progress bar that keeps track of all finished matchups
			if (nr_of_threads > 1)
			{
				/*
				int y = 0;
				Parallel.ForEach(tasks, new ParallelOptions { MaxDegreeOfParallelism = nr_of_threads },
					(EvaluationTask matchup) =>
				{
					processMatchup(matchup);
					Interlocked.Increment(ref y);
				});*/
				Thread[] threads = new Thread[nr_of_threads];
				for (int i = 0; i < nr_of_threads; i++)
				{
					
					Thread worker = new Thread(() => processMatchupsUntilDone());
					threads[i] = worker;
					worker.Start();
				}
				for (int i = 0; i < nr_of_threads; i++)
				{
					threads[i].Join();
				}
			}
			else
			{
				foreach (EvaluationTask task in tasks)
					processMatchup(task);
			}
		}

		public void processMatchupsUntilDone()
		{
			EvaluationTask task;
			while (!tasks.IsEmpty)
			{
				bool successfull = tasks.TryDequeue(out task);
				if (successfull)
					processMatchup(task);
			}
				
		}

		public void processMatchup(EvaluationTask task)
		{
			Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}, evaluates games {task.game_number + 1} of " +
				$"({this.agents[task.idx_player_1].AgentAuthor}, {this.agents[task.idx_player_2].AgentAuthor}), " +
				$"({this.decks[task.idx_deck_1].deckname}, {this.decks[task.idx_deck_2].deckname})");


			AbstractAgent player1 = Activator.CreateInstance(agents[task.idx_player_1].AgentClass) as AbstractAgent;
			AbstractAgent player2 = Activator.CreateInstance(agents[task.idx_player_2].AgentClass) as AbstractAgent;

			GameConfig gameConfig = new GameConfig
			{
				StartPlayer = 1,
				Player1HeroClass = decks[task.idx_deck_1].cardclass,
				Player2HeroClass = decks[task.idx_deck_2].cardclass,
				Player1Deck = decks[task.idx_deck_1].cards,
				Player2Deck = decks[task.idx_deck_1].cards,
			};

			GameStats gameStats = null;
			while (gameStats == null || (gameStats.PlayerA_Wins == 1) == (gameStats.PlayerB_Wins == 1))
			{
				var gameHandler = new POGameHandler(gameConfig, player1, player2);
				gameHandler.PlayGames(1);
				gameStats = gameHandler.getGameStats();
			}

			results[task.idx_player_1, task.idx_player_2, task.idx_deck_1, task.idx_deck_2].addResult(
				gameStats.PlayerA_Wins == 1,
				gameStats.PlayerB_Wins == 1,
				gameStats.PlayerA_Exceptions == 1,
				gameStats.PlayerB_Exceptions == 1
			);
		}

		public int GetTotalGamesPlayed()
		{
			int gamesPlayed = 0;
			for (int player_1 = 0; player_1 < results.GetLength(0); player_1++)
			{
				for (int player_2 = 0; player_2 < results.GetLength(1); player_2++)
				{
					if (player_1 == player_2)
						continue;

					for (int deck_1 = 0; deck_1 < results.GetLength(2); deck_1++)
					{
						for (int deck_2 = 0; deck_2 < results.GetLength(3); deck_2++)
						{
							gamesPlayed += results[player_1, player_2, deck_1, deck_2].GamesPlayed;
						}
					}
				}
			}
			return gamesPlayed;
		}

		public void PrintAgentStats()
		{
			for (int player_1 = 0; player_1 < agents.Length; player_1++)
			{
				int games_played = 0;
				int games_won = 0;
				int games_lost_by_exception = 0;
				for (int player_2 = 0; player_2 < agents.Length; player_2++)
				{
					if (player_1 == player_2) continue;

					for (int deck_1 = 0; deck_1 < decks.Length; deck_1++)
						for (int deck_2 = 0; deck_2 < decks.Length; deck_2++)
						{
							// first round
							games_played += results[player_1, player_2, deck_1, deck_2].GamesPlayed;
							games_won += results[player_1, player_2, deck_1, deck_2].WinsPlayer1;
							games_lost_by_exception += results[player_1, player_2, deck_1, deck_2].ExceptionsPlayer1;

							// second round
							games_played += results[player_2, player_1, deck_2, deck_1].GamesPlayed;
							games_won += results[player_2, player_1, deck_2, deck_1].WinsPlayer2;
							games_lost_by_exception += results[player_2, player_1, deck_2, deck_1].ExceptionsPlayer2;
						}
				}
				Console.WriteLine($"Agent {agents[player_1].AgentAuthor}'s win-rate={Math.Round(((float)games_won)/games_played,2)} " +
					$"({games_won} out of {games_played} games). {games_lost_by_exception} were lost by Exception.");
			}
		}

		public void PrintDetailedWinratesOfAgent(int agent)
		{
			throw new Exception("not implemented yet");
		}


		public void PrintDetailedDeckWinratesOfAgent(int agent)
		{
			throw new Exception("not implemented yet");
		}

	}

}
