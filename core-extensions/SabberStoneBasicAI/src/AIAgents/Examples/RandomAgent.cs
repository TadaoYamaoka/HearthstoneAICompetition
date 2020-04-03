using System;
using System.Collections.Generic;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.PartialObservation;


namespace SabberStoneBasicAI.AIAgents
{
	class RandomAgent : AbstractAgent
	{
		private Random Rnd = new Random();

		public override void InitializeAgent()
		{
			Rnd = new Random();
		}

		public override void FinalizeAgent()
		{
			//Nothing to do here
		}

		public override void FinalizeGame()
		{
			//Nothing to do here
		}

		public override PlayerTask GetMove(POGame poGame)
		{
			List<PlayerTask> options = poGame.CurrentPlayer.Options();
			return options[Rnd.Next(options.Count)];
		}

		public override void InitializeGame()
		{
			//Nothing to do here
		}
	}
}
