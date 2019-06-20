using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCoreAi.POGame;
using SabberStoneCoreAi.Agent.ExampleAgents;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.Meta;
using SabberStoneCoreAi.Competition.Agents;


namespace SabberStoneCoreAi.Competition
{
	public class PublicDecks
    {
		public static GameConfig gameConfig1 = new GameConfig()
		{
			StartPlayer = 1,
			Player1HeroClass = CardClass.WARRIOR,
			Player2HeroClass = CardClass.WARRIOR,
			Player1Deck = Decks.AggroPirateWarrior,
			Player2Deck = Decks.AggroPirateWarrior,
			FillDecks = false,
			Shuffle = true,
			Logging = false
		};


		public static GameConfig gameConfig2 = new GameConfig()
		{
			StartPlayer = 1,
			Player1HeroClass = CardClass.MAGE,
			Player2HeroClass = CardClass.MAGE,
			Player1Deck = Decks.RenoKazakusMage,
			Player2Deck = Decks.RenoKazakusMage,
			FillDecks = false,
			Shuffle = true,
			Logging = false
		};

		public static GameConfig gameConfig3 = new GameConfig()
		{
			StartPlayer = 1,
			Player1HeroClass = CardClass.SHAMAN,
			Player2HeroClass = CardClass.SHAMAN,
			Player1Deck = Decks.MidrangeJadeShaman,
			Player2Deck = Decks.MidrangeJadeShaman,
			FillDecks = false,
			Shuffle = true,
			Logging = false
		};

		public static LinkedList<GameConfig> getConfigListPremadeDeckPlaying()
		{
			var configList = new LinkedList<GameConfig>();
			configList.AddLast(gameConfig1);
			configList.AddLast(gameConfig2);
			configList.AddLast(gameConfig3);
			return configList;
		}
	}
}
