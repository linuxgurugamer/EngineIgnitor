using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;


namespace EngineIgnitor
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    public class EI : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Engine Ignitor"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Engine Ignitor"; } }
        public override string DisplaySection { get { return "Engine Ignitor"; } }
        public override int SectionOrder { get { return 3; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomFloatParameterUI("Unstable success chance", minValue = 0f, maxValue = 100.0f,
            toolTip ="Base chance of a successful ignition when the fuel is unstable.")]
        public double ChanceWhenUnstable = 20f;

        [GameParameters.CustomParameterUI("Allow test mode",
            toolTip = " If enabled, then there will be a toolbar button (either Blizzy or stock) to enable a test mode, which essentially disables this mod.\n (may require scene change to activate)")]
        public bool allowTestMode = false;

        [GameParameters.CustomParameterUI("Use Ullage simulation", toolTip = "If enabled, some engines will not fire if fuel is unstable")]
        public bool useUllage = true;

        [GameParameters.CustomParameterUI("Require EC for ignition", toolTip = "If enabled, engines will require a certain amount of EC for ignition")]
        public bool requireECforIgnition = true;




        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {

            return true;
        }


        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            
            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
