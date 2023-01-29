using InventorySystem;
using InventorySystem.Items;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;

namespace CrazyPills
{
    public class EventHandlers
    {
        private readonly Random _rand = new Random();
        
        [PluginEvent(PluginAPI.Enums.ServerEventType.PlayerUsedItem)]
        internal void OnItemUsed(Player plr, ItemBase item)
        {
            if (item.ItemTypeId != ItemType.Painkillers) return;
        
            List<Handlers.PillEffectType> pillTypes = Handlers.ActiveEnums();
            int num = _rand.Next(pillTypes.Count);
        
            Handlers.RunPillEffect(pillTypes[num], plr);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnChangeRole(Player plr, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            // if (!plr.IsHuman && CrazyPills.Instance.Config.SpawnWithPills) return;
            
            plr.ReferenceHub.inventory.ServerAddItem(ItemType.Painkillers);
        }
    }
}