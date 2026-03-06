using UnityEngine;

public class FightPhaseSet : MonoBehaviour, IPhaseSet<FightPhaseEnum>
{
	[SerializeField] StartFightPhase StartFightPhase;
	[SerializeField] PlayerTurnPhase PlayerTurnPhase;
	[SerializeField] EnemyTurnPhase EnemyTurnPhase;
	[SerializeField] EndFightPhase EndFightPhase;

	IPhase IPhaseSet<FightPhaseEnum>.GetPhase(FightPhaseEnum currentPhase)
	{
		switch (currentPhase)
		{
			case FightPhaseEnum.StartFight: return StartFightPhase;
			case FightPhaseEnum.PlayerTurn: return PlayerTurnPhase;
			case FightPhaseEnum.EnemyTurn: return EnemyTurnPhase;
			case FightPhaseEnum.EndFight: return EndFightPhase;
			default: return null;
		}
	}
}