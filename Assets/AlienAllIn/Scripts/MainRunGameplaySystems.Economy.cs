using System;
using UnityEngine;

namespace AlienAllIn
{
    public sealed partial class MainRunGameplaySystems
    {
        readonly int[] itemInventory = new int[3];
        [SerializeField] int chips;
        public int Chips => chips;
        public float AttackUpgradeAmount => damageUpgradeAmount;

        public bool TryExchangeChipsForCoins(int amount)
            => TryExchangeCurrency(amount, true);

        public bool TryExchangeCoinsForChips(int amount)
            => TryExchangeCurrency(amount, false);

        bool TryExchangeCurrency(int amount, bool chipsToCoins)
        {
            if (!runLoop || (runLoop.State != RunState.Placement && runLoop.State != RunState.Running && runLoop.State != RunState.Paused)) return false;
            int source = chipsToCoins ? chips : coins;
            int destination = chipsToCoins ? coins : chips;
            if (amount <= 0 || amount > source || destination > int.MaxValue - amount) return false;
            if (chipsToCoins) { chips -= amount; coins += amount; }
            else { coins -= amount; chips += amount; }
            string from = chipsToCoins ? "Chips" : "Coins";
            string to = chipsToCoins ? "Coins" : "Chips";
            SetStatusFromUI($"Exchanged {amount} {from} for {amount} {to}.");
            return true;
        }

        void ResetEconomyForNewRun()
        {
            Array.Clear(itemInventory, 0, itemInventory.Length);
            chips = 0;
        }

        public int GetItemCount(FieldItemKind kind) => itemInventory[ItemIndex(kind)];

        public int GetAttackUpgradeCost(GridActor actor)
        {
            if (!actor || actor.kind == ActorKind.Duck || actor.kind == ActorKind.Enemy) return int.MaxValue;
            int rank = actor.DamageUpgradeRank;
            if (rank >= 30) return int.MaxValue;
            long cost = (long)Mathf.Max(1, damageUpgradeBaseCost) << rank;
            return cost >= int.MaxValue ? int.MaxValue : (int)cost;
        }

        public bool TryBuyAttackUpgrade(GridActor actor)
        {
            if (!runLoop || runLoop.State == RunState.RunComplete || runLoop.State == RunState.Defeated || !actor || !actor.Alive) return false;
            int cost = GetAttackUpgradeCost(actor);
            if (cost == int.MaxValue) return false;
            if (coins < cost) { SetStatusFromUI("Not enough Coins."); return false; }
            coins -= cost;
            actor.AddDamageUpgrade(damageUpgradeAmount);
            SetStatusFromUI(actor.kind + " ATK upgraded.");
            return true;
        }

        public GridActor GetActorForUI(ActorKind kind) => GetActor(kind);
        public void SetStatusFromUI(string value) { statusMessage = value; RunDataChanged?.Invoke(); }

        void AddItem(FieldItemKind kind, int quantity)
        {
            int index = ItemIndex(kind);
            itemInventory[index] += Mathf.Max(1, quantity);
        }

        public bool TryUseItem(FieldItemKind item, GridActor target)
        {
            if (!runLoop || runLoop.State != RunState.Running || !target || !target.Alive || itemInventory[ItemIndex(item)] <= 0) return false;
            ActorKind required = item switch
            {
                FieldItemKind.AirPump => ActorKind.Pufferfish,
                FieldItemKind.LaserGun => ActorKind.Cat,
                _ => ActorKind.Seal
            };
            if (target.kind != required || !target.UltimateFull) return false;
            if (!ActivateUltimate(target)) return false;
            itemInventory[ItemIndex(item)]--;
            statusMessage = $"{item} used by {target.kind}.";
            RunDataChanged?.Invoke();
            return true;
        }

        public bool TryActivateUltimateSwitch(GridActor first, GridActor second)
        {
            if (!runLoop || runLoop.State != RunState.Running || !first || !second || first == second || first.kind == ActorKind.Duck || second.kind == ActorKind.Duck ||
                !first.Alive || !second.Alive || !first.UltimateFull || !second.UltimateFull) return false;
            Vector3 position = first.transform.position;
            first.transform.position = second.transform.position;
            second.transform.position = position;
            first.RefreshCell(); second.RefreshCell();
            first.SpendCharge(); second.SpendCharge();
            bool firstStarted = ActivateUltimateAfterChargeSpent(first);
            bool secondStarted = ActivateUltimateAfterChargeSpent(second);
            RefreshBoardOccupancy();
            statusMessage = firstStarted && secondStarted ? "Ultimate Switch: both Ultimates activated." : "Ultimate Switch failed.";
            RunDataChanged?.Invoke();
            return firstStarted && secondStarted;
        }

        bool ActivateUltimateAfterChargeSpent(GridActor actor)
        {
            // Restore one charge just long enough for the shared activation path to consume it.
            actor.AddUltimateCharge(1f);
            return ActivateUltimate(actor);
        }

        void AddCash(int amount, string message)
        {
            coins = Mathf.Max(0, coins + amount);
            statusMessage = message;
            RunDataChanged?.Invoke();
        }

        void AddScore(int amount, string message)
        {
            score = Mathf.Max(0, score + amount);
            statusMessage = $"{message} +{amount} Score";
            RunDataChanged?.Invoke();
        }

        static int ItemIndex(FieldItemKind kind) => (int)kind;
    }
}
