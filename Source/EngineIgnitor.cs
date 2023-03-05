using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EngineIgnitor
{


    public class ModuleEngineIgnitor : PartModule
    {
        private bool _isEngineMouseOver;

        public enum EngineIgnitionState
        {
            INVALID = -1,
            NOT_IGNITED = 0,
            HIGH_TEMP = 1,
            IGNITED = 2,
        }

        public bool InRange;
        public bool IsExternal;

        [KSPField(isPersistant = false)]
        public int IgnitionsAvailable = -1; //-1: Infinite. 0: Unavailable. 1~...: As is.

        [KSPField(isPersistant = true)]
        public int IgnitionsRemained = -1;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Ignitions")]
        public string IgnitionsAvailableString = "";

        [KSPField(isPersistant = false)]
        public float AutoIgnitionTemperature = 800;

        [KSPField(isPersistant = false)]
        public float ECforIgnition = 0;

        int ecId = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Auto-Ignite")]
        public string AutoIgnitionState = "?/800";

        // In case we have multiple engines...
        [KSPField(isPersistant = false)]
        public int EngineIndex = 0;

        [KSPField(isPersistant = false)]
        public string IgnitorType = "T0";

        [KSPField(isPersistant = false)]
        public bool UseUllageSimulation = true;

        [KSPField]
        public float ChanceWhenUnstable = -1f;

        [KSPField]
        public bool DontUseIgnitorIfMultiModeOn = false;

        bool MultiModeEngine = false;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Flow")]
        public string UllageState;

        private float _fuelFlowStability;
        private float _oldFuelFlowStability;
        //private float fuelFlowRelativePosition = 0f;

        // List of all engines. So we can pick the one we are corresponding to.
        private List<EngineWrapper> _engines = new List<EngineWrapper>();
        private EngineWrapper _engine = null;

        // A state for the FSM.
        [KSPField(isPersistant = false, guiActive = true, guiName = "Engine State")]
        private EngineIgnitionState _engineState = EngineIgnitionState.INVALID;


        public void Start()
        {
            Log.Info("ModuleEngineIgnitor.Start, part: " + part.partInfo.title);

            _engines.Clear();
            if (part == null || part.Modules == null)
                return;
            foreach (PartModule module in part.Modules)
            {
                
                if (module.moduleName == "ModuleEnginesFX") //find partmodule engine on the part
                {
                    Log.Info("Adding ModuleEnginesFX, engineID:" + ((ModuleEnginesFX)module).engineID);
                    _engines.Add(new EngineWrapper(module as ModuleEnginesFX));

                }
                if (module.moduleName == "ModuleEngines") //find partmodule engine on the part
                {
                    Log.Info("Adding ModuleEngine, engineID:" + ((ModuleEngines)module).engineID);
                    _engines.Add(new EngineWrapper(module as ModuleEngines));
                }
                if (module.moduleName == "MultiModeEngine")
                    MultiModeEngine = true;

            }
            Log.Info("OnStart, EngineIndex: " + EngineIndex + ",   _engines.Count: " + _engines.Count());
            if (EngineIndex > _engines.Count())
            {
                Log.Info("EngineIndex out of bounds");
                return;
            }
            _engine = _engines.Count > EngineIndex ? _engines[EngineIndex] : null;

            if (IgnitionsRemained == -1) IgnitionsRemained = IgnitionsAvailable;
        }


        public override string GetInfo()
        {
            if (IgnitionsAvailable != -1)
                return "Can ignite for " + IgnitionsAvailable + " time(s).\n" + "Ignitor type: " + IgnitorType + "\n";
            return "Can ignite for infinite times.\n" + "Ignitor type: " + IgnitorType + "\n";
        }

        public void OnMouseEnter()
        {
            if (HighLogic.LoadedSceneIsEditor) _isEngineMouseOver = true;
        }

        public void OnMouseExit()
        {
            if (HighLogic.LoadedSceneIsEditor) _isEngineMouseOver = false;
        }

        int CurrentActiveMode()
        {
            int cnt = 0;
            foreach (PartModule pm in part.Modules) //change from part to partmodules
            {
                if (pm.moduleName == "ModuleEngines") //find partmodule engine on the part
                {
                    ModuleEngines em = pm as ModuleEngines;
                    cnt++;
                    if (em.EngineIgnited)
                        break;
                }
                if (pm.moduleName == "ModuleEnginesFX") //find partmodule engine on the part
                {
                    cnt++;
                    ModuleEnginesFX emfx = pm as ModuleEnginesFX;
                    if (emfx.EngineIgnited == true)
                        break;
                }
            }
            return cnt - 1;
        }
        bool EnoughECforIgnition()
        {
            if (ECforIgnition > 0)
                return true;
            // need to add check for multimode engine here
            if (MultiModeEngine)
            {


                if (EngineIndex == CurrentActiveMode())
                    return false;
            }
            return true;
        }

        void OnGUI()
        {

            if (_isEngineMouseOver == false) return;

            string ignitorInfo = "Ignitor: ";

            if (IgnitionsRemained == -1) ignitorInfo += IgnitorType + "(Infinite).";
            else ignitorInfo += IgnitorType + " (" + IgnitionsRemained + ").";

            string resourceRequired = "No resource requirement for ignition.";
            if (EnoughECforIgnition())
                resourceRequired = "EC required for ignition: " + ECforIgnition;


            var ullageInfo = (HighLogic.CurrentGame.Parameters.CustomParams<EI>().useUllage & UseUllageSimulation) ? "Need settling down fuel before ignition." : "Ullage simulation disabled.";

            Vector2 screenCoords = Camera.main.WorldToScreenPoint(part.transform.position);
            Rect ignitorInfoRect = new Rect(screenCoords.x - 100.0f, Screen.height - screenCoords.y - 10, 200.0f, 20.0f);
            GUIStyle ignitorInfoStyle = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold };
            ignitorInfoStyle.alignment = TextAnchor.MiddleCenter;
            ignitorInfoStyle.normal.textColor = Color.red;
            GUI.Label(ignitorInfoRect, ignitorInfo, ignitorInfoStyle);
            Rect ignitorResourceListRect = new Rect(screenCoords.x - 100.0f, Screen.height - screenCoords.y + 10.0f, 200.0f, 20.0f);
            GUI.Label(ignitorResourceListRect, resourceRequired, ignitorInfoStyle);
            Rect ullageInfoRect = new Rect(screenCoords.x - 100.0f, Screen.height - screenCoords.y + 30.0f, 200.0f, 20.0f);
            GUI.Label(ullageInfoRect, ullageInfo, ignitorInfoStyle);
        }

        bool autoShutdown = false;
        private void FixedUpdate()
        {
            if (_engine == null)
                Log.Info("ModuleEngineIgnitor.Update, _engine == null");
            else
                Log.Info("ModuleEngineIgnitor.Update, _engine.allowShutdown: " + _engine.allowShutdown);
            if (!HighLogic.LoadedSceneIsFlight || _engine == null || !_engine.allowShutdown) return;

            if (vessel.Landed)
            {
                IsExternal = CheckExternalIgnitor();
                if (IsExternal)
                    IgnitionsAvailableString = "Provided from Ground" + " : [ " + IgnitionsRemained + "/" + IgnitionsAvailable + " ]";
                else
                {
                    if (IgnitionsRemained != -1)
                        IgnitionsAvailableString = IgnitorType + " : [ " + IgnitionsRemained + "/" + IgnitionsAvailable + " ]";
                    else
                        IgnitionsAvailableString = "Unlimited";
                }
            }
            else
            {
                IsExternal = false;
                if (IgnitionsRemained != -1)
                    IgnitionsAvailableString = IgnitorType + " : [ " + IgnitionsRemained + "/" + IgnitionsAvailable + " ]";
                else
                    IgnitionsAvailableString = "Unlimited";
            }


            if (part != null)
                AutoIgnitionState = part.temperature.ToString("F1") + "/" + AutoIgnitionTemperature.ToString("F1");
            else
                AutoIgnitionState = "?/" + AutoIgnitionTemperature.ToString("F1");



            if (FlightGlobals.ActiveVessel != null)
            {
                Events["ReloadIgnitor"].guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
                Events["ReloadIgnitor"].guiName = "Reload Ignitor (" + IgnitionsAvailableString + ")";
            }

            var oldState = _engineState;
            DecideNewState(oldState);

            Log.Info("oldEngineThrottle: " + oldEngineThrottle + ", autoShutdown: " + autoShutdown);
            if (_engine.requestedThrottle > 0.0f)
                autoShutdown = false;
            else
            {
                if (!autoShutdown)
                {
                    _engineState = EngineIgnitionState.NOT_IGNITED;
                    autoShutdown = true;
                }
            }

            _oldFuelFlowStability = _fuelFlowStability;
            CheckUllageState();

            var isIgnited = IgnitionProcess(oldState, IsExternal); //, totalRes);

            IgnitionResult(IsExternal, isIgnited);
        }

        private void DecideNewState(EngineIgnitionState oldState)
        {
            if (_engine.EngineIgnited)
                Log.Info("DecidenewState, oldState: " + oldState + ", _engine.requestedThrottle: " + _engine.requestedThrottle +
                    ", _engine.EngineIgnited: " + _engine.EngineIgnited + ", _engine.allowShutdown: " + _engine.allowShutdown);

            if ((_engine.requestedThrottle <= 0.0f && !MultiModeEngine) || _engine.flameout || (_engine.EngineIgnited == false && _engine.allowShutdown))
            {
                if (_engine.part.temperature >= AutoIgnitionTemperature)
                    _engineState = EngineIgnitionState.HIGH_TEMP;
                else
                    _engineState = EngineIgnitionState.NOT_IGNITED;
            }
            else
            {
                if (oldState != EngineIgnitionState.IGNITED)
                {
                    //When changing from not-ignited to ignited, we must ensure that the throttle is non-zero or locked (SRBs)
                    if (vessel.ctrlState.mainThrottle > 0.0f || _engine.throttleLocked)
                    {
                        _engineState = EngineIgnitionState.IGNITED;
                        autoShutdown = false;
                    }
                    else
                        _engineState = EngineIgnitionState.NOT_IGNITED;
                }
            }
        }


        private void CheckUllageState()
        {
            if (UseUllageSimulation)
            {
                Vector3d thrustDirection = _engine.ForwardTransform;
                Vector3d geeVector = vessel.acceleration_immediate - vessel.graviticAcceleration;
                var a = 180 - Vector3.Angle(thrustDirection, geeVector);
                //if (_engine.PreviousTranformPosition == null) 
                //{
                //    _engine.PreviousTranformPosition = _engine.Transform.position;
                //} 
                //Vector3 currentEnginePosition = _engine.Transform.position;
                //Vector3 centrifugalVector = (currentEnginePosition - vessel.CoM);
                //float distanceComToEngine = centrifugalVector.magnitude;
                //Vector3 tangentialVelocity = (currentEnginePosition - _engine.PreviousTranformPosition) / Time.fixedDeltaTime;
                //float centrifugalGee = Mathf.Pow(tangentialVelocity.magnitude, 2) / distanceComToEngine;
                //Vector3 centrifugalAcceleration = centrifugalVector.normalized * centrifugalGee;
                //geeVector = geeVector + centrifugalAcceleration;

                var gees = geeVector.magnitude / vessel.graviticAcceleration.magnitude;
                var geesInEngineDirection = Math.Cos(a * Math.PI / 180) * gees;
                //var fuelSpeedInstant = gees / Time.fixedDeltaTime;
                //fuelFlowRelativePosition = distanceComToEngine - (float)fuelSpeedInstant;
               // Debug.Log("a: " + a + ", gees: "+ gees + ", a_i: " + vessel.acceleration_immediate.magnitude);
                if (a < 90 && geesInEngineDirection >= 0.01 || vessel.Landed)
                {
                    UllageState = "Stable";
                    _fuelFlowStability = 1.0f;
                   // fuelFlowRelativePosition = 0;
                    return;
                }
                else
                {
                    Log.Info("Unstable");
                    if (ChanceWhenUnstable >= 0)
                        _fuelFlowStability = ChanceWhenUnstable;
                    else
                        _fuelFlowStability = (float)HighLogic.CurrentGame.Parameters.CustomParams<EI>().ChanceWhenUnstable / 100;
                    UllageState = "UnStable (Success Chance: " + (_fuelFlowStability * 100f) + "%)";

                    return;
                }
            }
            else
            {
                UllageState = "Very Stable";
                _fuelFlowStability = 1.0f;
            }
            //return true;
        }


        float oldEngineThrottle = 0;
        bool OtherEngineModeActive()
        {
            string s;

            // Check to see if any other engine mode is on
            int cnt = 0;
            foreach (PartModule pm in part.Modules) //change from part to partmodules
            {
                if (pm.moduleName == "ModuleEngines") //find partmodule engine on the part
                {
                    if (cnt == EngineIndex)
                        continue;
                    cnt++;
                    ModuleEngines em = pm as ModuleEngines;

                    bool deprived = em.CheckDeprived(.01, out s);
                    if (em.EngineIgnited == true && !em.flameout && !deprived)
                    {
                        oldEngineThrottle = em.requestedThrottle;
                        return true;
                    }
                }
                if (pm.moduleName == "ModuleEnginesFX") //find partmodule engine on the part
                {
                    if (cnt == EngineIndex)
                        continue;
                    cnt++;
                    ModuleEnginesFX emfx = pm as ModuleEnginesFX;

                    bool deprived = emfx.CheckDeprived(.01, out s);
                    if (emfx.EngineIgnited == true && !emfx.flameout && !deprived)
                    {
                        oldEngineThrottle = emfx.requestedThrottle;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IgnitionProcess(EngineIgnitionState oldState, bool isExternal) //, List<OnboardIgnitorResource> aaa)
        {
            Log.Info("IgnitionProcess, oldState: " + oldState + ", _engineState: " + _engineState + ",   isExternal: " + isExternal);

            if (DontUseIgnitorIfMultiModeOn && OtherEngineModeActive())
            {
                _engineState = EngineIgnitionState.IGNITED;
                autoShutdown = false;
                Log.Info("IgnitionProcess, engine already ignited");
                return true;
            }
            if (oldState == EngineIgnitionState.NOT_IGNITED && _engineState == EngineIgnitionState.IGNITED)
            {
                if (isExternal)
                {
                    return true;
                }
                Log.Info("IgnitionProcess, IgnitionsRemained: " + IgnitionsRemained + ",  ECforIgnition: " + ECforIgnition +
                    ", MultiModeEngine: " + MultiModeEngine + ", CurrentActiveMode: " + CurrentActiveMode());
                if (!MultiModeEngine || (MultiModeEngine && !OtherEngineModeActive()) || CurrentActiveMode() == EngineIndex)
                {
                    if (IgnitionsRemained > 0 || IgnitionsRemained <= -1)
                    {
                        double ec = 0;
                        if (HighLogic.CurrentGame.Parameters.CustomParams<EI>().requireECforIgnition && EnoughECforIgnition())
                        {

                            ec = part.RequestResource(ecId, (double)ECforIgnition);
                            if (ec != ECforIgnition)
                            {
                                ScreenMessages.PostScreenMessage("Do not have enough Electrical Charge", 3f, ScreenMessageStyle.UPPER_CENTER);
                                _engineState = EngineIgnitionState.NOT_IGNITED;
                                return false;
                            }
                        }

                        IgnitionsRemained--;
                    }
                    else
                    {
                        _engineState = EngineIgnitionState.NOT_IGNITED;
                        return false;
                    }
                }

                float minPotential = 1.0f;
                if (UseUllageSimulation)
                {
                    minPotential *= _oldFuelFlowStability;
                    var chance = UnityEngine.Random.Range(0.0f, 1.0f);
                    var attempt = chance <= minPotential;
                    Debug.Log("EngineIgnitor: minPotential: " + minPotential.ToString() + ", chance: " + chance.ToString());
                    ScreenMessages.PostScreenMessage("Chance of ignition success: " + minPotential + ", Random: " + chance.ToString(), 5, ScreenMessageStyle.UPPER_CENTER);
                    if (!attempt)
                    {
                        ScreenMessages.PostScreenMessage("Ignition failed due to fuel flow instability", 3f, ScreenMessageStyle.UPPER_CENTER);
                        _engineState = EngineIgnitionState.NOT_IGNITED;
                        return false;
                    }
                }
            }
            if (oldState == EngineIgnitionState.HIGH_TEMP && _engineState == EngineIgnitionState.IGNITED)
            {
                _engineState = EngineIgnitionState.IGNITED;
                autoShutdown = false;
                return true;
            }

            return true;
        }

        private void IgnitionResult(bool isExternal, bool isIgnited)
        {
          
            if (_engineState == EngineIgnitionState.NOT_IGNITED && ((IgnitionsRemained == 0 && !isExternal) || !isIgnited))
            {
                if (_engine.EngineIgnited)
                {
                    if (IgnitionsRemained == 0)
                        ScreenMessages.PostScreenMessage("No available ignitors", 3f, ScreenMessageStyle.UPPER_CENTER);
                    _engine.BurstFlameoutGroups();
                    _engine.SetRunningGroupsActive(false);
                    foreach (BaseEvent baseEvent in _engine.Events)
                    {
                        if (baseEvent.name.IndexOf("shutdown", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            baseEvent.Invoke();
                        }
                    }
                    _engine.SetRunningGroupsActive(false);
                }
            }
        }

        private bool CheckExternalIgnitor()
        {
            InRange = false;
            Vector3 r;

            Log.Info("CheckExternalIgnitor 1, count: " + ModuleExternalIgnitor.ExternalIgnitors.Count);
            for (int i = 0; i < ModuleExternalIgnitor.ExternalIgnitors.Count; ++i)
            {
                ModuleExternalIgnitor itor = ModuleExternalIgnitor.ExternalIgnitors[i];
                if (itor.vessel == null || itor.vessel.transform == null || itor.part == null || itor.part.transform == null)
                {
                    ModuleExternalIgnitor.ExternalIgnitors.RemoveAt(i);
                    --i;
                }
            }
            Log.Info("CheckExternalIgnitor 2, count: " + ModuleExternalIgnitor.ExternalIgnitors.Count);

            foreach (ModuleExternalIgnitor extIgnitor in ModuleExternalIgnitor.ExternalIgnitors)
            {
                if (extIgnitor.vessel == null || extIgnitor.vessel.transform == null || extIgnitor.part == null ||
                    extIgnitor.part.transform == null)
                    ModuleExternalIgnitor.ExternalIgnitors.Remove(extIgnitor);
                Vector3 range = new Vector3();
                bool b = false;
                if (extIgnitor.vessel != null && extIgnitor.part != null && extIgnitor.vessel.transform != null)
                {
                    r = extIgnitor.vessel.transform.TransformPoint(extIgnitor.part.orgPos) -
                             _engine.vessel.transform.TransformPoint(_engine.part.orgPos);
                    if (!b)
                    {
                        range = r;
                        b = true;
                    }
                    else
                    {
                        if (r.magnitude < range.magnitude)
                            range = r;
                    }
                }
                
                InRange = range.magnitude < extIgnitor.IgniteRange;
                if (InRange) return true;
            }
            
            return false;
        }

        [KSPEvent(name = "ReloadIgnitor", guiName = "Reload Ignitor", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ReloadIgnitor()
        {
            if (IgnitionsAvailable == -1 || IgnitionsRemained == IgnitionsAvailable) return;
            var eva = FlightGlobals.ActiveVessel;
            var evaKerbalExp = eva.GetVesselCrew().First().experienceTrait.Title;
            double engineIgnitorsAmountEva = 0;
            double engineIgnitorsMaxAmountEva = 0;
            int engineIgnitorsId = PartResourceLibrary.Instance.GetDefinition("EngineIgnitors").id;
            eva.rootPart.GetConnectedResourceTotals(engineIgnitorsId, out engineIgnitorsAmountEva, out engineIgnitorsMaxAmountEva);

            if (evaKerbalExp.Equals("Engineer"))
            {
                if (IgnitionsRemained < IgnitionsAvailable && engineIgnitorsAmountEva > 0)
                {
                    eva.rootPart.RequestResource("EngineIgnitors", (double)1);
                    IgnitionsRemained++;
                }
                else
                    ScreenMessages.PostScreenMessage("Nothing to load", 4.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                ScreenMessages.PostScreenMessage("Requires engineer power.", 4.0f, ScreenMessageStyle.UPPER_CENTER);
        }

#if false
        public override void OnSave(ConfigNode node)
        {
            foreach (IgnitorResource ignitorResource in IgnitorResources)
            {
                ignitorResource.Save(node.AddNode("IGNITOR_RESOURCE"));
            }
            base.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            IgnitorResourcesStr = new List<string>();
            IgnitorResources = new List<IgnitorResource>();

            foreach (ConfigNode subNode in node.GetNodes("IGNITOR_RESOURCE"))
            {
                if (subNode.HasValue("name") == false || subNode.HasValue("amount") == false)
                {
                    continue;
                }
                IgnitorResource newIgnitorResource = new IgnitorResource();
                newIgnitorResource.Load(subNode);
                IgnitorResources.Add(newIgnitorResource);
                IgnitorResourcesStr.Add(newIgnitorResource.ToString());
            }
        }
#endif
    }
}
