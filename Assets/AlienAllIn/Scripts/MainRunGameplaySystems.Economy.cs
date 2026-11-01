using System;
using UnityEngine;

namespace AlienAllIn
{
    public sealed partial class MainRunGameplaySystems
    {
        [Header("Prototype upgrade/item rules")]
        [SerializeField] bool shopOpenOnlyWhileRunning = true;

        readonly int[] itemInventory = new int[3];
        readonly int[] chipInventory = new int[3];
        readonly int[] chipStock = new int[3];
        float shopRestockClock;

        public int ShopRestockSecondsRemaining => Mathf.CeilToInt(Mathf.Max(0f, shopRestockClock));
        public int ChipStockCapacity => Mathf.Max(0, chipStockCapacity);

        void ResetEconomyForNewRun()
        {
            Array.Clear(itemInventory, 0, itemInventory.Length);
            Array.Clear(chipInventory, 0, chipInventory.Length);
            for (int i = 0; i < chipStock.Length; i++) chipStock[i] = Mathf.Max(0, chipStockCapacity);
            shopRestockClock = Mathf.Max(.1f, shopRestockSeconds);
        }

        void AdvanceEconomy(float dt)
        {
            if (dt <= 0f) return;
            shopRestockClock -= dt;
            if (shopRestockClock > 0f) return;
            shopRestockClock = Mathf.Max(.1f, shopRestockSeconds);
            for (int i = 0; i < chipStock.Length; i++) chipStock[i] = Mathf.Max(0, chipStockCapacity);
            statusMessage = "Chip shop stock replenished.";
            RunDataChanged?.Invoke();
        }

        public void ToggleShop()
        {
            if (!runLoop || (shopOpenOnlyWhileRunning && runLoop.State != RunState.Running))
            { statusMessage = "Shop opens while the run is active."; RunDataChanged?.Invoke(); return; }
            ShopOpen = !ShopOpen;
            statusMessage = ShopOpen ? "Chip shop opened. Buying does not pause the run." : "Chip shop closed.";
            RunDataChanged?.Invoke();
        }

        public int GetItemCount(FieldItemKind kind) => itemInventory[ItemIndex(kind)];
        public int GetChipCount(ActorKind kind) { int index = ChipIndex(kind); return index < 0 ? 0 : chipInventory[index]; }
        public int GetChipStock(ActorKind kind) { int index = ChipIndex(kind); return index < 0 ? 0 : chipStock[index]; }
        public int GetChipPrice(ActorKind kind) => kind == ActorKind.Cat ? catChipPrice : kind == ActorKind.Pufferfish ? pufferfishChipPrice : sealChipPrice;

        public bool TryBuyChip(ActorKind kind)
        {
            int index = ChipIndex(kind);
            if (!runLoop || runLoop.State != RunState.Running || !ShopOpen || index < 0 || chipStock[index] <= 0) return false;
            int price = Mathf.Max(0, GetChipPrice(kind));
            if (cash < price) { statusMessage = "Not enough Cash for this Chip."; RunDataChanged?.Invoke(); return false; }
            cash -= price;
            chipStock[index]--;
            chipInventory[index]++;
            statusMessage = $"Bought a {kind} Chip. Drag it onto {kind}.";
            RunDataChanged?.Invoke();
            return true;
        }

        public bool TryApplyChip(ActorKind chipKind, GridActor target)
        {
            int index = ChipIndex(chipKind);
            if (!runLoop || runLoop.State != RunState.Running || index < 0 || !target || target.kind != chipKind || !target.Alive || chipInventory[index] <= 0) return false;
            target.AddChipDamage(chipDamageBonus);
            chipInventory[index]--;
            statusMessage = $"{chipKind} Chip applied (+{chipDamageBonus} Damage).";
            RunDataChanged?.Invoke();
            return true;
        }

