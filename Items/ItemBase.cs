﻿using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Thalassophobia.Items
{

    // The directly below is entirely from TILER2 API (by ThinkInvis) specifically the Item module. Utilized to implement instancing for classes.
    // TILER2 API can be found at the following places:
    // https://github.com/ThinkInvis/RoR2-TILER2
    // https://thunderstore.io/package/ThinkInvis/TILER2/

    public abstract class ItemBase<T> : ItemBase where T : ItemBase<T>
    {
        //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
        public static T instance { get; private set; }

        public ItemBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBase was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class ItemBase
    {
        public abstract string ItemName { get; }
        public abstract string ItemLangTokenName { get; }
        public abstract string ItemPickupDesc { get; }
        public abstract string ItemFullDescription { get; }
        public abstract string ItemLore { get; }

        public abstract ItemTier Tier { get; }
        public virtual ItemTag[] ItemTags { get; set; } = new ItemTag[] { };

        public abstract GameObject ItemModel { get; }
        public abstract Sprite ItemIcon { get; }

        public ItemDef ItemDef;

        public virtual String CorruptsItem { get; }

        public virtual bool CanRemove { get; } = true;

        public virtual bool AIBlacklisted { get; set; } = false;

        public bool isHidden = false;

        public ItemDef.Pair pair;

        /// <summary>
        /// This method structures your code execution of this class. An example implementation inside of it would be:
        /// <para>CreateConfig(config);</para>
        /// <para>CreateLang();</para>
        /// <para>CreateItem();</para>
        /// <para>Hooks();</para>
        /// <para>This ensures that these execute in this order, one after another, and is useful for having things available to be used in later methods.</para>
        /// <para>P.S. CreateItemDisplayRules(); does not have to be called in this, as it already gets called in CreateItem();</para>
        /// </summary>
        /// <param name="config">The config file that will be passed into this from the main class.</param>
        public abstract void Init(ConfigFile config);

        public virtual void CreateConfig(ConfigFile config) { }

        protected virtual void CreateLang()
        {
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_NAME", ItemName);
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_PICKUP", ItemPickupDesc);
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_DESCRIPTION", ItemFullDescription);
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_LORE", ItemLore);
        }

        public abstract ItemDisplayRuleDict CreateItemDisplayRules();
        protected void CreateItem()
        {
            if (AIBlacklisted)
            {
                ItemTags = new List<ItemTag>(ItemTags) { ItemTag.AIBlacklist }.ToArray();
            }

            ItemDef = ScriptableObject.CreateInstance<ItemDef>();
            ItemDef.name = "ITEM_" + ItemLangTokenName;
            ItemDef.nameToken = "ITEM_" + ItemLangTokenName + "_NAME";
            ItemDef.pickupToken = "ITEM_" + ItemLangTokenName + "_PICKUP";
            ItemDef.descriptionToken = "ITEM_" + ItemLangTokenName + "_DESCRIPTION";
            ItemDef.loreToken = "ITEM_" + ItemLangTokenName + "_LORE";
            ItemDef.pickupModelPrefab = ItemModel;
            ItemDef.pickupIconSprite = ItemIcon;
            ItemDef.hidden = isHidden;
            ItemDef.canRemove = CanRemove;
            ItemDef.tier = Tier;

            if (ItemTags.Length > 0) { ItemDef.tags = ItemTags; }

            ItemAPI.Add(new CustomItem(ItemDef, CreateItemDisplayRules()));
        }

        public virtual void Hooks() { }

        //Based on ThinkInvis' methods
        public int GetCount(CharacterBody body)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCount(ItemDef);
        }

        public int GetCount(CharacterMaster master)
        {
            if (!master || !master.inventory) { return 0; }

            return master.inventory.GetItemCount(ItemDef);
        }

        public int GetCountSpecific(CharacterBody body, ItemDef itemDef)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCount(itemDef);
        }

        internal static void RegisterVoidPairings(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            var voidTiers = new ItemTier[]
{
                ItemTier.VoidBoss,
                ItemTier.VoidTier1,
                ItemTier.VoidTier2,
                ItemTier.VoidTier3
};

            foreach (ItemBase item in ItemHelper.Items)
            {
                //Log.LogInfo("check void");
                if (item.ItemDef && voidTiers.Any(x => item.ItemDef.tier == x))
                {

                    var itemToCorrupt = ItemCatalog.itemDefs.Where(x => x.nameToken == item.CorruptsItem).First();
                    if (itemToCorrupt)
                    {
                        var pair = new ItemDef.Pair[]
                    {
                            new ItemDef.Pair
                            {
                                itemDef1 = itemToCorrupt,
                                itemDef2 = item.ItemDef,
                            }
                    };
                        //Log.LogInfo(ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].Length);
                        ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddRangeToArray(pair);
                    }
                }
            }
            orig();
        }

        internal static void RegisterItemTier(On.RoR2.ItemTierCatalog.orig_Init orig)
        {
            orig();
            foreach (ItemBase item in ItemHelper.Items)
            {
                Log.LogInfo(item.ItemDef.name + " try assign tier: " + item.Tier);
                item.ItemDef.tier = item.Tier;
                if (item.Tier == ItemTier.NoTier)
                {
                    item.ItemDef._itemTierDef = null;
                    item.ItemDef.deprecatedTier = ItemTier.NoTier;
                }
                Log.LogInfo(item.ItemName + " is assigned to tier: " + item.ItemDef.tier);
            }
        }
    }
}
