using UnityEngine;

namespace BeyondTheBeat.Puzzles
{
    [DisallowMultipleComponent]
    public sealed class PuzzleGateBinding : MonoBehaviour
    {
        [SerializeField] private PuzzleStateController puzzleState;
        [SerializeField] private RestrictedGateController gate;
        [SerializeField] private bool relockWhenPuzzleResets = true;

        private bool subscribed;

        public PuzzleStateController PuzzleState => puzzleState;
        public RestrictedGateController Gate => gate;
        public bool RelockWhenPuzzleResets => relockWhenPuzzleResets;

        private void OnEnable()
        {
            Rebind();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Rebind()
        {
            Unsubscribe();
            if (puzzleState != null)
            {
                puzzleState.StateChanged += HandlePuzzleStateChanged;
                subscribed = true;
            }

            Synchronize();
        }

        public void Synchronize()
        {
            if (puzzleState == null || gate == null)
            {
                return;
            }

            if (puzzleState.IsSolved)
            {
                gate.SetLocked(false);
            }
            else if (relockWhenPuzzleResets)
            {
                gate.SetLocked(true);
            }
        }

        private void HandlePuzzleStateChanged(bool solved)
        {
            if (gate == null)
            {
                return;
            }

            if (solved)
            {
                gate.SetLocked(false);
            }
            else if (relockWhenPuzzleResets)
            {
                gate.SetLocked(true);
            }
        }

        private void Unsubscribe()
        {
            if (!subscribed || puzzleState == null)
            {
                subscribed = false;
                return;
            }

            puzzleState.StateChanged -= HandlePuzzleStateChanged;
            subscribed = false;
        }
    }
}
