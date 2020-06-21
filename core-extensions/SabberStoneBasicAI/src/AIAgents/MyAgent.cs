using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneCore.Enums;
using SabberStoneCore.Model.Entities;


// TODO choose your own namespace by setting up <submission_tag>
// each added file needs to use this namespace or a subnamespace of it
namespace SabberStoneBasicAI.AIAgents.TYamaoka
{
	class Node
	{
		public Node() { }
		public Node(Node parent, PlayerTask action)
		{
			this.parent = parent;
			this.action = action;
		}

		public int visitCount = 0;
		public float totalValue = 0;
		public PlayerTask action;
		public Node parent;
		public List<Node> children = null;
	}

	class CustomScore : Score.Score
	{
		readonly double[] scaling = new double[] {
				21.5,
				33.6,
				41.1,
				19.4,
				54,
				60.5,
				88.5,
				84.7
		};

		public override int Rate()
		{
			if (OpHeroHp < 1)
				return Int32.MaxValue;

			if (HeroHp < 1)
				return Int32.MinValue;

			double score = 0.0;

			score += scaling[0] * this.HeroHp;
			score -= scaling[1] * this.OpHeroHp;

			score += scaling[2] * this.BoardZone.Count;
			score -= scaling[3] * this.OpBoardZone.Count;

			foreach (var boardZoneEntry in BoardZone)
			{
				score += scaling[4] * boardZoneEntry.Health;
				score += scaling[5] * boardZoneEntry.AttackDamage;
			}

			foreach (var boardZoneEntry in OpBoardZone)
			{
				score -= scaling[6] * boardZoneEntry.Health;
				score -= scaling[7] * boardZoneEntry.AttackDamage;
			}

			return (int)Math.Round(score);
		}

		public override Func<List<IPlayable>, List<int>> MulliganRule()
		{
			return p => p.Where(t => t.Cost > 3).Select(t => t.Id).ToList();
		}
	}

	class MyAgent : AbstractAgent
	{
		// parameters
		private float C_UCT = 1.0f;
		private int NUM_SIMULATIONS = 3;
		private float FPU = 5.0f;

		Stopwatch stopwatchForThisTurn = new Stopwatch();
		int movesInThisTurn = 0;

		public override void InitializeAgent()
		{
		}

		public override void FinalizeAgent()
		{
		}

		public override void InitializeGame()
		{
		}

		public override void FinalizeGame()
		{
		}

		public override PlayerTask GetMove(POGame poGame)
		{
			var player = poGame.CurrentPlayer;
			// Implement a simple Mulligan Rule
			if (player.MulliganState == Mulligan.INPUT)
			{
				List<int> mulligan = new CustomScore().MulliganRule().Invoke(player.Choice.Choices.Select(p => poGame.getGame().IdEntityDic[p]).ToList());
				return ChooseTask.Mulligan(player, mulligan);
			}

			PlayerTask bestAction = null;

			if (poGame.CurrentPlayer.Options().Count == 1)
			{
				bestAction = poGame.CurrentPlayer.Options()[0];
			}
			else
			{
				stopwatchForThisTurn.Start();

				List<PlayerTask> taskToSimulate = new List<PlayerTask>(1);
				taskToSimulate.Add(null);
				Node root = new Node();
				POGame poGameRoot = poGame;

				Expand(root, poGameRoot);

				long think_time = (30 * 1000 - stopwatchForThisTurn.ElapsedMilliseconds) / Math.Max(3, 10 - movesInThisTurn);
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				while (stopwatch.ElapsedMilliseconds <= think_time)
				//for (int itr = 0; itr < 100; ++itr)
				{
					Node node = root;
					poGame = poGameRoot;

					// traverse
					do
					{
						var nodeNext = Select(node);

						taskToSimulate[0] = nodeNext.action;
						// Until the end of my own turn
						if (nodeNext.action.PlayerTaskType != PlayerTaskType.END_TURN)
						{
							var poGameNext = poGame.Simulate(taskToSimulate)[nodeNext.action];
							if (poGameNext == null)
							{
								// May be null because the transition is probabilistic
								node.children.Remove(nodeNext);
								continue;
							}
							poGame = poGameNext;
						}
						node = nodeNext;
					} while (node.children != null);

					if (node.action.PlayerTaskType != PlayerTaskType.END_TURN)
					{
						Expand(node, poGame);

						// Since the transition is probabilistic, the expected value is obtained by performing multiple simulations.
						float value = 0;
						for (int i = 0; i < NUM_SIMULATIONS; ++i)
						{
							value += Simulate(node, poGame);
						}
						Backup(node, value / NUM_SIMULATIONS);
					}
					else
					{
						float value;
						if (node.visitCount == 0)
							value = ScoreToValue(Score(poGame));
						else
							value = node.totalValue / node.visitCount;
						Backup(node, value);
					}
				}
				stopwatch.Stop();
				Console.WriteLine($"{think_time}, {root.visitCount}, {root.visitCount * 1000 / stopwatch.ElapsedMilliseconds} nps");

				// Choose the most visited node
				float best = Single.MinValue;
				foreach (Node child in root.children)
				{
					if (child.visitCount >= best)
					{
						best = child.visitCount;
						bestAction = child.action;
					}
				}
			}

			stopwatchForThisTurn.Stop();
			++movesInThisTurn;
			if (bestAction.PlayerTaskType == PlayerTaskType.END_TURN)
			{
				Console.WriteLine(movesInThisTurn);
				stopwatchForThisTurn.Reset();
				movesInThisTurn = 0;
			}

			return bestAction;
		}

