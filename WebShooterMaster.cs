using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using IngameDebugConsole;
using RootMotion.FinalIK;

namespace WebShooters
{
    public class WebShooterMaster : ThunderScript
    {
        public static Side? DefaultSide;
        public static ModOptionInt[] zeroToOneHundred()
        {
            ModOptionInt[] options = new ModOptionInt[101];
            for(int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionInt(i.ToString(), i); 
            }
            return options;
        }

        public static ModOptionInt[] zeroToOneFifty()
        {
            ModOptionInt[] options = new ModOptionInt[30];
            for (int i = 0; i < options.Length; i ++)
            {
                int res = i * 5;
                options[i] = new ModOptionInt(res.ToString("0"), res);
            }
            return options;
        }
        public static ModOptionFloat[] pointFiveToOne()
        {
            ModOptionFloat[] options = new ModOptionFloat[20];
            var val = 0.5f;
            for(int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.00"), val);
                val += 0.05f;
            }
            return options;
        }

        public static ModOptionInt[] zeroToTwenty()
        {
            ModOptionInt[] options = new ModOptionInt[20];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionInt(i.ToString(), i);
            }
            return options;
        }

        public static ModOptionBool[] AimValues = new[]
        {
            new ModOptionBool("Web Shooter Nozzle", true),
            new ModOptionBool("Hand", "Hand", false)
        };
        [ModOption("Web Reel Strength", "How Quickly you get reeled up the web", valueSourceName = nameof(zeroToOneHundred), defaultValueIndex = 15)]
        public static int ReelStrength;

        [ModOption("Kick Off Strength", "The Strength that is applied to you when you jump off a web line (move your hand quickly when letting go of a web line)", valueSourceName = nameof(zeroToOneHundred), defaultValueIndex = 10)]
        public static int KickOffStrength;

        [ModOption("Web Time Out", "If you miss, this is how long until the web self destructs", valueSourceName = nameof(zeroToTwenty), defaultValueIndex = 3)]
        public static int WebTimeOut;

        [ModOption("Web Gravity Mutliplier", "The Amount Of Gravity that effects the web", valueSourceName = nameof(pointFiveToOne), defaultValueIndex = 10)]
        public static float WebGravity;

        [ModOption("Web Line Projectile Force", "The Force used to shoot a web line", valueSourceName = nameof(zeroToOneFifty), defaultValueIndex = 10)]
        public static int WebLineForce;

        [ModOptionButton]
        [ModOption("Aim Point", "The point in which web shooters will use to shoot", valueSourceName = nameof(AimValues))]
        public static bool Nozzle;


        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            EventManager.onPossess += EventManager_onPossess;
        }

        private void EventManager_onPossess(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd) DefaultSide = Pointer.activeSide;
        }
    }
}
