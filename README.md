<p align="center">
<img src="docs/readme/HearthstoneAICompetition.png" alt="Competition logo" height="80%"/>
</p>

# Hearthstone-AI Competition 2020

Welcome to the 2020's Edition of the Hearthstone AI Competition. Please always download the latest version of the competition framework. Minor changes will be added until 17 April. Any further updates may include bugfixes. More information including setup information, tutorials and the source code of previous year's submissions can be found on the official competition homepage [Link](http://www.ci.ovgu.de/Research/HearthstoneAI.html).

**We are happy to announce that the Hearthstone AI competition will be part of the 2020 IEEE Conference on Games. Additionally, IEEE CIS is funding a first prize of 500$ for the best entry of each track. Please download the latest version of the competition framework.**


# Overview

The collectible online card game Hearthstone features a rich testbed and poses unique demands for generating artificial intelligence agents. The game is a turn-based card game between two opponents, using constructed decks of thirty cards along with a selected hero with a unique power. Players use their limited mana crystals to cast spells or summon minions to attack their opponent, with the goal to reduce the opponent’s health to zero. The competition aims to promote the stepwise development of fully autonomous AI agents in the context of Hearthstone.

Entrants will submit agents to participate in one of the two tracks:

* **Premade Deck Playing”-track:** participants will receive a list of three known decks and three decks unknown prior submission. During evaluation we will simulate all possible matchups for at least 100 games to determine the average win-rate for each agent. Determining and using the characteristics of the player’s and the opponent’s deck to the player’s advantage will help in winning the game. The decks for the premade deck playing track will be published at the 17 April.

* **“User Created Deck Playing”-track:** the competition framework allows agents to define their own deck. Finding a deck that can consistently beat a vast amount of other decks will play a key role in this competition track. Additionally, it gives the participants the chance in optimizing the agents’ strategy to the characteristics of their chosen deck.

As long as the number of subsmission remains below 32, we will use a round robin tournament to determine the best submissions based on their average win-rate. In case more agents are submitted we will use multiple smaller round-robin tournaments to determine likely candidates and use a final round robin tournament for determining the best three submissions.

**Competition Entry Deadline: July 1st 2019 23:59 UTC-12**


### Project Structure ###

* **SabberStoneCoreAI** *(.NET Core)*

  A test project to run A.I. simulations with predefinied decks and strategys.
		- Checkout Program.cs to find out how you can run your own tests.
		- Implement your own agents by interiting the AbstractAgent class and see the ExampleAgents folder for some examples.
		
* **SabberStoneCore** *(.NET Core)*

  Core simulator engine, all the functions needed for the simulator are in here. Check out the Wiki [Link](https://github.com/HearthSim/SabberStone/wiki) for informations about the core and how to use it.
		

### Installation

* See the Setup guide on [Link](http://www.ci.ovgu.de/Research/HearthstoneAI.html)

### Documentation

* Competition Website [Link](http://www.ci.ovgu.de/Research/HearthstoneAI.html)
* Wiki [Link](https://github.com/HearthSim/SabberStone/wiki)

### License

[![AGPLv3](https://www.gnu.org/graphics/agplv3-88x31.png)](http://choosealicense.com/licenses/agpl-3.0/)

SabberStone is licensed under the terms of the
[Affero GPLv3](https://www.gnu.org/licenses/agpl-3.0.en.html) or any later version.

### Community
We would like to thank the developers of the Sabberstone Framework on which our competition is based on. Special thanks go out to darkfriend77 (darkfriend@swissonline.ch) and Milva for their constant effort in keeping the Sabberstone framework up-to-date. Without your work, this competition would not exist!

Checkout the Sabberstone community:
* SabberStone on [Discord](https://discord.gg/my9WTwK) .. come and talk with us!
* SabberStone on [Reddit](https://redd.it/5p0ar8)
* SabberStone is a [HearthSim](http://hearthsim.info) project!
