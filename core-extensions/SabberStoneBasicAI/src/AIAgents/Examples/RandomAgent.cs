using System;
using System.Collections.Generic;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneCore.Enums;
using System.Linq;
using SabberStoneCore.Model.Entities;

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
			var player = poGame.CurrentPlayer;

			// During Mulligan: select Random cards
			if (player.MulliganState == Mulligan.INPUT)
			{
				List<int> mulligan = RandomMulliganRule().Invoke(player.Choice.Choices.Select(p => poGame.getGame().IdEntityDic[p]).ToList());
				return ChooseTask.Mulligan(player, mulligan);
			}

			// During Gameplay: select a random action
			List<PlayerTask> options = poGame.CurrentPlayer.Options();
			return options[Rnd.Next(options.Count)];
		}

		public override void InitializeGame()
		{
			//Nothing to do here
		}

		public Func<List<IPlayable>, List<int>> RandomMulliganRule()
		{
			return p => p.Where(t => Rnd.Next(1, 3) > 1).Select(t => t.Id).ToList();
		}
	}
}
