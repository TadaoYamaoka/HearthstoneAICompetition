/*
 * Copyright (c) 2019, Alvaro Florencio de Marcos Ales. All rights reserved.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * 
 * Contributors:
 * Antonio Jose Fernandez Leiva and Pablo Garcia Sanchez.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SabberStoneBasicAI.AIAgents;
using SabberStoneCoreAi.src.Agent.AlvaroMCTS;
using System.Diagnostics;
using SabberStoneCore.Enums;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.PartialObservation;

namespace SabberStoneCoreAi.Agent
{
	class AlvaroAgent : AbstractAgent
	{
		private Random Rnd = new Random();
		private ParametricGreedyAgent greedyAgent;

		//		======== PARAMETERS ==========
		private double EXPLORE_CONSTANT = 2;
		private int MAX_TIME = 1000;
		private string SELECTION_ACTION_METHOD = "MaxVictories";
		private string TREE_POLICY = "UCB1";
		private double SCORE_IMPORTANCE = 1;
		private int TREE_MAXIMUM_DEPTH = 10;
		private string SIMULATION_POLICY = "RandomPolicy";
		private double CHILDREN_CONSIDERED_SIMULATING = 1;
		private string ESTIMATION_MODE = "BaseEstimation";
		private int NUM_SIMULATIONS = 1;

		public override void FinalizeAgent() { }

		public override void FinalizeGame() { }

		public override void InitializeAgent() { }

		public override void InitializeGame() { }

		public AlvaroAgent()
		{
			EXPLORE_CONSTANT = 2;
			MAX_TIME = 1000;
			SELECTION_ACTION_METHOD = "MaxVictoriesOverVisited";
			SCORE_IMPORTANCE = 10;
			TREE_POLICY = "UCB1";
			TREE_MAXIMUM_DEPTH = 1;
			SIMULATION_POLICY = "GreedyPolicy";
			CHILDREN_CONSIDERED_SIMULATING = 1.0;
			ESTIMATION_MODE = "LinearEstimation";
			NUM_SIMULATIONS = 1;

			greedyAgent = new ParametricGreedyAgent("0.569460712743" + "#" + "0.958111820041" + "#" + "0.0689492467097" + "#" + "0.0" + "#" +
				"0.843573987219" + "#" + "0.700225423688" + "#" + "0.907680353441" + "#" + "0.0" + "#" + "0.993682660717" + "#" +
				"1.0" + "#" + "0.640753949511" + "#" + "0.992872512338" + "#" + "0.92870036875" + "#" + "0.168100484322" + "#" +
				"0.870080107454" + "#" + "0.0" + "#" + "0.42897762808" + "#" + "1.0" + "#" + "0.0" + "#" + "0.583884736646" + "#" + "0.0");

			Estimator.setWeights(0.7f, 0.4f, 0.4f, 0.9f, 0.4f, 0.01f, 0.02f, 0.4f, 0.3f, 0.8f, 0.5f, 0.4f, 0.5f, float.Parse("0.640753949511"), float.Parse("0.992872512338"), float.Parse("0.92870036875"),
				float.Parse("0.168100484322"), float.Parse("0.870080107454"), float.Parse("0.42897762808"), float.Parse("1.0"));

		}

		public AlvaroAgent(double exploreConstant, int maxTime, string selectionAction, double scoreImportance, string treePolicy, int treeMaximumDepth,
						   string simulationPolicy, double childrenConsideredSimulating, string estimationMode, int numSimulations,
						
						   string HERO_HEALTH_REDUCED, string HERO_ATTACK_REDUCED, string MINION_HEALTH_REDUCED, string MINION_ATTACK_REDUCED,
							string MINION_APPEARED, string MINION_KILLED, string SECRET_REMOVED, string MANA_REDUCED, string M_HEALTH,
							 string M_ATTACK, string M_HAS_CHARGE, string M_HAS_DEAHTRATTLE, string M_HAS_DIVINE_SHIELD, string M_HAS_INSPIRE,
							  string M_HAS_LIFE_STEAL, string M_HAS_STEALTH, string M_HAS_TAUNT, string M_HAS_WINDFURY, string M_RARITY, string M_MANA_COST,
							  string M_POISONOUS,

							  float weaponAttack, float weaponDurability, float health, float boardStats, float handSize, float deckRemaining,
							  float mana, float secret, float overload, float minionCost, float secretCost, float cardCost, float weaponCost)
		{
			EXPLORE_CONSTANT = exploreConstant;
			MAX_TIME = maxTime;
			SELECTION_ACTION_METHOD = selectionAction;
			SCORE_IMPORTANCE = scoreImportance;
			TREE_POLICY = treePolicy;
			TREE_MAXIMUM_DEPTH = treeMaximumDepth;
			SIMULATION_POLICY = simulationPolicy;
			CHILDREN_CONSIDERED_SIMULATING = childrenConsideredSimulating;
			ESTIMATION_MODE = estimationMode;
			NUM_SIMULATIONS = numSimulations;

			greedyAgent = new ParametricGreedyAgent(HERO_HEALTH_REDUCED + "#" + HERO_ATTACK_REDUCED + "#" + MINION_HEALTH_REDUCED + "#" + MINION_ATTACK_REDUCED + "#" +
				MINION_APPEARED + "#" + MINION_KILLED + "#" + SECRET_REMOVED + "#" + MANA_REDUCED + "#" + M_HEALTH + "#" +
				M_ATTACK + "#" + M_HAS_CHARGE + "#" + M_HAS_DEAHTRATTLE + "#" + M_HAS_DIVINE_SHIELD + "#" + M_HAS_INSPIRE + "#" +
				M_HAS_LIFE_STEAL + "#" + M_HAS_STEALTH + "#" + M_HAS_TAUNT + "#" + M_HAS_WINDFURY + "#" + M_RARITY + "#" + M_MANA_COST + "#" + M_POISONOUS);

			Estimator.setWeights(weaponAttack, weaponDurability, health, boardStats, handSize, deckRemaining, mana, secret, overload, minionCost, secretCost,
				cardCost, weaponCost, float.Parse(M_HAS_CHARGE), float.Parse(M_HAS_DEAHTRATTLE), float.Parse(M_HAS_DIVINE_SHIELD),
				float.Parse(M_HAS_INSPIRE), float.Parse(M_HAS_LIFE_STEAL), float.Parse(M_HAS_TAUNT), float.Parse(M_HAS_WINDFURY));
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

			if (poGame.CurrentPlayer.Options().Count == 1)
			{
				return poGame.CurrentPlayer.Options()[0];
			}

			POGame initialState = poGame.getCopy();

			Node root = new Node();

			Node selectedNode;
			Node nodeToSimulate;
			float scoreOfSimulation;
			int iterations = 0;

			InitializeRoot(root, initialState);

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			while (stopwatch.ElapsedMilliseconds <= MAX_TIME)
			{
				poGame = initialState;
				selectedNode = Selection(root, iterations, ref poGame);
				nodeToSimulate = Expansion(selectedNode, ref poGame);

				for(int i = 0; i < NUM_SIMULATIONS; i++)
				{
					scoreOfSimulation = Simulation(nodeToSimulate, poGame);
					Backpropagation(nodeToSimulate, scoreOfSimulation);
					iterations++;
				}
			}
			stopwatch.Stop();
		
			return SelectAction.selectTask(SELECTION_ACTION_METHOD, root, iterations, EXPLORE_CONSTANT);
		}

		private void InitializeRoot(Node root, POGame poGame)
		{
			foreach (PlayerTask task in poGame.CurrentPlayer.Options())
			{
				root.children.Add(new Node(task,root,root.depth+1));
			}
		}

		private Node Selection(Node root, int iterations, ref POGame poGame)
		{
			Node bestNode = new Node();
			double bestScore = double.MinValue;
			double childScore = 0;

			POGame pOGameIfSimulationFail = poGame.getCopy();

			foreach (Node node in root.children)
			{
				childScore = TreePolicies.selectTreePolicy(TREE_POLICY, node, iterations, EXPLORE_CONSTANT, ref poGame, SCORE_IMPORTANCE, greedyAgent);
				if (childScore > bestScore)
				{
					bestScore = childScore;
					bestNode = node;
				}
			}
			List<PlayerTask> taskToSimulate = new List<PlayerTask>();
			taskToSimulate.Add(bestNode.task);
	
			if(bestNode.task.PlayerTaskType != PlayerTaskType.END_TURN)
				poGame = poGame.Simulate(taskToSimulate)[bestNode.task];

			if (poGame == null) 
			{
				root.children.Remove(bestNode);
				if(root.children.Count == 0)
					root = root.parent;
				
				poGame = pOGameIfSimulationFail;
				return Selection(root,iterations, ref poGame);
			}
			
			if(bestNode.children.Count != 0)
			{
				bestNode = Selection(bestNode,iterations, ref poGame);
			}

			return bestNode;
		}

		private Node Expansion(Node leaf, ref POGame poGame)
		{
			Node nodeToSimulate;
			POGame pOGameIfSimulationFail = poGame.getCopy();
			if (leaf.timesVisited == 0 || leaf.depth >= TREE_MAXIMUM_DEPTH || leaf.task.PlayerTaskType == PlayerTaskType.END_TURN)
			{
				nodeToSimulate = leaf;
			} else
			{

				foreach (PlayerTask task in poGame.CurrentPlayer.Options())
				{
					leaf.children.Add(new Node(task, leaf, leaf.depth+1));
				}

				nodeToSimulate = leaf.children[0]; 
				List<PlayerTask> taskToSimulate = new List<PlayerTask>();
				taskToSimulate.Add(nodeToSimulate.task);
				if (nodeToSimulate.task.PlayerTaskType != PlayerTaskType.END_TURN)
					poGame = poGame.Simulate(taskToSimulate)[nodeToSimulate.task];
				
				while(poGame == null)
				{
					if (leaf.children.Count <= 1)
						return leaf;
					poGame = pOGameIfSimulationFail;
					taskToSimulate.Clear();
					leaf.children.Remove(leaf.children[0]);
					nodeToSimulate = leaf.children[0];
					taskToSimulate.Add(nodeToSimulate.task);
					if (nodeToSimulate.task.PlayerTaskType != PlayerTaskType.END_TURN)
						poGame = poGame.Simulate(taskToSimulate)[nodeToSimulate.task];
				}	
			}
			return nodeToSimulate;
		}
		
		private float Simulation(Node nodeToSimulate, POGame poGame)
		{
			float result = -1;
			int simulationSteps = 0;
			PlayerTask task = null;

			List<PlayerTask> taskToSimulate = new List<PlayerTask>();
			if (poGame == null)
				return 0.5f;
			
			if(nodeToSimulate.task.PlayerTaskType == PlayerTaskType.END_TURN)
				return Estimator.estimateFromState(ESTIMATION_MODE, poGame);
				
			while (poGame.getGame().State != SabberStoneCore.Enums.State.COMPLETE)
			{
				task = SimulationPolicies.selectSimulationPolicy(SIMULATION_POLICY, poGame, Rnd, greedyAgent, CHILDREN_CONSIDERED_SIMULATING);
				taskToSimulate.Add(task);
				if (task.PlayerTaskType != PlayerTaskType.END_TURN)
					poGame = poGame.Simulate(taskToSimulate)[taskToSimulate[0]];

				taskToSimulate.Clear();

				if(poGame == null)
					return 0.5f;

				if(task.PlayerTaskType == PlayerTaskType.END_TURN) 
					return Estimator.estimateFromState(ESTIMATION_MODE, poGame);

				simulationSteps++;
			}


			if (poGame.CurrentPlayer.PlayState == SabberStoneCore.Enums.PlayState.CONCEDED
				|| poGame.CurrentPlayer.PlayState == SabberStoneCore.Enums.PlayState.LOST)
			{
				result = 0;
			} else if (poGame.CurrentPlayer.PlayState == SabberStoneCore.Enums.PlayState.WON)
			{
				result = 1;
			}

			return result;
		}

		private void Backpropagation(Node node, float result)
		{
			node.timesVisited++;
			node.totalValue += result;
			if(node.parent != null)
			{
				Backpropagation(node.parent, result);
			}
		}
	}

}
