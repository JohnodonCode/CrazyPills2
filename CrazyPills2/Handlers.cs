using System;
using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEngine;
using CrazyPills.Translations;
using InventorySystem.Items;
using InventorySystem;
using Random = System.Random;
using PluginAPI.Core;
using PlayerRoles;
using PluginAPI.Core.Items;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Pickups;
using Footprinting;
using Mirror;

namespace CrazyPills
{
    public static class Handlers
    {
        private static readonly Random Rand = new Random();
        private static readonly Config Configs = CrazyPills.Instance.Config;
        private static readonly Translation translation = CrazyPills.Instance.Translation;
        private static readonly Hint HintText = translation.Hints;

        public enum PillEffectType
        {
            Kill,
            Zombify,
            FullHeal,
            GiveGun,
            Goto,
            Combustion,
            Shrink,
            Balls,
            Invincibility,
            Bring,
            FullPK,
            WarheadEvent,
            SwitchDead,
            Promote,
            Switch
        }

        public static List<PillEffectType> ActiveEnums() => Enum.GetValues(typeof(PillEffectType)).Cast<PillEffectType>().Where(e => CrazyPills.Instance.Config.PillEffects.Contains(e)).ToList();

        public static void RunPillEffect(PillEffectType type, Player p)
        {
            switch (type)
            {
                case PillEffectType.Kill:
                    Kill(p);
                    break;
                case PillEffectType.Zombify:
                    Zombify(p);
                    break;
                case PillEffectType.FullHeal:
                    FullHeal(p);
                    break;
                case PillEffectType.GiveGun:
                    GiveGun(p);
                    break;
                case PillEffectType.Goto:
                    Goto(p);
                    break;
                case PillEffectType.Combustion:
                    Combustion(p);
                    break;
                case PillEffectType.Shrink:
                    Shrink(p);
                    break;
                case PillEffectType.Balls:
                    Balls(p);
                    break;
                case PillEffectType.Invincibility:
                    Invincibility(p);
                    break;
                case PillEffectType.Bring:
                    Bring(p);
                    break;
                case PillEffectType.FullPK:
                    FullPK(p);
                    break;
                case PillEffectType.WarheadEvent:
                    WarheadEvent(p);
                    break;
                case PillEffectType.SwitchDead:
                    SwitchDead(p);
                    break;
                case PillEffectType.Promote:
                    Promote(p);
                    break;
                case PillEffectType.Switch:
                    Switch(p);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static void Kill(Player p)
        {
            if (Configs.ShowHints)
                p.ReceiveHint(HintText.DeathEvent, 8f);
            Timing.CallDelayed(6f, () => p.Kill("Cyanide Pill"));
        }

        private static void Zombify(Player p)
        {
            RoleTypeId preRole = p.Role;
            Vector3 prePosition = p.Position;
            p.DropEverything();
            p.Role = RoleTypeId.Scp0492;
            Timing.CallDelayed(0.4f, () => p.Position = prePosition);
            Timing.CallDelayed(30f, () =>
            {
                if (!p.IsAlive) return;
                prePosition = p.Position;
                p.Role = preRole;
                Timing.CallDelayed(0.2f, () =>
                {
                    p.ClearInventory();
                    p.Position = prePosition;
                });
            });
        }

        private static void FullHeal(Player p)
        {
            p.Health = p.MaxHealth;
            p.ArtificialHealth = 200;
        }

        private static bool HasItem(Player p, ItemType itemtype) => (p.ReferenceHub.inventory.UserInventory.Items.Values.Any(item => item.ItemTypeId == itemtype));

        private static void GiveGun(Player p)
        {
            if (Configs.ShowHints)
                p.ReceiveHint(HintText.GunAndAmmo, 8f);

            if (!HasItem(p, ItemType.GunCOM18))
            {
                p.AddItem(ItemType.GunCOM18);
                p.AddAmmo(ItemType.Ammo9x19, 100);
                return;
            }

            if (!HasItem(p, ItemType.GunE11SR))
            {
                p.AddItem(ItemType.GunE11SR);
                p.AddAmmo(ItemType.Ammo762x39, 100);
                return;
            }

            p.AddItem(ItemType.GunLogicer);
            p.AddAmmo(ItemType.Ammo762x39, 100);
        }

        private static void Goto(Player p)
        {
            List<Player> alive = Player.GetPlayers().Where(x => x.Team != Team.Dead && x != p && x.Role != RoleTypeId.Scp079 && (x.Team != Team.SCPs || Configs.AllowSCPTeleportation)).ToList();

            if (alive.Count == 0) return;

            p.Position = alive[Rand.Next(alive.Count)].Position;
        }
        public static ThrowableItem CreateThrowable(ItemType type, Player player = null) => (player != null ? player.ReferenceHub : ReferenceHub.HostHub)
            .inventory.CreateItemInstance(new ItemIdentifier(type, ItemSerialGenerator.GenerateNext()), false) as ThrowableItem;
        public static void SpawnActive(this ThrowableItem item, Vector3 position, float fuseTime = -1f,
            Player owner = null)
        {
            TimeGrenade grenade = (TimeGrenade)UnityEngine.Object.Instantiate(item.Projectile, position, Quaternion.identity);
            if (fuseTime >= 0)
                grenade._fuseTime = fuseTime;
            grenade.NetworkInfo = new PickupSyncInfo(item.ItemTypeId, position, Quaternion.identity, item.Weight, item.ItemSerial);
            grenade.PreviousOwner = new Footprint(owner != null ? owner.ReferenceHub : ReferenceHub.HostHub);
            if (grenade is Scp018Projectile scp018)
                scp018.RigidBody.velocity = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value); // add some force to make the ball bounce
            NetworkServer.Spawn(grenade.gameObject);
            grenade.ServerActivate();
        }
        private static void Combustion(Player p)
        {
            CreateThrowable(ItemType.SCP018, p).SpawnActive(p.Position, 0.1f);
        }

        
        public static void SetScale(this Player target, Vector3 scale)
        {
            GameObject go = target.GameObject;
            if (go.transform.localScale == scale)
                return;
            try
            {
                NetworkIdentity identity = target.ReferenceHub.networkIdentity;
                go.transform.localScale = scale;
                foreach (Player player in Player.GetPlayers())
                {
                    NetworkServer.SendSpawnMessage(identity, player.Connection);
                }
            }
            catch (Exception e)
            {
                Log.Info($"Set Scale error: {e}");
            }
        }
        private static void Shrink(Player p)
        {
            p.SetScale(new Vector3 { x = 0.4f, y = 0.4f, z = 0.4f });
            Timing.CallDelayed(30f, () => p.SetScale(new Vector3 { x = 1f, y = 1f, z = 1f }));
        }

        private static void Balls(Player p)
        {
            CreateThrowable(ItemType.SCP018, p).SpawnActive(p.Position);
            CreateThrowable(ItemType.SCP018, p).SpawnActive(p.Position);
            CreateThrowable(ItemType.SCP018, p).SpawnActive(p.Position);
            CreateThrowable(ItemType.SCP018, p).SpawnActive(p.Position);
        }

        private static void Invincibility(Player p)
        {
            if (Configs.ShowHints)
                p.ReceiveHint(HintText.Invincibility, 8f);
            p.IsGodModeEnabled = true;
            Timing.CallDelayed(20f, () => p.IsGodModeEnabled = false);

        }

        private static void Bring(Player p)
        {
            List<Player> alive = Player.GetPlayers().Where(x => x.Team != Team.Dead && x != p && x.Role != RoleTypeId.Scp079 && (x.Team != Team.SCPs || Configs.AllowSCPTeleportation)).ToList();
            if (alive.Count == 0)
                return;
            Player randAlive = alive[Rand.Next(alive.Count)];
            randAlive.Position = p.Position;
        }

        private static void FullPK(Player p)
        {
            p.DropEverything();
            p.ClearInventory();
            for (byte i = 0; i < 8; i++)
                p.AddItem(ItemType.Painkillers);
        }
        public static void Broadcast(ushort duration, string message, global::Broadcast.BroadcastFlags type = global::Broadcast.BroadcastFlags.Normal, bool shouldClearPrevious = false)
        {
            if (shouldClearPrevious)
            {
                ClearBroadcasts();
            }

            Server.Broadcast.RpcAddElement(message, duration, type);
        }
        public static void ClearBroadcasts()
        {
            Server.Broadcast.RpcClearElements();
        }
        private static void WarheadEvent(Player p)
        {
            if (Configs.WarheadStartStopChance > 0 && Configs.WarheadStartStopChance < 100 && Rand.Next(101) > Configs.WarheadStartStopChance)
            {
                Warhead.LeverStatus = !Warhead.LeverStatus;
                if (Configs.ShowHints)
                    p.ReceiveHint(string.Format(HintText.LeverSwitch.Replace("{LeverStatus}", Warhead.LeverStatus.ToString().ToLower()), 8f));
                return;
            }

            if (!(Configs.WarheadStartStopChance > 0 && Configs.WarheadStartStopChance < 100))
            {
                Log.Warning("Config value 'WarheadStartStopChance' must be greater than 0 and less than 100.");
                return;
            }
            if (Warhead.IsDetonationInProgress)
            {
                
                Broadcast(8, translation.Broadcasts.WarHeadDisabled);
                Warhead.Stop();
                return;
            }

            Warhead.Start();
            Broadcast(8, translation.Broadcasts.WarHeadEnabled);
        }

        private static void SwitchDead(Player p)
        {
            List<Player> dead = Player.GetPlayers().Where(x=> x.Team == Team.Dead).ToList();       
            Vector3 prePosition;

            if (dead.Any())
            {
                Player randDead = dead[Rand.Next(dead.Count)];
                List<ItemBase> preInv = new List<ItemBase>();

                preInv.AddRange(p.ReferenceHub.inventory.UserInventory.Items.Values);
                prePosition = p.Position;
                RoleTypeId preRole = p.Role;
                p.Role = RoleTypeId.Spectator;
                randDead.Role = preRole;
                Timing.CallDelayed(0.2f, () =>
                {
                    randDead.Position = prePosition;
                    foreach (var item in preInv)
                        randDead.AddItem(item.ItemTypeId);
                });
                if (!Configs.ShowHints) return;
                p.ReceiveHint(HintText.Replacement["Replaced"], 8f);
                randDead.ReceiveHint(HintText.Replacement["Replacer"], 8f);
                return;
            }

            List<Player> alive = Player.GetPlayers().Where(x => x.Team != Team.Dead && x != p && x.Role != RoleTypeId.Scp079 && (x.Team != Team.SCPs || Configs.AllowSCPTeleportation)).ToList();
            if (alive.Count == 0)
                return;
            Player randAlive = alive[Rand.Next(alive.Count)];
            prePosition = p.Position;
            p.Position = randAlive.Position;
            randAlive.Position = prePosition;

            if (!Configs.ShowHints) return;
            p.ReceiveHint(HintText.Switching, 8f);
            randAlive.ReceiveHint(HintText.Switching, 8f);
        }

        private static void Promote(Player p)
        {
            Vector3 prePosition = p.Position;
            switch (p.Role)
            {
                case RoleTypeId.ClassD:
                    p.DropEverything();
                    p.Role = RoleTypeId.ChaosConscript;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition);
                    break;
                case RoleTypeId.ChaosConscript:
                    p.DropEverything();
                    p.Role = RoleTypeId.ChaosRifleman;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition);
                    break;
                case RoleTypeId.ChaosRifleman:
                    p.DropEverything();
                    p.Role = RoleTypeId.ChaosRepressor;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition);
                    break;
                case RoleTypeId.ChaosRepressor:
                    p.DropEverything();
                    p.Role = RoleTypeId.ChaosMarauder;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition);
                    break;
                case RoleTypeId.Scientist:
                    p.DropEverything();
                    p.Role = RoleTypeId.NtfSpecialist;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition); break;
                case RoleTypeId.FacilityGuard:
                    p.DropEverything();
                    p.Role = RoleTypeId.NtfPrivate;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition); break;
                case RoleTypeId.NtfPrivate:
                    p.DropEverything();
                    p.Role = RoleTypeId.NtfSergeant;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition); break;
                case RoleTypeId.NtfSergeant:
                    p.DropEverything();
                    p.Role = RoleTypeId.NtfCaptain;
                    Timing.CallDelayed(0.4f, () => p.Position = prePosition); break;
                default:
                    p.DropEverything();
                    p.AddItem(ItemType.KeycardO5); break;
            }

            if (Configs.ShowHints)
                p.ReceiveHint(HintText.Promotion, 8f);
        }

        private static void Switch(Player p)
        {
            List<Player> alive = Player.GetPlayers().Where(x => x.Team != Team.Dead && x != p && x.Role != RoleTypeId.Scp079 && (x.Team != Team.SCPs || Configs.AllowSCPTeleportation)).ToList();
            if (alive.Count == 0)
                return;
            Player randAlive = alive[Rand.Next(alive.Count)];
            (p.Position, randAlive.Position) = (randAlive.Position, p.Position);

            if (!Configs.ShowHints) return;
            p.ReceiveHint(HintText.Switching, 8f);
            randAlive.ReceiveHint(HintText.Switching, 8f);
        }
    }
}
