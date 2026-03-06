using UnityEngine;

public class FightPhaseSolver : MonoBehaviour, IPhaseSolver<FightPhaseEnum>
{
	private FightState ActiveFightState => Services.Fight.State;

    FightPhaseEnum IPhaseSolver<FightPhaseEnum>.GetNext(FightPhaseEnum phase)
	{
		switch (phase)
		{
			case FightPhaseEnum.StartFight: return ActiveFightState.Turn == TurnState.Player ? FightPhaseEnum.PlayerTurn : FightPhaseEnum.EnemyTurn;
			case FightPhaseEnum.PlayerTurn: return FightPhaseEnum.EnemyTurn;
			case FightPhaseEnum.EnemyTurn: return FightPhaseEnum.PlayerTurn;
			case FightPhaseEnum.EndFight: return FightPhaseEnum.None;
			default: return FightPhaseEnum.None;
		}
	}
}
