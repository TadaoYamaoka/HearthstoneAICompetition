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
using SabberStoneCoreAi.Agent;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.PartialObservation;

namespace SabberStoneCoreAi.src.Agent.AlvaroMCTS
{
	class TreePolicies
	{
		public static double selectTreePolicy(string treePolicy, Node node, int iterations, double exploreConstant, ref POGame poGame,
			double scoreImportance, ParametricGreedyAgent greedyAgent)
		{
			double score;
			switch (treePolicy)
			{
				case "UCB1":
					score = ucb1(node,iterations,exploreConstant);
					break;
				case "UCB1Heuristic":
					score = ucb1Heuristic(node, iterations, exploreConstant, ref poGame, scoreImportance, greedyAgent);
					break;
				default:
					score = 0;
					break;
			}

			return score;
		}


		//  vi = media de valores					|| ni = Veces visitado		|| N = numero de veces que se realiza una seleccion  || C = entre [0 - 2]
		//  vi = totalValores / Veces visitado		||							|| (Sin contar en la que estas)						 || empírico
		// UCB1(Si) = vi + C * sqrt(ln(N)/ni) value.
		public static double ucb1(Node node, int iterations, double EXPLORE_CONSTANT)
		{
			double value;
			if (node.timesVisited > 0)
			{
				value = (node.totalValue / (double)node.timesVisited) + EXPLORE_CONSTANT * Math.Sqrt(Math.Log(iterations) / node.timesVisited);
			} else
			{
				value = Double.MaxValue;
			}
			return value;
		}

		public static double ucb1Heuristic(Node node, int iterations, double EXPLORE_CONSTANT, ref POGame poGame, double SCORE_IMPORTANCE, ParametricGreedyAgent greedyAgent)
		{
			double value;
			if (node.timesVisited > 0)
			{
				List<PlayerTask> taskToSimulate = new List<PlayerTask>();
				taskToSimulate.Add(node.task);

				POGame stateAfterSimulate = poGame.Simulate(taskToSimulate)[node.task];

				double score = greedyAgent.scoreTask(poGame, stateAfterSimulate);
				value = (node.totalValue / (double)node.timesVisited) + EXPLORE_CONSTANT * Math.Sqrt(Math.Log(iterations) / node.timesVisited) + SCORE_IMPORTANCE * (score/(double)node.timesVisited);
			} else
			{
				value = Double.MaxValue;
			}
			return value;
		}
	}
}
