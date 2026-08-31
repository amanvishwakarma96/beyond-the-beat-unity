using System;
using UnityEngine;

namespace BeyondTheBeat.Puzzles
{
    [DisallowMultipleComponent]
    public sealed class PuzzleStateController : MonoBehaviour
    {
        [SerializeField] private string puzzleId = "puzzle";
        [SerializeField] private bool solvedOnStart;

        private bool initialized;
        private bool isSolved;

        public string PuzzleId => puzzleId;
        public bool IsSolved => isSolved;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(puzzleId);

        public event Action<bool> StateChanged;
        public event Action Solved;
        public event Action Reset;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
        }

        public bool SetSolved(bool solved)
        {
            InitializeIfNeeded();
            if (isSolved == solved)
            {
                return false;
            }

            isSolved = solved;
            StateChanged?.Invoke(isSolved);
            if (isSolved)
            {
                Solved?.Invoke();
            }
            else
            {
                Reset?.Invoke();
            }

            return true;
        }

        public bool ResetPuzzle()
        {
            return SetSolved(false);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            isSolved = solvedOnStart;
            initialized = true;
        }
    }
}
