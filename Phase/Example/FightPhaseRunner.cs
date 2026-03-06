using UnityEngine;

public class FightPhaseRunner : AbstractPhaseRunner<FightPhaseEnum>
{
	[SerializeField] FightPhaseSet FightPhaseSet;
	[SerializeField] FightPhaseSolver FightPhaseSolver;
	[SerializeField] FightPhaseEnum FightPhaseEnum;

	protected override IPhaseSet<FightPhaseEnum> PhaseSet => FightPhaseSet;
	protected override IPhaseSolver<FightPhaseEnum> PhaseSolver => FightPhaseSolver;
	protected override FightPhaseEnum CurrentPhase { get => FightPhaseEnum; set => FightPhaseEnum = value; }
	public override bool isEndedPhase(FightPhaseEnum phase) => phase == FightPhaseEnum.None;
}