        public int GetUpgradeCost(GridActor actor, UpgradeKind kind)
        {
            if (!actor) return int.MaxValue;
            int rank = kind switch
            {
                UpgradeKind.Damage => actor.DamageUpgradeRank,
                UpgradeKind.CastSpeed => actor.CastSpeedUpgradeRank,
                _ => actor.HealthUpgradeRank
            };
            if (rank >= maxUpgradeRank) return int.MaxValue;
            int basis = kind switch
            {
                UpgradeKind.Damage => damageUpgradeBaseCost,
                UpgradeKind.CastSpeed => castSpeedUpgradeBaseCost,
                _ => healthUpgradeBaseCost
            };
            int step = kind switch
            {
                UpgradeKind.Damage => damageUpgradeCostStep,
                UpgradeKind.CastSpeed => castSpeedUpgradeCostStep,
                _ => healthUpgradeCostStep
            };
            return Mathf.Max(0, basis + step * rank);
        }

        public bool TryBuyUpgrade(GridActor actor, UpgradeKind kind)
        {
            if (!runLoop || runLoop.State != RunState.Running || !actor || !actor.Alive) return false;
            if (actor.kind == ActorKind.Duck && kind != UpgradeKind.Health)
            { statusMessage = "Duck has no attack to upgrade in this prototype."; RunDataChanged?.Invoke(); return false; }
            int cost = GetUpgradeCost(actor, kind);
            if (cost == int.MaxValue) { statusMessage = "Upgrade rank is at the prototype cap."; return false; }
            if (cash < cost) { statusMessage = "Not enough Cash for this upgrade."; RunDataChanged?.Invoke(); return false; }
            cash -= cost;
            switch (kind)
            {
                case UpgradeKind.Damage: actor.AddDamageUpgrade(damageUpgradeAmount); break;
                case UpgradeKind.CastSpeed: actor.AddCastSpeedUpgrade(castSpeedUpgradeMultiplier); break;
                default: actor.AddMaxHealthUpgrade(healthUpgradeAmount); break;
            }
            statusMessage = $"{actor.kind} {kind} upgraded.";
            RunDataChanged?.Invoke();
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

        public bool TryMoveActor(GridActor actor, Vector2Int destination)
        {
            if (!runLoop || runLoop.State != RunState.Running || !actor || !actor.Alive || !actor.IsOnBoard) return false;
            if (!stage.IsInActiveArea(destination) || IsCellReserved(destination)) return false;
            if (!actor.CanPickUp) return false;
            if (actor.kind == ActorKind.Pufferfish && !pufferfish.IsUltimateActive && pufferFloatClock <= 0f) return false;
            Vector3 target = board.CellCenter(destination);
            if (actor.kind == ActorKind.Duck)
            {
                actor.transform.position = target;
                actor.RefreshCell();
                if (IsBehindLivingAlien(target, actor)) actor.State = ActorState.Hiding;
                else actor.State = ActorState.Panic;
                RefreshBoardOccupancy();
                statusMessage = actor.State == ActorState.Hiding ? "Duck is hiding behind an Alien." : "Duck moved; place it behind an Alien to hide.";
                RunDataChanged?.Invoke();
                return true;
            }

            actor.SpendCharge();
            actor.transform.position = target;
            actor.RefreshCell();
            RefreshBoardOccupancy();
            statusMessage = $"{actor.kind} changed position (Ultimate gauge spent).";
            RunDataChanged?.Invoke();
            return true;
        }

        public bool TryUltimateSwitchAt(GridActor actor, Vector2Int destination)
        {
            if (!runLoop || runLoop.State != RunState.Running || !actor || !actor.UltimateFull || actor.kind == ActorKind.Duck) return false;
            BoardCell cell = board.GetCell(destination);
            if (!cell) return false;
            foreach (GridActor occupant in cell.Occupants)
                if (occupant && occupant != actor && occupant.Alive && occupant.kind != ActorKind.Duck && occupant.UltimateFull)
                    return TryActivateUltimateSwitch(actor, occupant);
            return false;
        }

        void AddCash(int amount, string message)
        {
            cash = Mathf.Max(0, cash + amount);
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
        static int ChipIndex(ActorKind kind) => kind == ActorKind.Seal ? 0 : kind == ActorKind.Cat ? 1 : kind == ActorKind.Pufferfish ? 2 : -1;
    }
}
