using System;
using System.Collections;
using UnityEngine;

public class PlayerTurnPhase : MonoBehaviour, IPhase
{
	[SerializeField, AutoFill(Context.Scene)] UITurnPanel TurnUI;
	[SerializeField, AutoFill(Context.Scene, Marker.Hero)] UIParty UIHeroParty;
	[SerializeField, AutoFill(Context.Scene, Marker.Enemy)] UIParty UIEnemyParty;

	[SerializeField, AutoFill(Context.Scene)] HeroActionIntent HeroActionIntent;

	[SerializeField, AutoFill(Context.Scene)] FightState FightState;

	CallbackHolder EndTurnCallback = new();

	public void Init() 
	{
	}

	void IPhase.StartPhase(Action onComplete)
	{
		EndTurnCallback.SetCallback(onComplete);
		TurnUI.Activate(EndTurn);
		StartTurn();
		PlayerAction();
	}

	private void PlayerAction() 
	{
		StartCoroutine(OnHeroActionDone());
	}

	private IEnumerator OnHeroActionDone() 
	{
		yield return new WaitForEndOfFrame();
		HeroActionIntent.Action(AfterPlayerAction);
	}

	private void AfterPlayerAction() 
	{
		UIHeroParty.UpdateAP();
		PlayerAction();
	}

	private void StartTurn()
	{
		FightState.Player.ResetAP();
		UIEnemyParty.UpdateAP();
	}

	private void EndTurn()
	{
		HeroActionIntent.Abandon();
		EndTurnCallback.Invoke();
	}
}
