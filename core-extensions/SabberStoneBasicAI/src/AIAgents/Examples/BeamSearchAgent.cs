using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneBasicAI.Score;
using SabberStoneCore.Enums;
using SabberStoneCore.Tasks.PlayerTasks;


namespace SabberStoneBasicAI.AIAgents
{
	class BeamSearchAgent : AbstractAgent
	{
		private Stopwatch _watch;


		public override PlayerTask GetMove(POGame game)
		{
			int depth;
			int beamWidth;

			// Check how much time we have left on this turn. The hard limit is 75 seconds so we already stop
			// beam searching when 60 seconds have passed, just to be sure.
			if (_watch.ElapsedMilliseconds < 60 * 1000)
			{ // We still have ample time, proceed with beam search
				depth = 15;
				beamWidth = 12;
			}
			else
			{ // Time is running out, just simulate one timestep now
				depth = 1;
				beamWidth = 1;
				Console.WriteLine("Over 60s in turn already. Pausing beam search for this turn!");
			}

			_watch.Start();
			var move = BeamSearch(game, depth, playerbeamWidth: beamWidth, opponentBeamWidth: 1);
			_watch.Stop();

			if (move.PlayerTaskType == PlayerTaskType.END_TURN)
			{
				_watch.Reset();
			}

			return move;
		}

		private PlayerTask BeamSearch(POGame game, int depth, int playerbeamWidth, int opponentBeamWidth)
		{
			var me = game.CurrentPlayer;


			var bestSimulations = Simulate(game, playerbeamWidth);
			LabelSimulations(bestSimulations, 0);


			for (var i = 1; i < depth; i++)
			{
				var newBestSimulations = new List<Simulation>();
				foreach (var sim in bestSimulations)
				{
					var beamWidth = sim.Game.CurrentPlayer.PlayerId == me.PlayerId
						? playerbeamWidth
						: opponentBeamWidth;
					var childSims = Simulate(sim.Game, beamWidth);
					LabelSimulations(childSims, i);
					childSims.ForEach(x => x.Parent = sim);
					newBestSimulations.AddRange(childSims);
				}

				bestSimulations = newBestSimulations
					.OrderBy(x => Score(x.Game, me.PlayerId))
					.TakeLast(playerbeamWidth)
					.Reverse()
					.ToList();
			}

			var nextMove = bestSimulations.Any()
				? bestSimulations.First().GetFirstTask()
				: me.Options().First(x => x.PlayerTaskType == PlayerTaskType.END_TURN);

			return nextMove;
		}


		private List<Simulation> Simulate(POGame game, int numSolutions)
		{
			var simulations = game
				.Simulate(game.CurrentPlayer.Options()).Where(x => x.Value != null)
				.Select(x => new Simulation
				{ Task = x.Key, Game = x.Value, Score = Score(x.Value, game.CurrentPlayer.PlayerId) })
				.OrderBy(x => x.Score)
				.TakeLast(numSolutions)
				.Reverse() // Best task first
				.ToList();

			return simulations;
		}


		// Calculate different scores based on our hero's class
		private static int Score(POGame state, int playerId)
		{
			var p = state.CurrentPlayer.PlayerId == playerId ? state.CurrentPlayer : state.CurrentOpponent;
			switch (state.CurrentPlayer.HeroClass)
			{
				case CardClass.WARRIOR: return new AggroScore { Controller = p }.Rate();
				case CardClass.MAGE: return new ControlScore { Controller = p }.Rate();
				default: return new MidRangeScore { Controller = p }.Rate();
			}
		}

		private void LabelSimulations(List<Simulation> simulations, int currentDepth)
		{
			for (var i = 0; i < simulations.Count; i++)
			{
				simulations[i].Label = currentDepth + "-" + i;
			}
		}


		public override void InitializeAgent()
		{
		}

		public override void InitializeGame()
		{
			_watch = new Stopwatch();
		}

		public override void FinalizeGame()
		{
		}

		public override void FinalizeAgent()
		{
		}
	}

	class Simulation
	{
		public PlayerTask Task { get; set; }
		public POGame Game { get; set; }
		public int Score { get; set; }
		public Simulation Parent { get; set; }
		public string Label { get; set; } = "<missing>";

		public PlayerTask GetFirstTask()
		{
			return Parent == null ? Task : Parent.GetFirstTask();
		}

		public string GetQualifiedLabel()
		{
			return Parent == null ? Label : Parent.GetQualifiedLabel() + " ==> " + Label;
		}

		public override string ToString()
		{
			return $"{nameof(Task)}: {Task}, {nameof(Score)}: {Score}";
		}
	}
}
