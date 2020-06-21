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
using SabberStoneCore.Model.Entities;
using SabberStoneBasicAI.PartialObservation;

namespace SabberStoneCoreAi.src.Agent.AlvaroMCTS
{
	class Estimator
	{
		private static float WEAPON_ATTACK_IMPORTANCE = 0;
		private static float WEAPON_DURABILITY_IMPORTANCE = 0;
		private static float HEALTH_IMPORTANCE = 0;
		private static float BOARD_STATS_IMPORTANCE = 0;
		private static float HAND_SIZE_IMPORTANCE = 0;
		private static float DECK_REMAINING_IMPORTANCE = 0;
		private static float MANA_IMPORTANCE = 0;
		private static float SECRET_IMPORTANCE = 0;
		private static float OVERLOAD_IMPORTANCE = 0;
		private static float M_HAS_CHARGE = 0;
		private static float M_HAS_DEAHTRATTLE = 0;
		private static float M_HAS_DIVINE_SHIELD = 0;
		private static float M_HAS_INSPIRE = 0;
		private static float M_HAS_LIFE_STEAL = 0;
		private static float M_HAS_TAUNT = 0;
		private static float M_HAS_WINDFURY = 0;

		private static float MINION_COST_IMPORTANCE = 0;
		private static float SECRET_COST_IMPORTANCE = 0;
		private static float CARD_COST_IMPORTANCE = 0;
		private static float WEAPON_COST_IMPORTANCE = 0;


		static public float estimateFromState(string estimationMode,POGame poGame)
		{
			float score = 0.5f;

			switch (estimationMode)
			{
				case "LinearEstimation":
					score = linearEstimation(poGame);
					break;
				case "ValueEstimation":
					score = valueEstimation(poGame);
					break;
				case "GradualEstimation":
					score = gradualEstimation(poGame);
					break;
				default:
					score = 0;
					break;
			}

			return score;
		}

		static public void setWeights(float weaponAttack, float weaponDurability, float health, float boardStats, float handSize, float deckRemaining,
			float mana, float secret, float overload, float minionCost, float secretCost, float cardCost, float weaponCost, float M_HAS_CHARGE, float M_HAS_DEAHTRATTLE,
			float M_HAS_DIVINE_SHIELD, float M_HAS_INSPIRE, float M_HAS_LIFE_STEAL, float M_HAS_TAUNT, float M_HAS_WINDFURY)
		{
			WEAPON_ATTACK_IMPORTANCE = weaponAttack;
			WEAPON_DURABILITY_IMPORTANCE = weaponDurability;
			HEALTH_IMPORTANCE = health;
			BOARD_STATS_IMPORTANCE = boardStats;
			HAND_SIZE_IMPORTANCE = handSize;
			DECK_REMAINING_IMPORTANCE = deckRemaining;
			MANA_IMPORTANCE = mana;
			SECRET_IMPORTANCE = secret;
			OVERLOAD_IMPORTANCE = overload;

			MINION_COST_IMPORTANCE = minionCost;
			SECRET_COST_IMPORTANCE = secretCost;
			CARD_COST_IMPORTANCE = cardCost;
			WEAPON_COST_IMPORTANCE = weaponCost;
		}	

		static private float linearEstimation(POGame poGame)
		{
			float finalScore = 0.5f;
			
			float score1 = calculateScorePlayer(poGame.CurrentPlayer);
			float score2 = calculateScorePlayer(poGame.CurrentOpponent);

			finalScore = score1 - score2;
			finalScore = finalScore / Math.Max(score1, score2);
			finalScore /= 2;
			finalScore += 0.5f;
			return finalScore;
		}

		static private float calculateScorePlayer(Controller player)
		{
			float score = 0;
			float statsOnBoard = 0;
			foreach(Minion m in player.BoardZone.GetAll())
			{
				statsOnBoard += m.Health + m.AttackDamage;
				if (m.HasCharge)
					score = statsOnBoard + M_HAS_CHARGE;
				if (m.HasDeathrattle)
					score = statsOnBoard + M_HAS_DEAHTRATTLE;
				if (m.HasDivineShield)
					score = statsOnBoard + M_HAS_DIVINE_SHIELD;
				if (m.HasInspire)
					score = statsOnBoard + M_HAS_INSPIRE;
				if (m.HasLifeSteal)
					score = statsOnBoard + M_HAS_LIFE_STEAL;
				if (m.HasTaunt)
					score = statsOnBoard + M_HAS_TAUNT;
				if (m.HasWindfury)
					score = statsOnBoard + M_HAS_WINDFURY;
			}

			float weaponQuality = 0;
			if (player.Hero.Weapon != null)
				 weaponQuality = player.Hero.Weapon.AttackDamage * WEAPON_ATTACK_IMPORTANCE + player.Hero.Weapon.Durability * WEAPON_DURABILITY_IMPORTANCE;
			
			score = player.Hero.Health * HEALTH_IMPORTANCE + player.Hero.Armor * HEALTH_IMPORTANCE + weaponQuality + statsOnBoard * BOARD_STATS_IMPORTANCE + player.HandZone.Count * HAND_SIZE_IMPORTANCE
				+ player.DeckZone.Count * DECK_REMAINING_IMPORTANCE + player.BaseMana * MANA_IMPORTANCE + player.SecretZone.Count * SECRET_IMPORTANCE - player.OverloadOwed * OVERLOAD_IMPORTANCE;

			if (score <= 0)
				return 0.0001f;

			return score;
		}

