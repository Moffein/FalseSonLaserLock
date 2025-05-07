using RoR2;
using UnityEngine;
using BepInEx;
using System.Security;
using System.Security.Permissions;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace FalseSonLaserLock
{
    [BepInPlugin("com.Moffein.FalseSonLaserLock", "FalseSonLaserLock", "1.0.0")]
    public class FalseSonLaserLockPlugin : BaseUnityPlugin
    {
        private static int[] activeLasers;

        private static int meridianPhase3Count = 0;

        private void Awake()
        {
            RoR2.Stage.onStageStartGlobal += Stage_onStageStartGlobal;
            On.EntityStates.PrimeMeridian.LunarGazeLaserFire.OnEnter += LunarGazeLaserFire_OnEnter;
            On.EntityStates.PrimeMeridian.LunarGazeLaserFire.OnExit += LunarGazeLaserFire_OnExit;
            On.EntityStates.MeridianEvent.Phase3.OnEnter += Phase3_OnEnter;
            On.EntityStates.MeridianEvent.Phase3.OnExit += Phase3_OnExit;

            On.EntityStates.FalseSonBoss.LunarGazeLeap.OnEnter += LunarGazeLeap_OnEnter;

            IL.EntityStates.FalseSonBoss.FissureSlam.FixedUpdate += FissureSlam_FixedUpdate;
        }

        private void LunarGazeLeap_OnEnter(On.EntityStates.FalseSonBoss.LunarGazeLeap.orig_OnEnter orig, EntityStates.FalseSonBoss.LunarGazeLeap self)
        {
            orig(self);
            if (self.skillLocator && self.isAuthority)
            {
                if (self.skillLocator.secondary)
                {
                    bool removedStock = false;
                    if (self.skillLocator.secondary.stock > 0)
                    {
                        removedStock = true;
                        self.skillLocator.secondary.stock = 0;
                    }

                    if (self.skillLocator.secondary.cooldownRemaining < 12f || removedStock)
                    {
                        self.skillLocator.secondary.rechargeStopwatch = self.skillLocator.secondary.finalRechargeInterval - 12f;
                    }
                }

                if (self.skillLocator.special)
                {
                    bool removedStock = false;
                    if (self.skillLocator.special.stock > 0)
                    {
                        removedStock = true;
                        self.skillLocator.special.stock = 0;
                    }

                    if (self.skillLocator.special.cooldownRemaining < 12f || removedStock)
                    {
                        self.skillLocator.special.rechargeStopwatch = self.skillLocator.special.finalRechargeInterval - 12f;
                    }
                }
            }
        }

        private void Phase3_OnExit(On.EntityStates.MeridianEvent.Phase3.orig_OnExit orig, EntityStates.MeridianEvent.Phase3 self)
        {
            meridianPhase3Count--;
            orig(self);
        }

        private void Phase3_OnEnter(On.EntityStates.MeridianEvent.Phase3.orig_OnEnter orig, EntityStates.MeridianEvent.Phase3 self)
        {
            orig(self);
            meridianPhase3Count++;
        }

        public static bool IsLaserActive(TeamIndex teamIndex)
        {
            return activeLasers[(int)teamIndex] > 0;
        }

        private void FissureSlam_FixedUpdate(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld(typeof(EntityStates.FalseSonBoss.FissureSlam), "enableColumns")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, EntityStates.FalseSonBoss.FissureSlam, bool>>((enableColumns, self) =>
                {
                    return enableColumns && !IsLaserActive(self.GetTeam());
                });
            }
            else
            {
                Debug.LogError("FalseSonLaserLock: Failed to lock Fissure Slam projectiles.");
            }
        }

        private void LunarGazeLaserFire_OnExit(On.EntityStates.PrimeMeridian.LunarGazeLaserFire.orig_OnExit orig, EntityStates.PrimeMeridian.LunarGazeLaserFire self)
        {
            if (meridianPhase3Count > 0) LightningStormController.SetStormActive(true);
            orig(self);
        }

        private void LunarGazeLaserFire_OnEnter(On.EntityStates.PrimeMeridian.LunarGazeLaserFire.orig_OnEnter orig, EntityStates.PrimeMeridian.LunarGazeLaserFire self)
        {
            orig(self);
            if (meridianPhase3Count > 0) LightningStormController.SetStormActive(false);
        }

        private void Stage_onStageStartGlobal(Stage obj)
        {
            meridianPhase3Count = 0;
            activeLasers = new int[(int)TeamIndex.Count];
            for (int i = 0; i < activeLasers.Length; i++)
            {
                activeLasers[i] = 0;
            }
        }
    }
}

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}