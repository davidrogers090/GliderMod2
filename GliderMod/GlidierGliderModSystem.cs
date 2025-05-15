using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace GliderMod
{


    public class GlidierGliderModSystem : ModSystem
    {
        protected const string harmonyId = "glidierglider";

        public const float speedMin = 0.0001f;
        public const float speedFactor = 0.25f;
        public const float speedMax = 1.5f;

        public const float fallSpeed = 0.05f;

        


        protected Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            this.harmony = new Harmony(harmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Dispose()
        {
            base.Dispose();

            if (harmony != null)
            {
                this.harmony.UnpatchAll(harmonyId);
            }
        }
    }

    [HarmonyPatch(typeof(PModuleInAir), "ApplyFlying")]
    public class GG_PMIA_ApplyFlying
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ApplyFlying(PModuleInAir __instance, float dt, Entity entity, EntityPos pos, EntityControls controls) { }
    }

    [HarmonyPatch(typeof(PModulePlayerInAir), "ApplyFlying")]
    public class GG_PMPIA_ApplyFlying
    {
        // Associates persistent data with each PModulePlayerInAir instance
        static readonly ConditionalWeakTable<PModulePlayerInAir, Vec3f> lastLook =
            new();

        static bool Prefix(PModulePlayerInAir __instance, float dt, Entity entity, EntityPos pos, EntityControls controls)
        {
            if (controls.Gliding)
            {

                Vec3d lastFlightPath = pos.Motion.Clone();
                lastFlightPath.Y += GlidierGliderModSystem.fallSpeed;
                Vec3f lastPath = lastFlightPath.ToVec3f().NormalizedCopy();

                Vec3f lastPosLook = lastLook.GetOrCreateValue(__instance).NormalizedCopy();
                lastLook.GetOrCreateValue(__instance).Set(pos.GetViewVector());

                bool locked = true;
                if (lastPath.DistanceTo(lastPosLook) > 0.01f)
                {
                    locked = false;
                }

                

                if (controls.GlideSpeed == 0)
                {
                    controls.GlideSpeed = pos.Motion.Length();
                }
                double cosPitch = Math.Cos(pos.Pitch);
                double sinPitch = Math.Sin(pos.Pitch);

                double cosYaw = Math.Cos(pos.Yaw);
                double sinYaw = Math.Sin(pos.Yaw);

                double glideFactor = sinPitch;

                controls.GlideSpeed = GameMath.Clamp(controls.GlideSpeed - (glideFactor * dt * GlidierGliderModSystem.speedFactor), GlidierGliderModSystem.speedMin, GlidierGliderModSystem.speedMax);


                var pitch = sinPitch * controls.GlideSpeed;

                Vec3d perfect = new Vec3d(-cosPitch * sinYaw, sinPitch, -cosPitch * cosYaw);
                perfect.Normalize();

                if (locked) { 
                    pos.Motion = perfect.Mul(controls.GlideSpeed);
                }
                else
                {
                    Vec3f perff = perfect.ToVec3f();
                    
                    double x = GameMath.Lerp(lastPath.X, perff.X, 0.1f);
                    double y = GameMath.Lerp(lastPath.Y, perff.Y, 0.1f);
                    double z = GameMath.Lerp(lastPath.Z, perff.Z, 0.1f);
                    Vec3d smoothed = new Vec3d(x, y, z);
                    smoothed.Normalize();

                    pos.Motion = smoothed.Mul(controls.GlideSpeed);
                }
                    pos.Motion.Y -= GlidierGliderModSystem.fallSpeed;



            }
            else
            {
                GG_PMIA_ApplyFlying.ApplyFlying(__instance, dt, entity, pos, controls);
            }
            return false;
        }


    }
}


