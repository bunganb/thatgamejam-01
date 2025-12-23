using UnityEngine;
using System.Collections.Generic;
public class SequencePuzzleController : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public List<SequencePuzzleStep> puzzleSteps;
    private int currentStepIndex = 0;

    public void NotifyActived(SequencePuzzleStep step)
    {
        if (puzzleSteps[currentStepIndex] == step)
        {
            currentStepIndex++;
            Debug.Log("Correct step! Moved to step index: " + currentStepIndex);
            if (currentStepIndex >= puzzleSteps.Count)
            {
                PuzzleCompleted();
            }
        }
        else
        {
            Debug.Log("Incorrect step! Resetting puzzle.");
            ResetPuzzle();
        }
    }
    private void PuzzleCompleted()
    {
        Debug.Log("Puzzle Completed!");
        Destroy(this.gameObject);
        // Add additional logic for puzzle completion here
    }
    private void ResetPuzzle()
    {
        currentStepIndex = 0;
        foreach (var step in puzzleSteps)
        {
            step.ResetStep();
        }
    }
}
