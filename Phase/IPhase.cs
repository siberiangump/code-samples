using System;

public interface IPhase
{
    void StartPhase(Action onComplete);
}
