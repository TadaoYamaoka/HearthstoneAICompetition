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
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneCoreAi.src.Agent.AlvaroMCTS
{
	class Node
	{
		public PlayerTask task		{ get; set; }
		public float totalValue		{ get; set; } 
		public int timesVisited		{ get; set; }
		public int depth			{ get; set; }
		public Node parent			{ get; set; }
		public List<Node> children;

		public Node()
		{
			totalValue = 0;
			timesVisited = 0;
			parent = null;
			task = null;
			depth = 0;
			children = new List<Node>();
		}

		public Node(PlayerTask task, Node parent, int depth)
		{
			totalValue = 0;
			timesVisited = 0;
			this.parent = parent;
			this.task = task;
			this.depth = depth;
			children = new List<Node>();
		}

	}
}
