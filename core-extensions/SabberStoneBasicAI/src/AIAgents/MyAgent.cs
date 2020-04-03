using System;
using System.Collections.Generic;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.PartialObservation;


// TODO choose your own namespace by setting up <submission_tag>
// each added file needs to use this namespace or a subnamespace of it
namespace SabberStoneBasicAI.AIAgents.submission_tag
{
	class MyAgent : AbstractAgent
	{

		public override void InitializeAgent()
		{
		}

		public override void FinalizeAgent()
		{
		}

		public override void FinalizeGame()
		{
		}

		public override PlayerTask GetMove(POGame poGame)
		{
			List<PlayerTask> options = poGame.CurrentPlayer.Options();
			return options[0];
		}

		public override void InitializeGame()
		{
		}
	}
}
