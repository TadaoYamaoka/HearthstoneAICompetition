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
	using Trajectory = List<(Node, int)>;

	class Edge
	{
		public Edge(long actionHashCode)
		{
			visitCount = 0;
			totalValue = 0;
			this.actionHashCode = actionHashCode;
		}

		public int visitCount;
		public float totalValue;
		public long actionHashCode;
	}

	class Node
	{
		public int visitCount = 0;
		public float totalValue = 0;
		public List<Edge> edges = null;
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
		private float FPU = 5.0f;

		Dictionary<long, Node> nodeHashMap = new Dictionary<long, Node>();

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

#if DEBUG
			Console.WriteLine($"root:{GetGameHashCode(poGame)}");
#endif
			PlayerTask bestAction = null;
			if (poGame.CurrentPlayer.Options().Count == 1)
			{
				bestAction = poGame.CurrentPlayer.Options()[0];
			}
			else
			{
				stopwatchForThisTurn.Start();

				long bestActionCode = 0;
				Trajectory trajectory = new Trajectory();

				List<PlayerTask> taskToSimulate = new List<PlayerTask>(1);
				taskToSimulate.Add(null);
				POGame poGameRoot = poGame;

				Node root;
				long gameHashCodeRoot = GetGameHashCode(poGameRoot);
				if (!nodeHashMap.TryGetValue(gameHashCodeRoot, out root))
				{
					root = new Node();
					nodeHashMap.Add(gameHashCodeRoot, root);
				}

				Expand(root, poGameRoot);
				/*foreach (var child in root.edges)
				{
					Console.WriteLine(child.actionHashCode);
				}*/

				long think_time = (30 * 1000 - stopwatchForThisTurn.ElapsedMilliseconds) / Math.Max(3, 5 - movesInThisTurn);
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				//for (int itr = 0; itr < 100; ++itr)
				while (stopwatch.ElapsedMilliseconds <= think_time)
				{
					Node node = root;
					poGame = poGameRoot.getCopy();
					long gameHashCode = gameHashCodeRoot;
					long actionHashCodeNext = 0;
					bool simulateResult = true;
					int index = 0;
					trajectory.Clear();

					// traverse
					do
					{
						index = Select(node);
						if (index >= node.edges.Count)
						{
							Console.WriteLine($"{index}, {node.edges.Count}, {node == root}");
							Debugger.Break();
						}
						actionHashCodeNext = node.edges[index].actionHashCode;

						// Until the end of my own turn
						if (actionHashCodeNext == 0)
						{
							trajectory.Add((node, index));
							break;
						}

						taskToSimulate[0] = null;
						foreach (PlayerTask task in poGame.CurrentPlayer.Options())
						{
							if (GetActionHashCode(task) == actionHashCodeNext)
							{
								taskToSimulate[0] = task;
								break;
							}
						}
						if (taskToSimulate[0] == null)
						{
							// Hash key conflict
							return poGame.CurrentPlayer.Options().First();
							/*foreach (PlayerTask task in poGame.CurrentPlayer.Options())
							{
								Console.WriteLine($"{task}, {GetActionHashCode(task)}");
							}
							Console.WriteLine("---");
							foreach (var edge in node.edges)
							{
								Console.WriteLine($"{edge.actionHashCode}");
							}
							Console.WriteLine(gameHashCode);
							Debugger.Break();*/
						}

						poGame = poGame.Simulate(taskToSimulate)[taskToSimulate[0]];
						long gameHashCodeNext = GetGameHashCode(poGame);
						if (gameHashCode == gameHashCodeNext)
						{
							// loop
							node.edges.RemoveAt(index);
							continue;
						}
						gameHashCode = gameHashCodeNext;

						trajectory.Add((node, index));

						if (!nodeHashMap.TryGetValue(gameHashCode, out node))
						{
							node = new Node();
							nodeHashMap.Add(gameHashCode, node);
						}
					} while (node.edges != null);

					if (simulateResult == false)
						continue;

					if (actionHashCodeNext != 0)
					{
#if DEBUG
						Console.WriteLine($"expand:{gameHashCode}");
#endif
						Expand(node, poGame);

						float value = Simulate(node, poGame);
						Backup(trajectory, value);
					}
					else
					{
						float value;
						if (node.edges[index].visitCount == 0)
							value = ScoreToValue(Score(poGame));
						else
							value = node.edges[index].totalValue / node.edges[index].visitCount;
						Backup(trajectory, value);
					}
				}
				stopwatch.Stop();
				//Console.WriteLine($"{think_time}, {root.visitCount}, {root.visitCount * 1000 / stopwatch.ElapsedMilliseconds} nps");

				// Choose the most visited node
				float best = Single.MinValue;
				foreach (Edge child in root.edges)
				{
					if (child.visitCount >= best)
					{
						best = child.visitCount;
						bestActionCode = child.actionHashCode;
					}
				}

				// Choose an action with a matching hash code
				foreach (PlayerTask task in poGameRoot.CurrentPlayer.Options())
				{
					if (GetActionHashCode(task) == bestActionCode)
					{
						bestAction = task;
						break;
					}
				}
			}

			stopwatchForThisTurn.Stop();
			++movesInThisTurn;
			if (bestAction.PlayerTaskType == PlayerTaskType.END_TURN)
			{
				//Console.WriteLine(movesInThisTurn);
				stopwatchForThisTurn.Reset();
				movesInThisTurn = 0;
				nodeHashMap.Clear();
			}

			return bestAction;
		}

		private void Expand(Node node, POGame poGame)
		{
#if DEBUG
			Console.WriteLine($"basemana:{poGame.CurrentPlayer.BaseMana}, usedmana:{poGame.CurrentPlayer.UsedMana}, tempmana:{poGame.CurrentPlayer.TemporaryMana}");
			foreach (var minion in poGame.CurrentPlayer.BoardZone)
			{
				Console.WriteLine($"minion:{minion}, cost:{minion.Cost}");
			}
			foreach (var card in poGame.CurrentPlayer.HandZone)
			{
				Console.WriteLine($"hand:{card}, cost:{card.Cost}");
			}
			if (poGame.CurrentPlayer.Hero.Weapon != null)
			{
				Console.WriteLine($"weapon:{poGame.CurrentPlayer.Hero.Weapon.AttackDamage}");
			}
#endif
			var options = poGame.CurrentPlayer.Options();
			node.edges = new List<Edge>();
			foreach (PlayerTask task in options)
			{
				var code = GetActionHashCode(task);
#if DEBUG
				Console.WriteLine($"{task}, {code}");
#endif
				node.edges.Add(new Edge(code));
			}
		}

		private int Select(Node node)
		{
			float maxUcb = Single.MinValue;
			int selected = 0;
			int index = 0;
			foreach (Edge child in node.edges)
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
					selected = index;
				}

				++index;
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

		private void Backup(Trajectory trajectory, float result)
		{
			foreach ((Node node, int index) in trajectory)
			{
				node.edges[index].visitCount++;
				node.edges[index].totalValue += result;
				node.visitCount++;
				node.totalValue += result;

			}
		}

		private static long GetGameHashCode(POGame poGame)
		{
			// mana
			long hash1 = ((5381 << 16) + 5381) ^ poGame.CurrentPlayer.BaseMana * 1566083941L;
			long hash2 = ((5381 << 16) + 5381) ^ poGame.CurrentPlayer.UsedMana * 1566083941L;
			hash1 = unchecked((hash2 << 5) + hash1) ^ poGame.CurrentPlayer.TemporaryMana * 1566083941L;
			// choice
			if (poGame.CurrentPlayer.Choice != null)
			{
				long choiceCode = 0;
				foreach (var choice in poGame.CurrentPlayer.Choice.Choices)
				{
					choiceCode += choice * 1566083941L;
				}
				hash2 = unchecked((hash2 << 5) + hash2) + choiceCode;
			}
			//Console.WriteLine($"1:hash1:{hash1}, hash2:{hash2}");

			// hands
			long handZoneCode = 0;
			foreach (var hand in poGame.CurrentPlayer.HandZone)
			{
				handZoneCode += GetHashCode(hand.Card.Id);
			}
			hash2 = unchecked((hash2 << 5) + hash2) ^ handZoneCode;

			// board zone
			foreach (Minion entry in poGame.CurrentPlayer.BoardZone)
			{
				UpdateMinionHashCode(ref hash1, ref hash2, entry);
			}

			// hero
			Hero hero = poGame.CurrentPlayer.Hero;
			hash1 = unchecked((hash2 << 5) + hash1) ^ hero.Health * 1566083941L;
			hash2 = unchecked((hash1 << 5) + hash2) ^ hero.Damage * 1566083941L;
			hash1 = unchecked((hash2 << 5) + hash1) ^ hero.Armor * 1566083941L;
			if (hero.Weapon != null)
			{
				hash2 = unchecked((hash1 << 5) + hash2) ^ hero.Weapon.Damage * 1566083941L;
				hash1 = unchecked((hash2 << 5) + hash2) ^ hero.Weapon.AttackDamage * 1566083941L;
				UpdateHashCode(ref hash1, ref hash2, hero.Weapon.Card.Id);
			}
			hash2 = unchecked((hash1 << 5) + hash2) ^ hero.HeroPower.Cost * 1566083941L;
			hash1 = unchecked((hash2 << 5) + hash1) ^ Convert.ToInt64(hero.HeroPower.IsExhausted) * 1566083941L;
			hash2 = unchecked((hash1 << 5) + hash2) ^ Convert.ToInt64(hero.CanAttack) * 1566083941L;
			UpdateHashCode(ref hash1, ref hash2, hero.HeroPower.Card.Id);

			//Console.WriteLine($"2:hash1:{hash1}, hash2:{hash2}");

			// ================================
			// opponent

			// board zone
			foreach (Minion entry in poGame.CurrentOpponent.BoardZone)
			{
				UpdateMinionHashCode(ref hash1, ref hash2, entry);
			}

			// hero
			Hero opponentHero = poGame.CurrentOpponent.Hero;
			hash1 = unchecked((hash2 << 5) + hash1) ^ opponentHero.Health * 1566083941L;
			hash2 = unchecked((hash1 << 5) + hash2) ^ opponentHero.Damage * 1566083941L;
			hash1 = unchecked((hash2 << 5) + hash1) ^ opponentHero.Armor * 1566083941L;

			return hash1 + hash2;
		}

		private static long GetActionHashCode(PlayerTask action)
		{
			// CHOOSE, CONCEDE, END_TURN, HERO_ATTACK, HERO_POWER, MINION_ATTACK, PLAY_CARD
			switch (action.PlayerTaskType)
			{
				case PlayerTaskType.END_TURN:
					return 0;
				case PlayerTaskType.HERO_ATTACK:
					{
						long hash1 = ((5381 << 16) + 5381) + 1 * 1566083941L;
						long hash2 = ((5381 << 16) + 5381) ^ GetHashCode(action.Controller.Hero.Card.Id);
						if (action.Target is Minion target)
						{
							UpdateMinionHashCode(ref hash1, ref hash2, target);
						}
						else
						{
							UpdateHashCode(ref hash1, ref hash2, action.Target.Card.Id);
						}
						return hash1 + hash2;
					}
				case PlayerTaskType.HERO_POWER:
					{
						long hash1 = ((5381 << 16) + 5381) + 2 * 1566083941L;
						long hash2 = ((5381 << 16) + 5381) ^ GetHashCode(action.Controller.Hero.HeroPower.Card.Id);
						if (action.HasTarget)
						{
							if (action.Target is Minion target)
							{
								UpdateMinionHashCode(ref hash1, ref hash2, target);
							}
							else
							{
								UpdateHashCode(ref hash1, ref hash2, action.Target.Card.Id);
							}
						}
						return hash1 + hash2;
					}
				case PlayerTaskType.MINION_ATTACK:
					{
						long hash1 = ((5381 << 16) + 5381) + 3 * 1566083941L;
						long hash2;
						if (action.Target is Minion target)
						{
							hash2 = GetMinionHashCode(target);
						}
						else
						{
							hash2 = GetHashCode(action.Target.Card.Id);
						}
						UpdateMinionHashCode(ref hash1, ref hash2, action.Source as Minion);
						return hash1 + hash2;
					}
				case PlayerTaskType.PLAY_CARD:
					{
						var playCardTask = action as PlayCardTask;
						long hash1 = ((5381 << 16) + 5381) + 4 * 1566083941L;
						long hash2 = ((5381 << 16) + 5381) + playCardTask.ZonePosition * 1566083941L;
						if (action.HasTarget)
						{
							if (action.Target is Minion target)
							{
								UpdateMinionHashCode(ref hash1, ref hash2, target);
							}
							else
							{
								UpdateHashCode(ref hash1, ref hash2, action.Target.Card.Id);
							}
						}
						UpdateHashCode(ref hash1, ref hash2, action.Source.Card.Id);
						return hash1 + hash2;
					}
				case PlayerTaskType.CHOOSE:
					{
						var chooseTask = action as ChooseTask;
						long hash1 = ((5381 << 16) + 5381) + 5 * 1566083941L;
						long hash2 = ((5381 << 16) + 5381) + chooseTask.Choices[0] * 1566083941L;
						return hash1 + hash2;
					}
				default:
					return 0;
			}
		}

		private static long GetCharacterHashCode(ICharacter entry)
		{
			long hash1 = ((5381 << 16) + 5381) + entry.Controller.PlayerId * 1566083941L;
			long hash2 = ((5381 << 16) + 5381);
			UpdateHashCode(ref hash1, ref hash2, entry.Card.Id);
			return hash1 + hash2;
		}

		private static long GetMinionHashCode(Minion entry)
		{
			long hash1 = ((5381 << 16) + 5381);
			long hash2 = ((5381 << 16) + 5381);
			UpdateMinionHashCode(ref hash1, ref hash2, entry);

			return hash1 + hash2;
		}

		private static void UpdateMinionHashCode(ref long hash1, ref long hash2, Minion entry)
		{
			hash1 = ((hash2 << 5) + hash1) + entry.Controller.PlayerId * 1566083941L;
			hash2 = ((hash1 << 5) + hash2) + entry.Health * 1566083941L;
			hash1 = ((hash2 << 5) + hash1) + entry.AttackDamage * 1566083941L;
			hash2 = ((hash1 << 5) + hash2) + entry.NumAttacksThisTurn * 1566083941L;
			hash1 = ((hash2 << 5) + hash1) + entry.Cost * 1566083941L;
			hash2 = ((hash1 << 5) + hash2) + Convert.ToInt64(entry.HasCharge) * 1566083941L;
			hash1 = ((hash2 << 5) + hash1) + Convert.ToInt64(entry.IsExhausted) * 1566083941L;
			hash2 = ((hash1 << 5) + hash2) + Convert.ToInt64(entry.IsFrozen) * 1566083941L;
			if (entry.AuraEffects != null)
			{
				var auraEffects = entry.AuraEffects;
				long auraEffectsCode = Convert.ToInt64(auraEffects.CardCostHealth);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.Echo);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.CantBeTargetedBySpells);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.ATK);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.Health);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.Charge);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.Taunt);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.Lifesteal);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.Rush);
				auraEffectsCode = auraEffectsCode << 1 | Convert.ToInt64(auraEffects.CantAttack);
				hash2 = ((hash1 << 5) + hash1) + auraEffectsCode * 1566083941L;
			}
			if (entry.AppliedEnchantments != null)
			{
				long enchantsCode = 0;
				foreach (var enchant in entry.AppliedEnchantments)
				{
					enchantsCode += GetHashCode(enchant.Card.Id);
				}
				hash1 = ((hash2 << 5) + hash1) ^ enchantsCode;
			}
			UpdateHashCode(ref hash1, ref hash2, entry.Card.Id);
		}

		private static long GetHashCode(string s)
		{
			long hash1 = ((5381 << 16) + 5381) ^ s[0] * 1566083941L;
			long hash2 = ((5381 << 16) + 5381) ^ s[1] * 1566083941L;

			for (int i = s.Length - 1; i >= 0; --i)
			{
				long c = s[i] * 1566083941L;
				hash1 = unchecked((hash2 << 5) + hash1) ^ c;

				if (--i < 0)
					break;

				c = s[i] * 1566083941L;
				hash2 = unchecked((hash1 << 5) + hash2) ^ c;
			}

			return hash1 + hash2;
		}
		private static void UpdateHashCode(ref long hash1, ref long hash2, string s)
		{
			hash1 = unchecked((hash2 << 5) + hash1) * 1566083941L;
			hash2 = unchecked((hash1 << 5) + hash2) ^ s[1] * 1566083941L;

			for (int i = s.Length - 1; i >= 0; --i)
			{
				long c = s[i] * 1566083941L;
				hash1 = unchecked((hash2 << 5) + hash1) ^ c;

				if (--i < 0)
					break;

				c = s[i] * 1566083941L;
				hash2 = unchecked((hash1 << 5) + hash2) ^ c;
			}
		}
	}
}
