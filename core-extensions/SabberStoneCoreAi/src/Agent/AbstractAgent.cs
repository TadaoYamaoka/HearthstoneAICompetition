using System;
using System.Collections.Generic;
using System.Text;

using SabberStoneCore.Tasks;
using SabberStoneCoreAi.POGame;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneCore.Model;
using SabberStoneCore.Enums;

namespace SabberStoneCoreAi.Agent
{
	abstract class AbstractAgent

	{
		public static List<Card> preferedDeck;
		public static CardClass preferedHero;
 
		public abstract void InitializeAgent();

		public abstract void InitializeGame();

		public abstract PlayerTask GetMove(POGame.POGame poGame);

		public abstract void FinalizeGame();

		public abstract void FinalizeAgent();

	}
}
