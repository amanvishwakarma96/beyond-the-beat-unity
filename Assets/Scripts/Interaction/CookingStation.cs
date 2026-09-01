using System;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(InteractionTrigger))]
    public sealed class CookingStation : TimedActivityInteractable
    {
        [Header("Cooking")]
        [SerializeField] private string recipeId = "camp-meal";
        [SerializeField] private string activityLabel = "Cook meal";

        private int mealsPrepared;

        public string RecipeId => recipeId;
        public string ActivityLabel => activityLabel;
        public int MealsPrepared => mealsPrepared;

        public event Action<CookingStation, GameObject, int> MealPrepared;

        protected override void OnActivityCompleted(GameObject actor)
        {
            mealsPrepared++;
            MealPrepared?.Invoke(this, actor, mealsPrepared);
        }
    }
}