		private void Expand(Node node, POGame poGame)
		{
			node.children = new List<Node>();
			foreach (PlayerTask task in poGame.CurrentPlayer.Options())
			{
				node.children.Add(new Node(node, task));
			}
		}

		private Node Select(Node node)
		{
			float maxUcb = Single.MinValue;
			Node selected = null;
			foreach (Node child in node.children)
			{
				float q;
				float u;
				if (child.visitCount == 0)
				{
					if (node.totalValue > 0)
						q = node.totalValue / node.visitCount;
					else
						q = 1.0f;
					u = FPU;
				}
				else
				{
					q = child.totalValue / child.visitCount;
					u = (float)Math.Sqrt(2.0 * Math.Log(node.visitCount) / child.visitCount);
				}

				float ucb = q + C_UCT * u;

				if (ucb > maxUcb)
				{
					maxUcb = ucb;
					selected = child;
				}
			}

			return selected;
		}

		private float Simulate(Node nodeToSimulate, POGame poGame)
		{
			float result = -1;
			int simulationSteps = 0;
			PlayerTask task = null;

			if (poGame == null)
				return 0.5f;

			List<PlayerTask> taskToSimulate = new List<PlayerTask>(1);
			taskToSimulate.Add(null);

			while (poGame.getGame().State != SabberStoneCore.Enums.State.COMPLETE)
			{
				task = Greedy(poGame);
				taskToSimulate[0] = task;
				if (task.PlayerTaskType != PlayerTaskType.END_TURN)
					poGame = poGame.Simulate(taskToSimulate)[taskToSimulate[0]];

				if (poGame == null)
					return 0.5f;

				if (task.PlayerTaskType == PlayerTaskType.END_TURN)
					return ScoreToValue(Score(poGame));

				simulationSteps++;
			}


			if (poGame.CurrentPlayer.PlayState == SabberStoneCore.Enums.PlayState.CONCEDED
				|| poGame.CurrentPlayer.PlayState == SabberStoneCore.Enums.PlayState.LOST)
			{
				result = 0;
			}
			else if (poGame.CurrentPlayer.PlayState == SabberStoneCore.Enums.PlayState.WON)
			{
				result = 1;
			}

			return result;
		}

		PlayerTask Greedy(POGame poGame)
		{
			PlayerTask bestTask = null;
			int bestScore = Int32.MinValue;

			List<PlayerTask> options = poGame.CurrentPlayer.Options();
			var simulated = poGame.Simulate(options);

			foreach (var stateAfterSimulate in simulated)
			{
				int score = Score(stateAfterSimulate.Value);
				if (score >= bestScore)
				{
					bestScore = score;
					bestTask = stateAfterSimulate.Key;
				}
			}
			return bestTask;
		}

		private static int Score(POGame state)
		{
			return new CustomScore { Controller = state.CurrentPlayer }.Rate();
		}

		private float ScoreToValue(int score)
		{
			return 1.0f / (1.0f + (float)Math.Exp(-score / 600.0));
		}

		private void Backup(Node node, float result)
		{
			node.visitCount++;
			node.totalValue += result;
			if (node.parent != null)
			{
				Backup(node.parent, result);
			}
		}
	}
}