		static private float gradualEstimation(POGame poGame)
		{
			float finalScore = 0.5f;

			float score1 = calculateScorePlayerGradual(poGame.CurrentPlayer);
			float score2 = calculateScorePlayerGradual(poGame.CurrentOpponent);

			finalScore = score1 - score2;
			finalScore = finalScore / Math.Max(score1, score2);
			finalScore /= 2;
			finalScore += 0.5f;
			return finalScore;
		}

		static private float calculateScorePlayerGradual(Controller player)
		{
			float score = 0;
			float statsOnBoard = 0;
			foreach (Minion m in player.BoardZone.GetAll())
			{
				statsOnBoard += m.Health + m.AttackDamage;
				if (m.HasCharge)
					score = statsOnBoard + M_HAS_CHARGE;
				if (m.HasDeathrattle)
					score = statsOnBoard + M_HAS_DEAHTRATTLE;
				if (m.HasDivineShield)
					score = statsOnBoard + M_HAS_DIVINE_SHIELD;
				if (m.HasInspire)
					score = statsOnBoard + M_HAS_INSPIRE;
				if (m.HasLifeSteal)
					score = statsOnBoard + M_HAS_LIFE_STEAL;
				if (m.HasTaunt)
					score = statsOnBoard + M_HAS_TAUNT;
				if (m.HasWindfury)
					score = statsOnBoard + M_HAS_WINDFURY;
			}

			float weaponQuality = 0;
			if (player.Hero.Weapon != null)
				weaponQuality = player.Hero.Weapon.AttackDamage * WEAPON_ATTACK_IMPORTANCE + player.Hero.Weapon.Durability * WEAPON_DURABILITY_IMPORTANCE;

			score = (float)Math.Sqrt(player.Hero.Health) * HEALTH_IMPORTANCE + (float)Math.Sqrt(player.Hero.Armor) * 2 * HEALTH_IMPORTANCE + weaponQuality + statsOnBoard * BOARD_STATS_IMPORTANCE + (float)Math.Sqrt(player.HandZone.Count) * HAND_SIZE_IMPORTANCE
				+ (float)Math.Sqrt(player.DeckZone.Count) * DECK_REMAINING_IMPORTANCE + player.BaseMana * MANA_IMPORTANCE + player.SecretZone.Count * SECRET_IMPORTANCE - player.OverloadOwed * OVERLOAD_IMPORTANCE;

			if (score <= 0)
				return 0.0001f;

			return score;
		}


		static private float valueEstimation(POGame poGame)
		{
			float finalScore = 0.5f;

			float score1 = calculateValuePlayer(poGame.CurrentOpponent);
			float score2 = calculateValuePlayer(poGame.CurrentPlayer);

			finalScore = score1 - score2;
			finalScore = finalScore / Math.Max(score1, score2);
			finalScore /= 2;
			finalScore += 0.5f;

			return finalScore;
		}

		private static float calculateValuePlayer(Controller player)
		{
			float score = 0;
			float BoardMana = 0;
			foreach (Minion minion in player.BoardZone.GetAll())
			{
				BoardMana += minion.Cost * MINION_COST_IMPORTANCE;
			}
			foreach (Spell spell in player.SecretZone.GetAll())
			{
				BoardMana += spell.Cost * SECRET_COST_IMPORTANCE;
			}
			foreach (IPlayable card in player.HandZone.GetAll())
			{
				BoardMana += card.Cost * CARD_COST_IMPORTANCE;
			}
			if (player.Hero.Weapon != null)
				BoardMana += player.Hero.Weapon.Cost * WEAPON_COST_IMPORTANCE;

			score = player.Hero.Health * HEALTH_IMPORTANCE + BoardMana + player.DeckZone.Count * DECK_REMAINING_IMPORTANCE
				+ player.BaseMana * MANA_IMPORTANCE;

			if (score == 0)
				score = 0.0001f;

			return score;
		}
	}
}
