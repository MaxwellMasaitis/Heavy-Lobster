using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Leg
        {
            // Naming conventions should change in the next version. So should the near target booleans.
            private Program _p;
            private IMyMotorStator _outerHip, _innerHip, _outerKnee, _innerKnee, _ankle;
            private PID _outerHipPid, _innerHipPid, _outerKneePid, _innerKneePid, _anklePid;
            private List<IMyLandingGear> _mags;
            public Leg(Program p, IMyMotorStator innerHip, IMyMotorStator outerHip, IMyMotorStator innerKnee, IMyMotorStator outerKnee, IMyMotorStator ankle, List<IMyLandingGear> mags)
            {
                _p = p;
                _innerHip = innerHip;
                _outerHip = outerHip;
                _innerKnee = innerKnee;
                _outerKnee = outerKnee;
                _ankle = ankle;
                _mags = mags;
                Setup();
            }
            private void Setup()
            {
                _innerHipPid = new PID(pComp, iComp, dComp, TimeStep);
                _outerHipPid = new PID(pComp, iComp, dComp, TimeStep);
                _innerKneePid = new PID(pComp, iComp, dComp, TimeStep);
                _outerKneePid = new PID(pComp, iComp, dComp, TimeStep);
                _anklePid = new PID(pComp, iComp, dComp, TimeStep);
            }

            public void SetInnerHipInertialTensor(bool isEnabled)
            {
                ((IMyTerminalBlock)_innerHip).SetValue<bool>("ShareInertiaTensor", isEnabled);
            }

            public void LockMags()
            {
                _mags.ForEach(item => item.Lock());
            }
            public void UnlockMags()
            {
                _mags.ForEach(item => item.Unlock());
            }

            public void SetRotorRPMsFromAngles(double innerHipTargetAngle, double outerHipTargetAngle, double innerKneeTargetAngle, double outerKneeTargetAngle, double ankleTargetAngle)
            {
                _p.SetTargetVelocityRPMFromAngle(_innerHip, _innerHipPid, innerHipTargetAngle);
                _p.SetTargetVelocityRPMFromAngle(_outerHip, _outerHipPid, outerHipTargetAngle);
                _p.SetTargetVelocityRPMFromAngle(_innerKnee, _innerKneePid, innerKneeTargetAngle);
                _p.SetTargetVelocityRPMFromAngle(_outerKnee, _outerKneePid, outerKneeTargetAngle);
                _p.SetTargetVelocityRPMFromAngle(_ankle, _anklePid, ankleTargetAngle);
            }
            public bool AnyReadyToLock()
            {
                return _mags.Any(i => i.LockMode == LandingGearMode.ReadyToLock);
            }
            public bool AllReadyToLock()
            {
                return _mags.All(i => i.LockMode == LandingGearMode.ReadyToLock);
            }
            public bool NearTargets(double targetHipAngle, double targetKneeAngle, double targetAnkleAngle)
            {
                // TODO: modify as appropriate
                return (Math.Abs(_p.correctError(targetHipAngle, MathHelper.ToDegrees(_innerHip.Angle) + MathHelper.ToDegrees(_outerHip.Angle))) < 0.1) &&
                    (Math.Abs(_p.correctError(targetKneeAngle, MathHelper.ToDegrees(_innerKnee.Angle) + MathHelper.ToDegrees(_outerKnee.Angle))) < 0.1) &&
                    (Math.Abs(_p.correctError(targetAnkleAngle, MathHelper.ToDegrees(_ankle.Angle))) < 0.1);
            }
        }
    }
}
