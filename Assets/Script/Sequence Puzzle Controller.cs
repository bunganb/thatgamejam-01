using UnityEngine;
using System.Collections.Generic;

public class SequencePuzzleController : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public List<int> correctSequence = new List<int>();

    private int currentIndex = 0;
    private bool isSolved = false;

    private void Start()
    {
        ResetPuzzle();
    }

    public void ReceiveInput(int inputID)
    {
        if (isSolved)
            return;

        if (correctSequence == null || correctSequence.Count == 0)
        {
            Debug.LogWarning("Correct sequence is empty");
            return;
        }

        Debug.Log($"Input: {inputID}, Expect: {correctSequence[currentIndex]}");

        if (inputID == correctSequence[currentIndex])
        {
            currentIndex++;

            if (currentIndex >= correctSequence.Count)
            {
                PuzzleCompleted();
            }
        }
        else
        {
            Debug.Log("Wrong input, reset puzzle");
            ResetPuzzle();
        }
    }

    private void PuzzleCompleted()
    {
        isSolved = true;
        Debug.Log("Puzzle Completed!");
        this.gameObject.SetActive(false);
        // Contoh: buka pintu
        // door.Open();
    }

    private void ResetPuzzle()
    {
        currentIndex = 0;
    }

    public bool IsSolved => isSolved;
}
