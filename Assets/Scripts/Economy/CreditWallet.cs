using System;
using UnityEngine;

namespace BeyondTheBeat.Economy
{
    public sealed class CreditWallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int balance;

        public int Balance => balance;

        public event Action<CreditWallet, int, int> BalanceChanged;

        public bool AddCredits(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            int previous = balance;
            balance += amount;
            BalanceChanged?.Invoke(this, previous, balance);
            return true;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > balance)
            {
                return false;
            }

            int previous = balance;
            balance -= amount;
            BalanceChanged?.Invoke(this, previous, balance);
            return true;
        }

        public void SetBalance(int value)
        {
            int next = Mathf.Max(0, value);
            if (next == balance)
            {
                return;
            }

            int previous = balance;
            balance = next;
            BalanceChanged?.Invoke(this, previous, balance);
        }
    }
}
