public interface IPhaseRunner<PhaseEnum>
{
    IPhaseSet<PhaseEnum> PhaseSet { get; }
    IPhaseSolver<PhaseEnum> PhaseSolver { get; }
    PhaseEnum CurrentPhase { get; set; }
}