using System.Collections.Generic;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneCore.Model;
using SabberStoneCore.Enums;

namespace SabberStoneBasicAI.AIAgents
{
	abstract class AbstractAgent

	{
		public List<Card> preferedDeck;
		public CardClass preferedHero;


		public abstract void InitializeAgent();

		public abstract void InitializeGame();

		public abstract PlayerTask GetMove(POGame poGame);

		public abstract void FinalizeGame();

		public abstract void FinalizeAgent();

	}
}
