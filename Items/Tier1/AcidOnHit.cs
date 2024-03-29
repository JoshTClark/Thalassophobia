﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using Thalassophobia.Items.Lunar;
using UnityEngine;
using static On.RoR2.GlobalEventManager;
using static RoR2.DotController;

namespace Thalassophobia.Items.Tier1
{
    public class AcidOnHit : ItemBase<AcidOnHit>
    {
        public override string ItemName => "Acidic Rounds";

        public override string ItemLangTokenName => "ACID_ON_HIT";

        public override string ItemPickupDesc => "Chance to coat enemies with acid on hit.";

        public override string ItemFullDescription => "<style=cIsDamage>20%</style> chance to <style=cIsDamage>coat enemies with acid</style> doing <style=cIsDamage>25%</style> <style=cStack>(+25% per stack)</style> base damage 6 times over 3 seconds. The amount of times it deals damage <style=cIsDamage>scales with attack speed</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override GameObject ItemModel => Plugin.assetBundle.LoadAsset<GameObject>("AcidRoundModel.prefab");

        public override Sprite ItemIcon => Plugin.assetBundle.LoadAsset<Sprite>("AcidRoundsIcon.png");

        // Custom DoT for the acid
        public DotIndex acidDoTIndex;

        // Item stats
        private float chance;
        private float duration;
        private float interval;
        private float scale;
        private float damage;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }

        public override void CreateConfig(ConfigFile config)
        {
            ItemTags = new ItemTag[] { ItemTag.Damage };

            chance = config.Bind<float>("Item: " + ItemName, "Proc Chance", 20f, "Percent chance to proc the item.").Value;
            duration = config.Bind<float>("Item: " + ItemName, "Duration", 3f, "The amount of time the debuff lasts in seconds.").Value;
            interval = config.Bind<float>("Item: " + ItemName, "Interval", 1f, "Time inbetween each damage tick.").Value;
            scale = config.Bind<float>("Item: " + ItemName, "Attack Speed Scaling", 1.5f, "How much the interval between damage ticks decreases with attack speed.").Value;
            damage = config.Bind<float>("Item: " + ItemName, "Damage Coefficient", 0.25f, "Percent of your damage the item deals where 1.0 is 100%.").Value;

            BuffDef acidAffliction = ScriptableObject.CreateInstance<BuffDef>();
            acidAffliction.name = "Acidic Affliction";
            acidAffliction.iconSprite = Plugin.assetBundle.LoadAsset<Sprite>("AcidAfflictionIcon.png");
            acidAffliction.canStack = true;
            acidAffliction.isDebuff = true;
            ContentAddition.AddBuffDef(acidAffliction);

            DotDef acidDoT = new DotDef();
            acidDoT.damageColorIndex = DamageColorIndex.WeakPoint;
            acidDoT.associatedBuff = acidAffliction;
            acidDoT.damageCoefficient = damage;
            acidDoT.interval = interval;
            acidDoTIndex = DotAPI.RegisterDotDef(acidDoT, (self, dotStack) =>
            {
                var body = dotStack.attackerObject.GetComponent<CharacterBody>();
                var itemCount = GetCount(body);
                float percentAttackSpeed = 1 + (((body.attackSpeed - body.baseAttackSpeed) / body.baseAttackSpeed) * scale);
                dotStack.dotDef.interval = interval / percentAttackSpeed;
            });
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) => { 
                orig(self, damageInfo, victim);
                OnHitEffect(damageInfo, victim);
            };
            LunarDice.hook_DiceReroll += OnHitEffect;
        }

        public void OnHitEffect(global::RoR2.DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker)
            {
                var acidCount = GetCount(damageInfo.attacker.GetComponent<CharacterBody>());
                if (acidCount > 0)
                {
                    if (Util.CheckRoll(chance * damageInfo.procCoefficient, damageInfo.attacker.GetComponent<CharacterMaster>()) && !damageInfo.rejected)
                    {
                        DotController.InflictDot(victim, damageInfo.attacker, acidDoTIndex, duration, acidCount);
                    }
                }
            }
        }
    }
}