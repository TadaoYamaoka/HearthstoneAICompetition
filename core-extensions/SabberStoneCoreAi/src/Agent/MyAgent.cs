using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCore.Tasks;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.POGame;
using SabberStoneCore.Tasks.PlayerTasks;


namespace SabberStoneCoreAi.Agent
{
	class MyAgent : AbstractAgent
	{
		private Random Rnd = new Random();

		public override void FinalizeAgent()
		{
		}

		public override void FinalizeGame()
		{
		}

		public override PlayerTask GetMove(SabberStoneCoreAi.POGame.POGame poGame)
		{
			List<PlayerTask> simulatedactions = new List<PlayerTask>();
			simulatedactions.AddRange(poGame.CurrentPlayer.Options());
			Dictionary<PlayerTask, SabberStoneCoreAi.POGame.POGame> sim = poGame.Simulate(simulatedactions);

			Dictionary<PlayerTask, SabberStoneCoreAi.POGame.POGame>.KeyCollection keyColl = sim.Keys;

			foreach (PlayerTask key in keyColl)
			{
				//do something with simulated actions
				//in case an EndTurn was simulated you need to set your own cards
				//see POGame.prepareOpponent() for an example
			}

			return poGame.CurrentPlayer.Options()[0];
		}

		public override void InitializeAgent()
		{
			Rnd = new Random();
		}

		public override void InitializeGame()
		{
		}
	}
}
