﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonUnity;
using PokemonUnity.Inventory;
using PokemonUnity.Combat;
using PokemonEssentials.Interface;
using PokemonEssentials.Interface.Battle;
using PokemonEssentials.Interface.Item;
using PokemonEssentials.Interface.Field;
using PokemonEssentials.Interface.Screen;
using PokemonEssentials.Interface.PokeBattle;
using PokemonEssentials.Interface.PokeBattle.Effects;

namespace PokemonUnity.UX
{
	public partial class Battler : IBattlerEffectIE
	{
		#region Sleep
		IEnumerator IBattlerEffectIE.CanSleep(IBattler attacker, bool showMessages, IBattleMove move, bool ignorestatus, System.Action<bool> result) {
			if (isFainted()) { result?.Invoke(false); yield break; }
			bool selfsleep=(attacker.IsNotNullOrNone() && attacker.Index==this.Index);
			if (!ignorestatus && Status==Status.SLEEP) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1} is already asleep!",ToString()));
				result?.Invoke(false); yield break;
			}
			if (!selfsleep) {
				if (Status!=0 ||
					(effects.Substitute>0 && (!move.IsNotNullOrNone() || !move.ignoresSubstitute(attacker)))) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
					result?.Invoke(false); yield break;
				}
			}
			if (!this.isAirborne(attacker.IsNotNullOrNone() && attacker.hasMoldBreaker()))
				if (@battle.field.ElectricTerrain>0) {
					if (showMessages) yield return @battle.Display(Game._INTL("The Electric Terrain prevented {1} from falling asleep!",ToString(true)));
					result?.Invoke(false); yield break;
				} else if (@battle.field.MistyTerrain>0) {
					if (showMessages) yield return @battle.Display(Game._INTL("The Misty Terrain prevented {1} from falling asleep!",ToString(true)));
					result?.Invoke(false); yield break;
				}
			if ((attacker.IsNotNullOrNone() && attacker.hasMoldBreaker()) || !hasWorkingAbility(Abilities.SOUNDPROOF))
				for (int i = 0; i < @battle.battlers.Length; i++)
					if (@battle.battlers[i].effects.Uproar>0) {
						if (showMessages) yield return @battle.Display(Game._INTL("But the uproar kept {1} awake!",ToString(true)));
						result?.Invoke(false); yield break;
					}
			if (!attacker.IsNotNullOrNone() || attacker.Index==this.Index || !attacker.hasMoldBreaker()) {
				if (hasWorkingAbility(Abilities.VITAL_SPIRIT) ||
					hasWorkingAbility(Abilities.INSOMNIA) ||
					hasWorkingAbility(Abilities.SWEET_VEIL) ||
					(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
					(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
					@battle.Weather==Weather.HARSHSUN))) {
					string abilityname=Game._INTL(Ability.ToString(TextScripts.Name));
					if (showMessages) yield return @battle.Display(Game._INTL("{1} stayed awake using its {2}!",ToString(),abilityname));
					result?.Invoke(false); yield break;
				}
				if (Partner.hasWorkingAbility(Abilities.SWEET_VEIL) ||
					(Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS))) {
					string abilityname=Game._INTL(Partner.Ability.ToString(TextScripts.Name));
					if (showMessages) yield return @battle.Display(Game._INTL("{1} stayed awake using its partner's {2}!",ToString(),abilityname));
					result?.Invoke(false); yield break;
				}
			}
			if (!selfsleep)
				if (OwnSide.Safeguard>0 &&
					(!attacker.IsNotNullOrNone() || !attacker.hasWorkingAbility(Abilities.INFILTRATOR))) {
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s team is protected by Safeguard!",ToString()));
					result?.Invoke(false); yield break;
				}
			result?.Invoke(true);
		}

		//bool IBattlerEffect.CanSleepYawn() {
		//	if (status!=0) return false;
		//	if (!hasWorkingAbility(Abilities.SOUNDPROOF))
		//		for (int i = 0; i < @battle.battlers.Length; i++)
		//		if (@battle.battlers[i].effects.Uproar>0) return false;
		//	if (!this.isAirborne()) {
		//		if (@battle.field.ElectricTerrain>0) return false;
		//		if (@battle.field.MistyTerrain>0) return false;
		//	}
		//	if (hasWorkingAbility(Abilities.VITAL_SPIRIT) ||
		//		hasWorkingAbility(Abilities.INSOMNIA) ||
		//		hasWorkingAbility(Abilities.SWEET_VEIL) ||
		//		(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
		//		@battle.Weather==Weather.HARSHSUN)))
		//		return false;
		//	if (Partner.hasWorkingAbility(Abilities.SWEET_VEIL)) return false;
		//	return true;
		//}

		new public IEnumerator Sleep(string msg=null) {
			Status=Status.SLEEP;
			this.StatusCount=2+@battle.Random(3);
			if (this.hasWorkingAbility(Abilities.EARLY_BIRD)) this.StatusCount=(int)Math.Floor(this.StatusCount/2d);
			CancelMoves();
			@battle.CommonAnimation("Sleep",this,null);
			if (!string.IsNullOrEmpty(msg))
				yield return @battle.Display(msg);
			else
				yield return @battle.Display(Game._INTL("{1} fell asleep!",ToString()));
			GameDebug.Log($"[Status change] #{ToString()} fell asleep (#{this.StatusCount} turns)");
		}

		//public void SleepSelf(int duration=-1) {
		//	Status=Status.SLEEP;
		//	if (duration>0)
		//		this.StatusCount=(byte)duration;
		//	else
		//		this.StatusCount=(byte)(2+@battle.Random(3));
		//	if (this.hasWorkingAbility(Abilities.EARLY_BIRD)) this.StatusCount=(int)Math.Floor(this.StatusCount/2d);
		//	CancelMoves();
		//	@battle.CommonAnimation("Sleep",this,null);
		//	GameDebug.Log($"[Status change] #{ToString()} made itself fall asleep (#{this.StatusCount} turns)");
		//}
		#endregion

		#region Poison
		public IEnumerator CanPoison(IBattler attacker,bool showMessages,IBattleMove move=null,System.Action<bool> result=null) {
			if (isFainted()) { result?.Invoke(false); yield break; }
			if (Status==Status.POISON) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1} is already poisoned.",ToString()));
				result?.Invoke(false); yield break;
			}
			if (Status!=0 ||
				(effects.Substitute>0 && (!move.IsNotNullOrNone() || !move.ignoresSubstitute(attacker)))) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
				result?.Invoke(false); yield break;
			}
			if ((HasType(Types.POISON) || HasType(Types.STEEL)) && !hasWorkingItem(Items.RING_TARGET)) {
				if (showMessages) yield return @battle.Display(Game._INTL("It doesn't affect {1}...",ToString(true)));
				result?.Invoke(false); yield break;
			}
			if (@battle.field.MistyTerrain>0 &&
				!this.isAirborne(attacker.IsNotNullOrNone() && attacker.hasMoldBreaker())) {
				if (showMessages) yield return @battle.Display(Game._INTL("The Misty Terrain prevented {1} from being poisoned!",ToString(true)));
				result?.Invoke(false); yield break;
			}
			if (!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker()) {
				if (hasWorkingAbility(Abilities.IMMUNITY) ||
					(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
					(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
					@battle.Weather==Weather.HARSHSUN))) {
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents poisoning!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
					result?.Invoke(false); yield break;
				}
				if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
					string abilityname=Game._INTL(Partner.Ability.ToString(TextScripts.Name));
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s partner's {2} prevents poisoning!",ToString(),abilityname));
					result?.Invoke(false); yield break;
				}
			}
			if (OwnSide.Safeguard>0 &&
				(!attacker.IsNotNullOrNone() || !attacker.hasWorkingAbility(Abilities.INFILTRATOR))) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s team is protected by Safeguard!",ToString()));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true);
		}

		public IEnumerator CanPoisonSynchronize(IBattler opponent, System.Action<bool> result) {
			if (isFainted()) { result(false); yield break; }
			if ((HasType(Types.POISON) || HasType(Types.STEEL)) && !hasWorkingItem(Items.RING_TARGET)) {
				yield return @battle.Display(Game._INTL("{1}'s {2} had no effect on {3}!",
					opponent.ToString(),Game._INTL(opponent.Ability.ToString(TextScripts.Name)),ToString(true)));
				result(false); yield break;
			}
			if (Status != 0) { result(false); yield break; }
			if (hasWorkingAbility(Abilities.IMMUNITY) ||
				(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
				(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
													@battle.Weather==Weather.HARSHSUN))) {
				yield return @battle.Display(Game._INTL("{1}'s {2} prevents {3}'s {4} from working!",
					ToString(),Game._INTL(Ability.ToString(TextScripts.Name)),
					opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
				result(false); yield break;
			}
			if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
				yield return @battle.Display(Game._INTL("{1}'s {2} prevents {3}'s {4} from working!",
					Partner.ToString(),Game._INTL(Partner.Ability.ToString(TextScripts.Name)),
					opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
				result(false); yield break;
			}
			result(true);
		}

		//public bool CanPoisonSpikes(bool moldbreaker=false) {
		//	if (isFainted()) return false;
		//	if (Status!=0) return false;
		//	if (HasType(Types.POISON) || HasType(Types.STEEL)) return false;
		//	if (!moldbreaker) {
		//		if (hasWorkingAbility(Abilities.IMMUNITY) ||
		//			(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
		//			(Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS))) return false;
		//		if (hasWorkingAbility(Abilities.LEAF_GUARD) &&
		//			(@battle.Weather==Weather.SUNNYDAY ||
		//			@battle.Weather==Weather.HARSHSUN)) return false;
		//	}
		//	if (OwnSide.Safeguard>0) return false;
		//	return true;
		//}

		new public IEnumerator Poison(IBattler attacker,string msg=null, bool toxic=false) {
			Status=Status.POISON;
			this.StatusCount=(toxic) ? 1 : 0;
			this.effects.Toxic=0;
			@battle.CommonAnimation("Poison",this,null);
			if (!string.IsNullOrEmpty(msg))
				yield return @battle.Display(msg);
			else {
				if (toxic)
					yield return @battle.Display(Game._INTL("{1} was badly poisoned!",ToString()));
				else
					yield return @battle.Display(Game._INTL("{1} was poisoned!",ToString()));
			}
			if (toxic)
				GameDebug.Log($"[Status change] #{ToString()} was badly poisoned]");
			else
				GameDebug.Log($"[Status change] #{ToString()} was poisoned");
			if (attacker.IsNotNullOrNone() && this.Index!=attacker.Index &&
				this.hasWorkingAbility(Abilities.SYNCHRONIZE))
				if (attacker is IBattlerEffect a && a.CanPoisonSynchronize(this)) {
					GameDebug.Log($"[Ability triggered] #{this.ToString()}'s Synchronize");
					a.Poison(null,Game._INTL("{1}'s {2} poisoned {3}!",this.ToString(),
						Game._INTL(Ability.ToString(TextScripts.Name)),attacker.ToString(true)),toxic);
				}
		}
		#endregion

		#region Burn
		public IEnumerator CanBurn(IBattler attacker,bool showMessages,IBattleMove move=null,System.Action<bool> result=null) {
			if (isFainted()) { result?.Invoke(false); yield break; }
			if (Status==Status.BURN) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1} already has a burn.",ToString()));
				result?.Invoke(false); yield break;
			}
			if (Status!=0 ||
				(effects.Substitute>0 && (!move.IsNotNullOrNone() || !move.ignoresSubstitute(attacker)))) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
				result?.Invoke(false); yield break;
			}
			if (@battle.field.MistyTerrain>0 &&
				!this.isAirborne(attacker.IsNotNullOrNone() && attacker.hasMoldBreaker())) {
				if (showMessages) yield return @battle.Display(Game._INTL("The Misty Terrain prevented {1} from being burned!",ToString(true)));
				result?.Invoke(false); yield break;
			}
			if (HasType(Types.FIRE) && !hasWorkingItem(Items.RING_TARGET)) {
				if (showMessages) yield return @battle.Display(Game._INTL("It doesn't affect {1}...",ToString(true)));
				result?.Invoke(false); yield break;
			}
			if (!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker()) {
				if (hasWorkingAbility(Abilities.WATER_VEIL) ||
					(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
					(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
					@battle.Weather==Weather.HARSHSUN))) {
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents burns!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
						result?.Invoke(false); yield break;
				}
				if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
					string abilityname=Game._INTL(Partner.Ability.ToString(TextScripts.Name));
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s partner's {2} prevents burns!",ToString(),abilityname));
					result?.Invoke(false); yield break;
				}
			}
			if (OwnSide.Safeguard>0 &&
				(!attacker.IsNotNullOrNone() || !attacker.hasWorkingAbility(Abilities.INFILTRATOR))) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s team is protected by Safeguard!",ToString()));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true);
		}

		public IEnumerator CanBurnSynchronize(IBattler opponent, System.Action<bool> result) {
			if (isFainted()) { result(false); yield break; }
			if (Status != 0) { result(false); yield break; }
			if (HasType(Types.FIRE) && !hasWorkingItem(Items.RING_TARGET)) {
				yield return @battle.Display(Game._INTL("{1}'s {2} had no effect on {3}!",
					opponent.ToString(),Game._INTL(opponent.Ability.ToString(TextScripts.Name)),ToString(true)));
				result(false); yield break;
			}   
			if (hasWorkingAbility(Abilities.WATER_VEIL) ||
				(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
				(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
				@battle.Weather==Weather.HARSHSUN))) {
				yield return @battle.Display(Game._INTL("{1}'s {2} prevents {3}'s {4} from working!",
					ToString(),Game._INTL(Ability.ToString(TextScripts.Name)),
					opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
				result(false); yield break;
			}
			if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
				yield return @battle.Display(Game._INTL("{1}'s {2} prevents {3}'s {4} from working!",
					Partner.ToString(),Game._INTL(Partner.Ability.ToString(TextScripts.Name)),
					opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
				result(false); yield break;
			}
			result(true);
		}

		new public IEnumerator Burn(IBattler attacker,string msg=null) {
			Status=Status.BURN;
			this.StatusCount=0;
			@battle.CommonAnimation("Burn",this,null);
			if (!string.IsNullOrEmpty(msg))
				yield return @battle.Display(msg);
			else
				yield return @battle.Display(Game._INTL("{1} was burned!",ToString()));
			GameDebug.Log($"[Status change] #{ToString()} was burned");
			if (attacker.IsNotNullOrNone() && this.Index!=attacker.Index &&
				this.hasWorkingAbility(Abilities.SYNCHRONIZE))
				if (attacker is IBattlerEffect a && a.CanBurnSynchronize(this)) {
					GameDebug.Log($"[Ability triggered] #{this.ToString()}'s Synchronize");
					a.Burn(null,Game._INTL("{1}'s {2} burned {3}!",this.ToString(),
						Game._INTL(Ability.ToString(TextScripts.Name)),attacker.ToString(true)));
				}
		}
		#endregion

		#region Paralyze
		public IEnumerator CanParalyze(IBattler attacker,bool showMessages,IBattleMove move=null,System.Action<bool> result=null) {
			if (isFainted()) { result?.Invoke(false); yield break; }
			if (Status==Status.PARALYSIS) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1} is already paralyzed!",ToString()));
				result?.Invoke(false); yield break;
			}
			if (Status!=0 ||
				(effects.Substitute>0 && (!move.IsNotNullOrNone() || !move.ignoresSubstitute(attacker)))) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
				result?.Invoke(false); yield break;
			}
			if (@battle.field.MistyTerrain>0 &&
				!this.isAirborne(attacker.IsNotNullOrNone() && attacker.hasMoldBreaker())) {
				if (showMessages) yield return @battle.Display(Game._INTL("The Misty Terrain prevented {1} from being paralyzed!",ToString(true)));
				result?.Invoke(false); yield break;
			}
			if (HasType(Types.ELECTRIC) && !hasWorkingItem(Items.RING_TARGET) && Core.USENEWBATTLEMECHANICS) {
				if (showMessages) yield return @battle.Display(Game._INTL("It doesn't affect {1}...",ToString(true)));
				result?.Invoke(false); yield break;
			}
			if (!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker()) {
				if (hasWorkingAbility(Abilities.LIMBER) ||
					(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
					(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
					@battle.Weather==Weather.HARSHSUN))) {
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents paralysis!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
					result?.Invoke(false); yield break;
				}
				if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
					string abilityname=Game._INTL(Partner.Ability.ToString(TextScripts.Name));
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s partner's {2} prevents paralysis!",ToString(),abilityname));
					result?.Invoke(false); yield break;
				}
			}
			if (OwnSide.Safeguard>0 &&
				(!attacker.IsNotNullOrNone() || !attacker.hasWorkingAbility(Abilities.INFILTRATOR))) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s team is protected by Safeguard!",ToString()));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true);
		}

		public IEnumerator CanParalyzeSynchronize(IBattler opponent, System.Action<bool> result) {
			if (Status != 0) { result(false); yield break; }
			if (@battle.field.MistyTerrain > 0 && !this.isAirborne()) { result(false); yield break; }
			if (HasType(Types.ELECTRIC) && !hasWorkingItem(Items.RING_TARGET) && Core.USENEWBATTLEMECHANICS) { result(false); yield break; }
			if (hasWorkingAbility(Abilities.LIMBER) ||
				(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
				(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
				@battle.Weather==Weather.HARSHSUN))) {
				yield return @battle.Display(Game._INTL("{1}'s {2} prevents {3}'s {4} from working!",
					ToString(),Game._INTL(Ability.ToString(TextScripts.Name)),
					opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
				result(false); yield break;
			}
			if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
				yield return @battle.Display(Game._INTL("{1}'s {2} prevents {3}'s {4} from working!",
					Partner.ToString(),Game._INTL(Partner.Ability.ToString(TextScripts.Name)),
					opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
				result(false); yield break;
			}
			result(true);
		}

		new public IEnumerator Paralyze(IBattler attacker,string msg=null) {
			Status=Status.PARALYSIS;
			this.StatusCount=0;
			@battle.CommonAnimation("Paralysis",this,null);
			if (!string.IsNullOrEmpty(msg))
				yield return @battle.Display(msg);
			else
				yield return @battle.Display(Game._INTL("{1} is paralyzed! It may be unable to move!",ToString()));
			GameDebug.Log($"[Status change] #{ToString()} was paralyzed");
			if (attacker.IsNotNullOrNone() && this.Index!=attacker.Index &&
				this.hasWorkingAbility(Abilities.SYNCHRONIZE))
				if (attacker is IBattlerEffect a && a.CanParalyzeSynchronize(this)) {
					GameDebug.Log($"[Ability triggered] #{this.ToString()}'s Synchronize");
					a.Paralyze(null,Game._INTL("{1}'s {2} paralyzed {3}! It may be unable to move!",
						this.ToString(),Game._INTL(Ability.ToString(TextScripts.Name)),attacker.ToString(true)));
				}
		}
		#endregion

		#region Freeze
		IEnumerator IBattlerEffectIE.CanFreeze(IBattler attacker,bool showMessages,IBattleMove move, System.Action<bool> result) {
			if (isFainted()) { result(false); yield break; }
			if (Status==Status.FROZEN) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1} is already frozen solid!",ToString()));
				result(false); yield break;
			}
			if (Status!=0 ||
				(effects.Substitute>0 && (!move.IsNotNullOrNone() || !move.ignoresSubstitute(attacker))) ||
				@battle.Weather==Weather.SUNNYDAY ||
				@battle.Weather==Weather.HARSHSUN) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
				result(false); yield break;
			}
			if (HasType(Types.ICE) && !hasWorkingItem(Items.RING_TARGET)) {
				if (showMessages) yield return @battle.Display(Game._INTL("It doesn't affect {1}...",ToString(true)));
				result(false); yield break;
			}
			if (@battle.field.MistyTerrain>0 &&
				!this.isAirborne(attacker.IsNotNullOrNone() && attacker.hasMoldBreaker())) {
				if (showMessages) yield return @battle.Display(Game._INTL("The Misty Terrain prevented {1} from being frozen!",ToString(true)));
				result(false); yield break;
			}
			if (!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker()) {
				if (hasWorkingAbility(Abilities.MAGMA_ARMOR) ||
					(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) ||
					(hasWorkingAbility(Abilities.LEAF_GUARD) && (@battle.Weather==Weather.SUNNYDAY ||
					@battle.Weather==Weather.HARSHSUN))) {
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents freezing!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
					result(false); yield break;
				}
				if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
					string abilityname=Game._INTL(Partner.Ability.ToString(TextScripts.Name));
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s partner's {2} prevents freezing!",ToString(),abilityname));
					result(false); yield break;
				}
			}
			if (OwnSide.Safeguard>0 &&
				(!attacker.IsNotNullOrNone() || !attacker.hasWorkingAbility(Abilities.INFILTRATOR))) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s team is protected by Safeguard!",ToString()));
				result(false); yield break;
			}
			result(true);
		}

		new public IEnumerator Freeze(string msg=null) {
			Status=Status.FROZEN;
			this.StatusCount=0;
			CancelMoves();
			@battle.CommonAnimation("Frozen",this,null);
			if (!string.IsNullOrEmpty(msg))
				yield return @battle.Display(msg);
			else
				yield return @battle.Display(Game._INTL("{1} was frozen solid!",ToString()));
			GameDebug.Log($"[Status change] #{ToString()} was frozen");
		}
		#endregion

		#region Generalized status displays
		new public IEnumerator ContinueStatus(bool showAnim=true) {
			switch (Status) {
				case Status.SLEEP:
					@battle.CommonAnimation("Sleep",this,null);
					yield return @battle.Display(Game._INTL("{1} is fast asleep.",ToString()));
					break;
				case Status.POISON:
					@battle.CommonAnimation("Poison",this,null);
					yield return @battle.Display(Game._INTL("{1} was hurt by poison!",ToString()));
					break;
				case Status.BURN:
					@battle.CommonAnimation("Burn",this,null);
					yield return @battle.Display(Game._INTL("{1} was hurt by its burn!",ToString()));
					break;
				case Status.PARALYSIS:
					@battle.CommonAnimation("Paralysis",this,null);
					yield return @battle.Display(Game._INTL("{1} is paralyzed! It can't move!",ToString())) ;
					break;
				case Status.FROZEN:
					@battle.CommonAnimation("Frozen",this,null);
					yield return @battle.Display(Game._INTL("{1} is frozen solid!",ToString()));
					break;
			}
		}

		new public IEnumerator CureStatus(bool showMessages=true) {
			Status oldstatus=Status;
			Status=0;
			this.StatusCount=0;
			if (showMessages)  
				switch (oldstatus) {
					case Status.SLEEP:
						yield return @battle.Display(Game._INTL("{1} woke up!",ToString()));
						break;
					case Status.POISON:
					case Status.BURN:
					case Status.PARALYSIS:
						yield return @battle.Display(Game._INTL("{1} was cured of paralysis.",ToString()));
						break;
					case Status.FROZEN:
						yield return @battle.Display(Game._INTL("{1} thawed out!",ToString()));
						break;
				}
			GameDebug.Log($"[Status change] #{ToString()}'s status was cured");
		}
		#endregion

		#region Confuse
		public IEnumerator CanConfuse(IBattler attacker=null,bool showMessages=true,IBattleMove move=null,System.Action<bool> result=null) {
			if (isFainted()) { result?.Invoke(false); yield break; }
			if (effects.Confusion>0) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1} is already confused!",ToString()));
				result?.Invoke(false); yield break;
			}
			if (effects.Substitute>0 && (!move.IsNotNullOrNone() || !move.ignoresSubstitute(attacker))) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
				result?.Invoke(false); yield break;
			}
			if (!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker())
				if (hasWorkingAbility(Abilities.OWN_TEMPO)) {
					if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents confusion!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
					result?.Invoke(false); yield break;
				}
			if (OwnSide.Safeguard>0 &&
				(!attacker.IsNotNullOrNone() || !attacker.hasWorkingAbility(Abilities.INFILTRATOR))) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s team is protected by Safeguard!",ToString()));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true);
		}

		public IEnumerator CanConfuseSelf(bool showMessages,System.Action<bool> result=null) {
			if (isFainted()) { result?.Invoke(false); yield break; }
			if (effects.Confusion>0) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1} is already confused!",ToString()));
				result?.Invoke(false); yield break;
			}
			if (hasWorkingAbility(Abilities.OWN_TEMPO)) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents confusion!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true); 
		}

		//public void Confuse() {
		//	effects.Confusion=2+@battle.Random(4);
		//	@battle.CommonAnimation("Confusion",this,null);
		//	GameDebug.Log($"[Lingering effect triggered] #{ToString()} became confused (#{effects.Confusion} turns)");
		//}

		new public IEnumerator ConfuseSelf() {
			bool canConfuseSelf = false;
			yield return CanConfuseSelf(false, result: value => canConfuseSelf = value);
			if (canConfuseSelf) {
			//if (CanConfuseSelf(false)) {
				effects.Confusion=2+@battle.Random(4);
				@battle.CommonAnimation("Confusion",this,null);
				yield return @battle.Display(Game._INTL("{1} became confused!",ToString()));
				GameDebug.Log($"[Lingering effect triggered] #{ToString()} became confused (#{effects.Confusion} turns)");
			}
		}

		new public IEnumerator ContinueConfusion() {
			@battle.CommonAnimation("Confusion",this,null);
			yield return @battle.DisplayBrief(Game._INTL("{1} is confused!",ToString()));
		}

		new public IEnumerator CureConfusion(bool showMessages=true) {
			effects.Confusion=0;
			if (showMessages) yield return @battle.Display(Game._INTL("{1} snapped out of confusion!",ToString()));
			GameDebug.Log($"[End of effect] #{ToString()} was cured of confusion");
		}
		#endregion

		#region Attraction
		public IEnumerator CanAttract(IBattler attacker,bool showMessages=true,System.Action<bool> result=null) {
			if (isFainted()) { result?.Invoke(false); yield break; }
			if (!attacker.IsNotNullOrNone() || attacker.isFainted()) { result?.Invoke(false); yield break; }
			if (effects.Attract>=0) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
				result?.Invoke(false); yield break;
			}
			bool? agender=attacker.Gender;
			bool? ogender=this.Gender;
			if (!agender.HasValue || !ogender.HasValue || agender==ogender) {
				if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
				result?.Invoke(false); yield break;
			}
			if ((!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker()) && hasWorkingAbility(Abilities.OBLIVIOUS)) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents romance!",ToString(),
					Game._INTL(Ability.ToString(TextScripts.Name))));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true);
		}

		new public IEnumerator Attract(IBattler attacker,string msg=null) {
			effects.Attract=attacker.Index;
			@battle.CommonAnimation("Attract",this,null);
			if (!string.IsNullOrEmpty(msg))
				yield return @battle.Display(msg);
			else
				yield return @battle.Display(Game._INTL("{1} fell in love!",ToString()));
			GameDebug.Log($"[Lingering effect triggered] #{ToString()} became infatuated (with #{attacker.ToString(true)})");
			if (this.hasWorkingItem(Items.DESTINY_KNOT) &&
				attacker is IBattlerEffect a && a.CanAttract(this,false)) {
				GameDebug.Log($"[Item triggered] #{ToString()}'s Destiny Knot");
				a.Attract(this,Game._INTL("{1}'s {2} made {3} fall in love!",ToString(),
					Game._INTL(this.Item.ToString(TextScripts.Name)),attacker.ToString(true)));
			}
		}

		new public IEnumerator AnnounceAttract(IBattler seducer) {
			@battle.CommonAnimation("Attract",this,null);
			yield return @battle.DisplayBrief(Game._INTL("{1} is in love with {2}!",
				ToString(),seducer.ToString(true)));
		}

		new public IEnumerator ContinueAttract() {
			yield return @battle.Display(Game._INTL("{1} is immobilized by love!",ToString()));
		}

		//public void CureAttract() {
		//	effects.Attract=-1;
		//	GameDebug.Log($"[End of effect] #{ToString()} was cured of infatuation");
		//}
		#endregion

		#region Flinching
		//public bool Flinch(IBattler attacker) {
		//	if ((!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker()) && hasWorkingAbility(Abilities.INNER_FOCUS)) return false;
		//	effects.Flinch=true;
		//	return true;
		//}
		#endregion

		#region Increase stat stages
		//public bool TooHigh(Stats stat) {
		//	return @stages[(int)stat]>=6;
		//}

		public IEnumerator CanIncreaseStatStage(Stats stat,IBattler attacker=null,bool showMessages=false,IBattleMove move=null,bool moldbreaker=false,bool ignoreContrary=false,System.Action<bool> result=null) {
			if (!moldbreaker)
				if (!attacker.IsNotNullOrNone() || attacker.Index==this.Index || !attacker.hasMoldBreaker())
					if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
					{ yield return CanReduceStatStage(stat,attacker,showMessages,moldbreaker:moldbreaker,ignoreContrary: true, result: value => result?.Invoke(value)); yield break; }
			if (isFainted()) { result?.Invoke(false); yield break; }
			if (TooHigh(stat)) { 
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} won't go any higher!",
					ToString(),Game._INTL(stat.ToString(TextScripts.Name))));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true);
		}

		//public int IncreaseStatBasic(Stats stat,int increment,IBattler attacker=null,bool moldbreaker=false,bool ignoreContrary=false) {
		//	if (!moldbreaker)
		//		if (!attacker.IsNotNullOrNone() || attacker.Index==this.Index || !attacker.hasMoldBreaker()) { 
		//			if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
		//				return ReduceStatBasic(stat,increment,attacker,moldbreaker,true);
		//			if (hasWorkingAbility(Abilities.SIMPLE)) increment*=2;
		//		}
		//	increment=Math.Min(increment,6-@stages[(int)stat]);
		//	GameDebug.Log($"[Stat change] #{ToString()}'s #{Game._INTL(stat.ToString(TextScripts.Name))} rose by #{increment} stage(s) (was #{@stages[(int)stat]}, now #{@stages[(int)stat]+increment})");
		//	@stages[(int)stat]+=increment;
		//	return increment;
		//}

		public IEnumerator IncreaseStat(Stats stat,int increment,IBattler attacker,bool showMessages,IBattleMove move=null,bool upanim=true,bool moldbreaker=false,bool ignoreContrary=false,System.Action<bool> result=null) {
			if (!moldbreaker)
				if (!attacker.IsNotNullOrNone() || attacker.Index==this.Index || !attacker.hasMoldBreaker())
					if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
					{ yield return ReduceStat(stat,increment,attacker,showMessages,move,upanim,moldbreaker,true, result: value => result?.Invoke(value)); yield break; }
			if (stat != Stats.ATTACK && stat != Stats.DEFENSE &&
				stat != Stats.SPATK && stat != Stats.SPDEF &&
				stat != Stats.SPEED && stat != Stats.EVASION &&
				stat != Stats.ACCURACY) { result?.Invoke(false); yield break; }
			bool canIncreaseStatStage = false;
			yield return CanIncreaseStatStage(stat, attacker, showMessages, move, moldbreaker, ignoreContrary, result: value => canIncreaseStatStage = value);
			if (canIncreaseStatStage) {
			//if (CanIncreaseStatStage(stat, attacker, showMessages, move, moldbreaker, ignoreContrary)) { 
				increment=IncreaseStatBasic(stat,increment,attacker,moldbreaker,ignoreContrary);
				if (increment > 0) { 
					if (ignoreContrary)
						if (upanim) yield return @battle.Display(Game._INTL("{1}'s {2} activated!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
					if (upanim) @battle.CommonAnimation("StatUp", this, null);
					string[] arrStatTexts=new string[] {Game._INTL("{1}'s {2} rose!",ToString(),Game._INTL(stat.ToString(TextScripts.Name))),
						Game._INTL("{1}'s {2} rose sharply!",ToString(),Game._INTL(stat.ToString(TextScripts.Name))),
						Game._INTL("{1}'s {2} rose drastically!",ToString(),Game._INTL(stat.ToString(TextScripts.Name)))};
					yield return @battle.Display(arrStatTexts[Math.Min(increment-1,2)]);
					result?.Invoke(true); yield break;
				}
			}
			result?.Invoke(false); 
		}

		public IEnumerator IncreaseStatWithCause(Stats stat,int increment,IBattler attacker,string cause,bool showanim=true,bool showmessage=true,bool moldbreaker=false,bool ignoreContrary=false,System.Action<bool> result=null) {
			if (!moldbreaker)
				if (!attacker.IsNotNullOrNone() || attacker.Index == this.Index || !attacker.hasMoldBreaker())
					if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
					{ yield return ReduceStatWithCause(stat, increment, attacker, cause, showanim, showmessage, moldbreaker, true, result: value => result?.Invoke(value)); yield break; }
			if (stat != Stats.ATTACK && stat != Stats.DEFENSE &&
				stat != Stats.SPATK && stat != Stats.SPDEF &&
				stat != Stats.SPEED && stat != Stats.EVASION &&
				stat != Stats.ACCURACY) { result?.Invoke(false); yield break; }
			bool canIncreaseStatStage = false;
			yield return CanIncreaseStatStage(stat,attacker,false,null,moldbreaker,ignoreContrary, result: value => canIncreaseStatStage = value);
			if (canIncreaseStatStage) {
			//if (CanIncreaseStatStage(stat,attacker,false,null,moldbreaker,ignoreContrary)) {
				increment=IncreaseStatBasic(stat,increment,attacker,moldbreaker,ignoreContrary);
				if (increment > 0) { 
					//if (ignoreContrary) //ToDo: UpAnimation?
					//  if (upanim) yield return @battle.Display(Game._INTL("{1}'s {2} activated!",ToString(),Game._INTL(this.ability.ToString(TextScripts.Name))));
					if (showanim) @battle.CommonAnimation("StatUp", this, null); 
					string [] arrStatTexts = null;
					if (attacker.Index==this.Index)
						arrStatTexts=new string[] {Game._INTL("{1}'s {2} raised its {3}!",ToString(),cause,Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} sharply raised its {3}!",ToString(),cause,Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} went way up!",ToString(),Game._INTL(stat.ToString(TextScripts.Name)))};
					else
						arrStatTexts=new string[] {Game._INTL("{1}'s {2} raised {3}'s {4}!",attacker.ToString(),cause,ToString(true),Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} sharply raised {3}'s {4}!",attacker.ToString(),cause,ToString(true),Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} drastically raised {3}'s {4}!",attacker.ToString(),cause,ToString(true),Game._INTL(stat.ToString(TextScripts.Name)))};
					if (showmessage) yield return @battle.Display(arrStatTexts[Math.Min(increment-1,2)]); 
					result?.Invoke(true); yield break;
				}
			}
			result?.Invoke(false);
		}
		#endregion

		#region Decrease stat stages
		//public bool TooLow(Stats stat) {
		//	return @stages[(int)stat]<=-6;
		//}

		/// <summary>
		/// Tickle (04A) and Noble Roar (13A) can't use this, but replicate it instead.
		/// (Reason is they lowers more than 1 stat independently, and therefore could
		/// show certain messages twice which is undesirable.)
		/// </summary>
		/// <param name="stat"></param>
		/// <param name="attacker"></param>
		/// <param name="showMessages"></param>
		/// <param name="move"></param>
		/// <param name="moldbreaker"></param>
		/// <param name="ignoreContrary"></param>
		/// <returns></returns>
		public IEnumerator CanReduceStatStage(Stats stat,IBattler attacker=null,bool showMessages=false,IBattleMove move=null,bool moldbreaker=false,bool ignoreContrary=false,System.Action<bool> result=null) {
			if (!moldbreaker)
				if (!attacker.IsNotNullOrNone() || attacker.Index==this.Index || !attacker.hasMoldBreaker())
					if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
					{ yield return CanIncreaseStatStage(stat,attacker,showMessages,move,moldbreaker,true); yield break; }
			if (isFainted()) { result?.Invoke(false); yield break; }
			bool selfreduce=(attacker.IsNotNullOrNone() && attacker.Index==this.Index);
			if (!selfreduce) {
				if (effects.Substitute>0 && (!move.IsNotNullOrNone() || !move.ignoresSubstitute(attacker))) {
					if (showMessages) yield return @battle.Display(Game._INTL("But it failed!"));
					result?.Invoke(false); yield break;
				}
				if (OwnSide.Mist>0 &&
				(!attacker.IsNotNullOrNone() || !attacker.hasWorkingAbility(Abilities.INFILTRATOR))) {
					if (showMessages) yield return @battle.Display(Game._INTL("{1} is protected by Mist!",ToString()));
					result?.Invoke(false); yield break;
				}
				string abilityname;
				if (!moldbreaker && (!attacker.IsNotNullOrNone() || !attacker.hasMoldBreaker())) {
					if (hasWorkingAbility(Abilities.CLEAR_BODY) || hasWorkingAbility(Abilities.WHITE_SMOKE)) {
						abilityname=Game._INTL(Ability.ToString(TextScripts.Name));
						if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents stat loss!",ToString(),abilityname));
						result?.Invoke(false); yield break;
					}
					if (HasType(Types.GRASS))
						if (hasWorkingAbility(Abilities.FLOWER_VEIL)) {
							abilityname=Game._INTL(Ability.ToString(TextScripts.Name));
							if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents stat loss!",ToString(),abilityname));
							result?.Invoke(false); yield break;
						} else if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL)) {
							abilityname=Game._INTL(Partner.Ability.ToString(TextScripts.Name));
							if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents {3}'s stat loss!",Partner.ToString(),abilityname,ToString(true)));
							result?.Invoke(false); yield break;
						}
					if (stat==Stats.ATTACK && hasWorkingAbility(Abilities.HYPER_CUTTER)) {
						abilityname=Game._INTL(Ability.ToString(TextScripts.Name));
						if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents Attack loss!",ToString(),abilityname));
						result?.Invoke(false); yield break;
					}
					if (stat==Stats.DEFENSE && hasWorkingAbility(Abilities.BIG_PECKS)) {
						abilityname=Game._INTL(Ability.ToString(TextScripts.Name));
						if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents Defense loss!",ToString(),abilityname));
						result?.Invoke(false); yield break;
					}
					if (stat==Stats.ACCURACY && hasWorkingAbility(Abilities.KEEN_EYE)) {
						abilityname=Game._INTL(Ability.ToString(TextScripts.Name));
						if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} prevents accuracy loss!",ToString(),abilityname));
						result?.Invoke(false); yield break;
					}
				}
			}
			if (TooLow(stat)) {
				if (showMessages) yield return @battle.Display(Game._INTL("{1}'s {2} won't go any lower!",
					ToString(),Game._INTL(stat.ToString(TextScripts.Name))));
				result?.Invoke(false); yield break;
			}
			result?.Invoke(true);
		}

		//public int ReduceStatBasic(Stats stat,int increment,IBattler attacker=null,bool moldbreaker=false,bool ignoreContrary=false) {
		//	if (!moldbreaker) // moldbreaker is true only when Roar forces out a Pokémon into Sticky Web
		//		if (!attacker.IsNotNullOrNone() || attacker.Index==this.Index || !attacker.hasMoldBreaker()) {
		//			if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
		//				return IncreaseStatBasic(stat,increment,attacker,moldbreaker,true);
		//			if (hasWorkingAbility(Abilities.SIMPLE)) increment*=2;
		//		}
		//	increment=Math.Min(increment,6+@stages[(int)stat]);
		//	GameDebug.Log($"[Stat change] #{ToString()}'s #{Game._INTL(stat.ToString(TextScripts.Name))} fell by #{increment} stage(s) (was #{@stages[(int)stat]}, now #{@stages[(int)stat]-increment})");
		//	@stages[(int)stat]-=increment;
		//	return increment;
		//}

		public IEnumerator ReduceStat(Stats stat,int increment,IBattler attacker,bool showMessages,IBattleMove move=null,bool downanim=true,bool moldbreaker=false,bool ignoreContrary=false,System.Action<bool> result=null) {
			if (!moldbreaker)
				if (!attacker.IsNotNullOrNone() || attacker.Index==this.Index || !attacker.hasMoldBreaker())
					if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
					{ yield return IncreaseStat(stat,increment,attacker,showMessages,move,downanim,moldbreaker,true, result: value => result?.Invoke(value)); yield break; }
			if (stat != Stats.ATTACK && stat != Stats.DEFENSE &&
				stat != Stats.SPATK && stat != Stats.SPDEF &&
				stat != Stats.SPEED && stat != Stats.EVASION &&
				stat != Stats.ACCURACY) { result?.Invoke(false); yield break; }
			bool canReduceStatStage = false;
			yield return CanReduceStatStage(stat, attacker, showMessages, move, moldbreaker, ignoreContrary, result: value => canReduceStatStage = value);
			if (canReduceStatStage) {
			//if (CanReduceStatStage(stat, attacker, showMessages, move, moldbreaker, ignoreContrary)) {
				increment=ReduceStatBasic(stat,increment,attacker,moldbreaker,ignoreContrary);
				if (increment > 0) {
					if (ignoreContrary)
						if (downanim) yield return @battle.Display(Game._INTL("{1}'s {2} activated!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name))));
					if (downanim) @battle.CommonAnimation("StatDown", this, null); 
					string[] arrStatTexts= new string[] {Game._INTL("{1}'s {2} fell!",ToString(),Game._INTL(stat.ToString(TextScripts.Name))),
						Game._INTL("{1}'s {2} harshly fell!",ToString(),Game._INTL(stat.ToString(TextScripts.Name))),
						Game._INTL("{1}'s {2} severely fell!",ToString(),Game._INTL(stat.ToString(TextScripts.Name)))};
					yield return @battle.Display(arrStatTexts[Math.Min(increment-1,2)]);
					// Defiant
					if (hasWorkingAbility(Abilities.DEFIANT) && (!attacker.IsNotNullOrNone() || attacker.IsOpposing(this.Index)))
						yield return IncreaseStatWithCause(Stats.ATTACK,2,this,Game._INTL(Ability.ToString(TextScripts.Name)));
					// Competitive
					if (hasWorkingAbility(Abilities.COMPETITIVE) && (!attacker.IsNotNullOrNone() || attacker.IsOpposing(this.Index)))
						yield return IncreaseStatWithCause(Stats.SPATK, 2, this, Game._INTL(Ability.ToString(TextScripts.Name)));
					result?.Invoke(true); yield break;
				}
			}
			result?.Invoke(false);
		}

		public IEnumerator ReduceStatWithCause(Stats stat,int increment,IBattler attacker,string cause,bool showanim=true,bool showmessage=true,bool moldbreaker=false,bool ignoreContrary=false,System.Action<bool> result=null) {
			if (!moldbreaker)
				if (!attacker.IsNotNullOrNone() || attacker.Index == this.Index || !attacker.hasMoldBreaker())
					if (hasWorkingAbility(Abilities.CONTRARY) && !ignoreContrary)
					{ yield return IncreaseStatWithCause(stat, increment, attacker, cause, showanim, showmessage, moldbreaker, true, result: value => result?.Invoke(value)); yield break; }
			if (stat != Stats.ATTACK && stat != Stats.DEFENSE &&
				stat != Stats.SPATK && stat != Stats.SPDEF &&
				stat != Stats.SPEED && stat != Stats.EVASION &&
				stat != Stats.ACCURACY) { result?.Invoke(false); yield break; }
			bool canReduceStatStage = false;
			yield return CanReduceStatStage(stat, attacker, false, null, moldbreaker, ignoreContrary, result: value => canReduceStatStage = value);
			if (canReduceStatStage) {
			//if (CanReduceStatStage(stat, attacker, false, null, moldbreaker, ignoreContrary)) {
				increment=ReduceStatBasic(stat,increment,attacker,moldbreaker,ignoreContrary);
				if (increment > 0) {
					if (ignoreContrary) //ToDo: DownAnimation? must be a typo for "ShowAnimation" -> showanim
						//if (downanim) yield return @battle.Display(Game._INTL("{1}'s {2} activated!",ToString(),Game._INTL(this.ability.ToString(TextScripts.Name)))); 
						if (showanim) yield return @battle.Display(Game._INTL("{1}'s {2} activated!",ToString(),Game._INTL(Ability.ToString(TextScripts.Name)))); 
					if (showanim) @battle.CommonAnimation("StatDown",this,null);
					string[] arrStatTexts = null;
					if (attacker.Index==this.Index)
						arrStatTexts=new string[] {Game._INTL("{1}'s {2} lowered its {3}!",ToString(),cause,Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} harshly lowered its {3}!",ToString(),cause,Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} severely lowered its {3}!",ToString(),Game._INTL(stat.ToString(TextScripts.Name)))};
					else
						arrStatTexts=new string[] {Game._INTL("{1}'s {2} lowered {3}'s {4}!",attacker.ToString(),cause,ToString(true),Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} harshly lowered {3}'s {4}!",attacker.ToString(),cause,ToString(true),Game._INTL(stat.ToString(TextScripts.Name))),
							Game._INTL("{1}'s {2} severely lowered {3}'s {4}!",attacker.ToString(),cause,ToString(true),Game._INTL(stat.ToString(TextScripts.Name)))};
					if (showmessage) yield return @battle.Display(arrStatTexts[Math.Min(increment-1,2)]); 
					// Defiant
					if (hasWorkingAbility(Abilities.DEFIANT) && (!attacker.IsNotNullOrNone() || attacker.IsOpposing(this.Index)))
						yield return IncreaseStatWithCause(Stats.ATTACK,2,this,Game._INTL(Ability.ToString(TextScripts.Name)));
					// Competitive
					if (hasWorkingAbility(Abilities.COMPETITIVE) && (!attacker.IsNotNullOrNone() || attacker.IsOpposing(this.Index)))
						yield return IncreaseStatWithCause(Stats.SPATK,2,this,Game._INTL(Ability.ToString(TextScripts.Name)));
						result?.Invoke(true); yield break;
				}
			}
			result?.Invoke(false);
		}

		public IEnumerator ReduceAttackStatIntimidate(IBattler opponent, System.Action<bool> result) {
			if (isFainted()) { result(false); yield break; }
			if (effects.Substitute>0) {
				yield return @battle.Display(Game._INTL("{1}'s substitute protected it from {2}'s {3}!",
					ToString(),opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
				result(false); yield break;
			}
			if (!opponent.hasWorkingAbility(Abilities.CONTRARY)) {
				if (OwnSide.Mist>0) {
					yield return @battle.Display(Game._INTL("{1} is protected from {2}'s {3} by Mist!",
						ToString(),opponent.ToString(true),Game._INTL(opponent.Ability.ToString(TextScripts.Name))));
					result(false); yield break;
				}
				if (hasWorkingAbility(Abilities.CLEAR_BODY) || hasWorkingAbility(Abilities.WHITE_SMOKE) ||
					hasWorkingAbility(Abilities.HYPER_CUTTER) ||
					(hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS))) {
					string abilityname=Game._INTL(Ability.ToString(TextScripts.Name));
					string oppabilityname=Game._INTL(opponent.Ability.ToString(TextScripts.Name));
					yield return @battle.Display(Game._INTL("{1}'s {2} prevented {3}'s {4} from working!",
						ToString(),abilityname,opponent.ToString(true),oppabilityname));
					result(false); yield break;
				}
				if (Partner.hasWorkingAbility(Abilities.FLOWER_VEIL) && HasType(Types.GRASS)) {
					string abilityname=Game._INTL(Partner.Ability.ToString(TextScripts.Name));
					string oppabilityname=Game._INTL(opponent.Ability.ToString(TextScripts.Name));
					yield return @battle.Display(Game._INTL("{1}'s {2} prevented {3}'s {4} from working!",
						Partner.ToString(),abilityname,opponent.ToString(true),oppabilityname));
					result(false); yield break;
				}
			}
			yield return ReduceStatWithCause(Stats.ATTACK,1,opponent,Game._INTL(opponent.Ability.ToString(TextScripts.Name)), result: value => result(value));
		}
		#endregion
	}

	public interface IBattlerEffectIE : PokemonEssentials.Interface.PokeBattle.IBattlerEffect
	{
		#region Sleep
		IEnumerator CanSleep(IBattler attacker, bool showMessages, IBattleMove move, bool ignorestatus, System.Action<bool> result);

		//bool CanSleepYawn();

		new IEnumerator Sleep(string msg = null);

		//void SleepSelf(int duration = -1);
		#endregion

		#region Poison
		IEnumerator CanPoison(IBattler attacker, bool showMessages, IBattleMove move = null, System.Action<bool> result = null);

		IEnumerator CanPoisonSynchronize(IBattler opponent, System.Action<bool> result);

		//bool CanPoisonSpikes(bool moldbreaker = false);

		new IEnumerator Poison(IBattler attacker, string msg = null, bool toxic = false);
		#endregion

		#region Burn
		IEnumerator CanBurn(IBattler attacker, bool showMessages, IBattleMove move = null, System.Action<bool> result = null);

		IEnumerator CanBurnSynchronize(IBattler opponent, System.Action<bool> result);

		new IEnumerator Burn(IBattler attacker, string msg = null);
		#endregion

		#region Paralyze
		IEnumerator CanParalyze(IBattler attacker, bool showMessages, IBattleMove move = null, System.Action<bool> result = null);

		IEnumerator CanParalyzeSynchronize(IBattler opponent, System.Action<bool> result);

		new IEnumerator Paralyze(IBattler attacker, string msg = null);
		#endregion

		#region Freeze
		IEnumerator CanFreeze(IBattler attacker, bool showMessages, IBattleMove move, System.Action<bool> result);

		new IEnumerator Freeze(string msg = null);
		#endregion

		#region Generalized status displays
		new IEnumerator ContinueStatus(bool showAnim = true);

		new IEnumerator CureStatus(bool showMessages = true);
		#endregion

		#region Confuse
		IEnumerator CanConfuse(IBattler attacker = null, bool showMessages = true, IBattleMove move = null, System.Action<bool> result = null);

		IEnumerator CanConfuseSelf(bool showMessages, System.Action<bool> result = null);

		//void Confuse();

		new IEnumerator ConfuseSelf();

		new IEnumerator ContinueConfusion();

		new IEnumerator CureConfusion(bool showMessages = true);
		#endregion

		#region Attraction
		IEnumerator CanAttract(IBattler attacker, bool showMessages = true, System.Action<bool> result = null);

		new IEnumerator Attract(IBattler attacker, string msg = null);

		new IEnumerator AnnounceAttract(IBattler seducer);

		new IEnumerator ContinueAttract();

		//void CureAttract();
		#endregion

		#region Flinching
		//bool Flinch(IBattler attacker);
		#endregion

		#region Increase stat stages
		//bool TooHigh(Stats stat);

		IEnumerator CanIncreaseStatStage(Stats stat, IBattler attacker = null, bool showMessages = false, IBattleMove move = null, bool moldbreaker = false, bool ignoreContrary = false, System.Action<bool> result = null);

		//int IncreaseStatBasic(Stats stat, int increment, IBattler attacker = null, bool moldbreaker = false, bool ignoreContrary = false);

		IEnumerator IncreaseStat(Stats stat, int increment, IBattler attacker, bool showMessages, IBattleMove move = null, bool upanim = true, bool moldbreaker = false, bool ignoreContrary = false, System.Action<bool> result = null);

		IEnumerator IncreaseStatWithCause(Stats stat, int increment, IBattler attacker, string cause, bool showanim = true, bool showmessage = true, bool moldbreaker = false, bool ignoreContrary = false, System.Action<bool> result = null);
		#endregion

		#region Decrease stat stages
		//bool TooLow(Stats stat);

		/// <summary>
		/// Tickle (04A) and Noble Roar (13A) can't use this, but replicate it instead.
		/// (Reason is they lowers more than 1 stat independently, and therefore could
		/// show certain messages twice which is undesirable.)
		/// </summary>
		/// <param name="stat"></param>
		/// <param name="attacker"></param>
		/// <param name="showMessages"></param>
		/// <param name="move"></param>
		/// <param name="moldbreaker"></param>
		/// <param name="ignoreContrary"></param>
		/// <returns></returns>
		IEnumerator CanReduceStatStage(Stats stat, IBattler attacker = null, bool showMessages = false, IBattleMove move = null, bool moldbreaker = false, bool ignoreContrary = false, System.Action<bool> result = null);

		//int ReduceStatBasic(Stats stat, int increment, IBattler attacker = null, bool moldbreaker = false, bool ignoreContrary = false);

		IEnumerator ReduceStat(Stats stat, int increment, IBattler attacker, bool showMessages, IBattleMove move = null, bool downanim = true, bool moldbreaker = false, bool ignoreContrary = false, System.Action<bool> result = null);

		IEnumerator ReduceStatWithCause(Stats stat, int increment, IBattler attacker, string cause, bool showanim = true, bool showmessage = true, bool moldbreaker = false, bool ignoreContrary = false, System.Action<bool> result = null);

		IEnumerator ReduceAttackStatIntimidate(IBattler opponent, System.Action<bool> result);
		#endregion
	}
}