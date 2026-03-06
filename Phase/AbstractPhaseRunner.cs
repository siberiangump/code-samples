using UnityEngine;
using System.Collections;
using System;

public abstract class AbstractPhaseRunner<PhaseEnum> : MonoBehaviour where PhaseEnum : struct, IConvertible
{
    protected abstract IPhaseSet<PhaseEnum> PhaseSet { get; }
    protected abstract IPhaseSolver<PhaseEnum> PhaseSolver { get; }
    protected abstract PhaseEnum CurrentPhase { get; set; }

    Action OnEnd;

    public void Run(Action onEnd = null)
    {
        GotoPhase(CurrentPhase);
    }

    private void GotoPhase(PhaseEnum phaseEnum)
    {
        if (CheckEnd(phaseEnum)) 
            return;

        CurrentPhase = phaseEnum;
        Debug.Log($"GotoPhase : {CurrentPhase}");
        PhaseSet.GetPhase(phaseEnum).StartPhase(GotoNext);
    }

    private void GotoNext()
    {
        GotoPhase(PhaseSolver.GetNext(CurrentPhase));
    }

    private bool CheckEnd(PhaseEnum phaseEnum) 
    {
		bool result = isEndedPhase(phaseEnum);
        if(result)
			End();
        return result;
	}

    public void End() 
    {
        Action action = OnEnd;
		OnEnd = null;
        action?.Invoke();
	}

    public abstract bool isEndedPhase(PhaseEnum phase);
}
