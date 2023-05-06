using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
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
    partial class Program : MyGridProgram
    {
		// This file contains your actual script.
		//
		// You can either keep all your code here, or you can create separate
		// code files to make your program easier to navigate while coding.
		//
		// In order to add a new utility class, right-click on your project, 
		// select 'New' then 'Add Item...'. Now find the 'Space Engineers'
		// category under 'Visual C# Items' on the left hand side, and select
		// 'Utility Class' in the main area. Name it in the box below, and
		// press OK. This utility class will be merged in with your code when
		// deploying your final script.
		//
		// You can also simply create a new utility class manually, you don't
		// have to use the template if you don't want to. Just do so the first
		// time to see what a utility class looks like.
		// 
		// Go to:
		// https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
		//
		// to learn more about ingame scripts.

		IMyRemoteControl remoteControl;
		//TODO: use .IsUnderControl to make it so you can manually take control of the states
		// then maybe use the AI to make it autonomous otherwise?

		Vector3D gravityVec, toTargetVec, targetPos;
		IMyGyro rightGyro, leftGyro;
		IMySensorBlock sensor;

		List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();

		IMyTurretControlBlock rightTurret, leftTurret;

		IMyThrust rearThrust, frontThrust, rightThrust, leftThrust;

		IMyMotorStator rightWing, rightOuterHip, rightInnerHip, rightOuterKnee, rightInnerKnee, rightAnkle, rightWrist, leftWing, leftInnerHip, leftOuterHip, leftInnerKnee, leftOuterKnee, leftAnkle, leftWrist;

		double targetRightHipAngle, targetRightKneeAngle, targetRightAnkleAngle, targetLeftHipAngle, targetLeftKneeAngle, targetLeftAnkleAngle;
		int frame, leftFrame;
		bool readyToCharge, readyToLeap;

		const double TimeStep = 1.0 / 60.0, pComp = 3, iComp = 0, dComp = 0;
		PID rightInnerHipPid, rightOuterHipPid, rightInnerKneePid, rightOuterKneePid, rightAnklePid, rightElevPid, rightAzPid, leftInnerHipPid, leftOuterHipPid, leftInnerKneePid, leftOuterKneePid, leftAnklePid, rollPid, pitchPid, yawPid, leftElevPid, leftAzPid;
		const float standingHip = 24, standingKnee = 15;

		ImmutableArray<MyTuple<int, int>> legFrames = ImmutableArray.Create(
			new MyTuple<int, int>(26, 14),
			new MyTuple<int, int>(28, 13),
			new MyTuple<int, int>(30, 11),
			new MyTuple<int, int>(32, 10),
			new MyTuple<int, int>(34, 8),
			new MyTuple<int, int>(36, 6),
			new MyTuple<int, int>(38, 4),
			new MyTuple<int, int>(40, 2),
			new MyTuple<int, int>(43, 0),
			new MyTuple<int, int>(45, -2),
			new MyTuple<int, int>(48, -5),
			new MyTuple<int, int>(51, -8),
			new MyTuple<int, int>(54, -11),
			new MyTuple<int, int>(57, -15),
			new MyTuple<int, int>(61, -20),
			new MyTuple<int, int>(65, -25),
			new MyTuple<int, int>(43, 14),
			new MyTuple<int, int>(37, 27),
			new MyTuple<int, int>(29, 42),
			new MyTuple<int, int>(26, 52),
			new MyTuple<int, int>(20, 60),
			new MyTuple<int, int>(19, 68),
			new MyTuple<int, int>(13, 76),
			new MyTuple<int, int>(13, 82),
			new MyTuple<int, int>(7, 86),
			new MyTuple<int, int>(4, 88),
			new MyTuple<int, int>(0, 90),
			new MyTuple<int, int>(-4, 92),
			new MyTuple<int, int>(-7, 93),
			new MyTuple<int, int>(-7, 90),
			new MyTuple<int, int>(-14, 93),
			new MyTuple<int, int>(-12, 88),
			new MyTuple<int, int>(-19, 91),
			new MyTuple<int, int>(-17, 86),
			new MyTuple<int, int>(-23, 88),
			new MyTuple<int, int>(-20, 83),
			new MyTuple<int, int>(-26, 85),
			new MyTuple<int, int>(-22, 80),
			new MyTuple<int, int>(-28, 81),
			new MyTuple<int, int>(-24, 76),
			new MyTuple<int, int>(-21, 66),
			new MyTuple<int, int>(-17, 60),
			new MyTuple<int, int>(-16, 55),
			new MyTuple<int, int>(-12, 49),
			new MyTuple<int, int>(-11, 42),
			new MyTuple<int, int>(-6, 36),
			new MyTuple<int, int>(-4, 28),
			new MyTuple<int, int>(1, 21),
			new MyTuple<int, int>(-4, 28),
			new MyTuple<int, int>(-6, 36),
			new MyTuple<int, int>(-11, 42),
			new MyTuple<int, int>(-12, 49),
			new MyTuple<int, int>(-13, 52),
			new MyTuple<int, int>(-13, 54),
			new MyTuple<int, int>(-13, 57),
			new MyTuple<int, int>(-13, 60),
			new MyTuple<int, int>(-9, 54),
			new MyTuple<int, int>(-6, 53),
			new MyTuple<int, int>(-2, 47),
			new MyTuple<int, int>(1, 46),
			new MyTuple<int, int>(5, 40),
			new MyTuple<int, int>(12, 32),
			new MyTuple<int, int>(16, 25),
			// this is what would be frame 0, it's moved to the end to simplify the walking code a bit
			new MyTuple<int, int>(24, 15)
			);

		List<IMyLandingGear> leftMags, rightMags;

		Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
		MyCommandLine _commandLine = new MyCommandLine();

		public enum State
		{
			Stand,
			Walk,
			Charge,
			Leap
		}

		State currentState;

		public Program()
		{
			Runtime.UpdateFrequency = UpdateFrequency.Update1;

			_commands["setState"] = SetState;

			remoteControl = GridTerminalSystem.GetBlockWithName("Lobster Remote Control") as IMyRemoteControl;
			sensor = GridTerminalSystem.GetBlockWithName("Lobster Automaton Sensor") as IMySensorBlock;

			rightWing = GridTerminalSystem.GetBlockWithName("Lobster Wing Rotor Right") as IMyMotorStator;
			rightWrist = GridTerminalSystem.GetBlockWithName("Lobster Claw Wrist Hinge Right") as IMyMotorStator;
			rightInnerHip = GridTerminalSystem.GetBlockWithName("Lobster Hip Inner Rotor Right") as IMyMotorStator;
			rightOuterHip = GridTerminalSystem.GetBlockWithName("Lobster Hip Outer Rotor Right") as IMyMotorStator;
			rightInnerKnee = GridTerminalSystem.GetBlockWithName("Lobster Knee Inner Rotor Right") as IMyMotorStator;
			rightOuterKnee = GridTerminalSystem.GetBlockWithName("Lobster Knee Outer Rotor Right") as IMyMotorStator;
			rightAnkle = GridTerminalSystem.GetBlockWithName("Lobster Ankle Rotor Right") as IMyMotorStator;
			rightInnerHipPid = new PID(pComp, iComp, dComp, TimeStep);
			rightOuterHipPid = new PID(pComp, iComp, dComp, TimeStep);
			rightInnerKneePid = new PID(pComp, iComp, dComp, TimeStep);
			rightOuterKneePid = new PID(pComp, iComp, dComp, TimeStep);
			rightAnklePid = new PID(pComp, iComp, dComp, TimeStep);
			rightElevPid = new PID(pComp, iComp, dComp, TimeStep);
			rightAzPid = new PID(pComp, iComp, dComp, TimeStep);
			rightGyro = GridTerminalSystem.GetBlockWithName("Lobster Gyroscope Right") as IMyGyro;
			rightTurret = GridTerminalSystem.GetBlockWithName("Lobster Claw Turret Controller Right") as IMyTurretControlBlock;
			IMyBlockGroup rightMagsGroup = GridTerminalSystem.GetBlockGroupWithName("Lobster Magnetic Plates Right");
			rightMags = new List<IMyLandingGear>();
			rightMagsGroup.GetBlocksOfType(rightMags);

			leftWing = GridTerminalSystem.GetBlockWithName("Lobster Wing Rotor Left") as IMyMotorStator;
			leftWrist = GridTerminalSystem.GetBlockWithName("Lobster Claw Wrist Hinge Left") as IMyMotorStator;
			leftInnerHip = GridTerminalSystem.GetBlockWithName("Lobster Hip Inner Rotor Left") as IMyMotorStator;
			leftOuterHip = GridTerminalSystem.GetBlockWithName("Lobster Hip Outer Rotor Left") as IMyMotorStator;
			leftInnerKnee = GridTerminalSystem.GetBlockWithName("Lobster Knee Inner Rotor Left") as IMyMotorStator;
			leftOuterKnee = GridTerminalSystem.GetBlockWithName("Lobster Knee Outer Rotor Left") as IMyMotorStator;
			leftAnkle = GridTerminalSystem.GetBlockWithName("Lobster Ankle Rotor Left") as IMyMotorStator;
			leftInnerHipPid = new PID(pComp, iComp, dComp, TimeStep);
			leftOuterHipPid = new PID(pComp, iComp, dComp, TimeStep);
			leftInnerKneePid = new PID(pComp, iComp, dComp, TimeStep);
			leftOuterKneePid = new PID(pComp, iComp, dComp, TimeStep);
			leftAnklePid = new PID(pComp, iComp, dComp, TimeStep);
			leftElevPid = new PID(pComp, iComp, dComp, TimeStep);
			leftAzPid = new PID(pComp, iComp, dComp, TimeStep);
			leftGyro = GridTerminalSystem.GetBlockWithName("Lobster Gyroscope Left") as IMyGyro;
			leftTurret = GridTerminalSystem.GetBlockWithName("Lobster Claw Turret Controller Left") as IMyTurretControlBlock;
			IMyBlockGroup leftMagsGroup = GridTerminalSystem.GetBlockGroupWithName("Lobster Magnetic Plates Left");
			leftMags = new List<IMyLandingGear>();
			leftMagsGroup.GetBlocksOfType(leftMags);

			rearThrust = GridTerminalSystem.GetBlockWithName("Lobster Hydrogen Thruster Rear") as IMyThrust;
			frontThrust = GridTerminalSystem.GetBlockWithName("Lobster Hydrogen Thruster Front") as IMyThrust;
			rightThrust = GridTerminalSystem.GetBlockWithName("Lobster Hydrogen Thruster Rear Right") as IMyThrust;
			leftThrust = GridTerminalSystem.GetBlockWithName("Lobster Hydrogen Thruster Rear Left") as IMyThrust;

			currentState = State.Stand;
			frame = 0;
			leftFrame = legFrames.Length;

			readyToCharge = false;
			readyToLeap = false;

			//TODO: move the overrides?
			rightGyro.GyroOverride = true;
			leftGyro.GyroOverride = true;
			rollPid = new PID(10, 0, 0, TimeStep);
			pitchPid = new PID(10, 0, 0, TimeStep);
			//recheck this - 8 0 9 was working ok in space, but it doesnt seem any more effective on land than 10 0 0
			yawPid = new PID(10, 0, 0, TimeStep);
		}

		public void SetState()
		{
			Enum.TryParse(_commandLine.Argument(1), out currentState);
		}

		//TODO: save current state and frame data?

		public void Main(string argument, UpdateType updateSource)
		{
			if (_commandLine.TryParse(argument))
			{
				Action commandAction;
				string command = _commandLine.Argument(0);
				if (command == null)
				{
					Echo("No command specified");
				}
				else if (_commands.TryGetValue(command, out commandAction))
				{
					commandAction();
				}
				else
				{
					Echo($"Unknown command {command}");
				}
			}
			gravityVec = Vector3D.TransformNormal(remoteControl.GetTotalGravity(), MatrixD.Transpose(remoteControl.WorldMatrix));
			Echo("Gravity X");
			Echo(gravityVec.X.ToString()); // Roll, - = left, + = right
			Echo("Gravity Z");
			Echo(gravityVec.Z.ToString()); // Pitch, + = back, - = forward

			Echo("Roll PID");
			Echo(rollPid.Control(-gravityVec.X).ToString());
			Echo("Pitch PID");
			Echo(pitchPid.Control(gravityVec.Z).ToString());

			//TODO: fix whatever this BS is: it's detecting neutral entities as enemies, and then crashes when they're gone from the world
			// IsActive doesn't work - apparently the script technically runs faster, causing things to break
			// try getting the entity list and checking its .Count property
			// doesnt work...? do better error checking: https://github.com/malware-dev/MDK-SE/wiki/Debugging-Your-Scripts
			if (sensor.IsActive)
			{
				sensor.DetectedEntities(entities);
				if (entities.Count > 0)
				{
					Echo("Target");
					Echo(entities[0].Name);
					//TODO: consider using sensor.DetectedEntities(listWhatTheEntitiesGoIn) and pulling detected entities from that instead.
					toTargetVec = entities[0].Position - sensor.GetPosition();
					targetPos = Vector3D.TransformNormal(toTargetVec, MatrixD.Transpose(sensor.WorldMatrix));
					rightGyro.Yaw = (float)yawPid.Control(targetPos.X);
					leftGyro.Yaw = (float)yawPid.Control(targetPos.X);
				}
			}
			else
			{
				rightGyro.Yaw = 0;
				leftGyro.Yaw = 0;
			}

			rightGyro.Roll = (float)rollPid.Control(-gravityVec.X);
			rightGyro.Pitch = (float)pitchPid.Control(gravityVec.Z);
			leftGyro.Roll = (float)rollPid.Control(-gravityVec.X);
			leftGyro.Pitch = (float)pitchPid.Control(gravityVec.Z);

			if (rightTurret.HasTarget || leftTurret.HasTarget)
			{
				rightWrist.TargetVelocityRPM = 60;
				leftWrist.TargetVelocityRPM = 60;
			}
			else
			{
				rightWrist.TargetVelocityRPM = -60;
				leftWrist.TargetVelocityRPM = -60;
				//set turret rotors to 0 and +-10
				rightTurret.ElevationRotor.TargetVelocityRPM = (float)rightElevPid.Control(correctError(0, MathHelper.ToDegrees(rightTurret.ElevationRotor.Angle)));
				rightTurret.AzimuthRotor.TargetVelocityRPM = (float)rightAzPid.Control(correctError(0, MathHelper.ToDegrees(rightTurret.AzimuthRotor.Angle)));
				leftTurret.ElevationRotor.TargetVelocityRPM = (float)leftElevPid.Control(correctError(0, MathHelper.ToDegrees(leftTurret.ElevationRotor.Angle)));
				leftTurret.AzimuthRotor.TargetVelocityRPM = (float)leftAzPid.Control(correctError(0, MathHelper.ToDegrees(leftTurret.AzimuthRotor.Angle)));
			}

			if (currentState == State.Walk)
			{
				//TODO: maybe change this?
				rearThrust.Enabled = false;
				frontThrust.Enabled = false;
				rightThrust.Enabled = false;
				leftThrust.Enabled = false;
				rightTurret.Enabled = true;
				leftTurret.Enabled = true;
				readyToCharge = false;
				readyToLeap = false;

				((IMyTerminalBlock)rightInnerHip).SetValue<bool>("ShareInertiaTensor", false);
				((IMyTerminalBlock)leftInnerHip).SetValue<bool>("ShareInertiaTensor", false);

				leftFrame = frame + legFrames.Length/2;
				if (leftFrame >= legFrames.Length)
				{
					leftFrame -= legFrames.Length;
				}

				Echo("Frame");
				Echo(frame.ToString());

				targetRightHipAngle = legFrames[frame].Item1;
				targetLeftHipAngle = -legFrames[leftFrame].Item1;

				targetRightKneeAngle = legFrames[frame].Item2;
				targetLeftKneeAngle = -legFrames[leftFrame].Item2;

				// -10 degrees on frames 17-40
				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				if (17 <= frame && frame <= 40)
				{
					targetRightAnkleAngle = targetRightAnkleAngle - 10;
				}
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;
				if (17 <= leftFrame && leftFrame <= 40)
				{
					targetLeftAnkleAngle = targetLeftAnkleAngle + 10;
				}

				if (17 <= frame && frame <= 40)
				{
					rightMags.ForEach(item => item.Unlock());
				}
				else if (48 <= frame || frame < 17)
				{
					rightMags.ForEach(item => item.Lock());
				}
				if (17 <= leftFrame && leftFrame <= 40)
				{
					leftMags.ForEach(item => item.Unlock());
				}
				else if (48 <= leftFrame || leftFrame < 17)
				{
					leftMags.ForEach(item => item.Lock());
				}

				if (frame == 21 || leftFrame == 21)
				{
					rightWing.TargetVelocityRPM = 60;
					leftWing.TargetVelocityRPM = -60;
				}
				else if (frame == 29 || leftFrame == 29)
				{
					rightWing.TargetVelocityRPM = -60;
					leftWing.TargetVelocityRPM = 60;
				}

				frame++;
				if (frame >= legFrames.Length)
				{
					frame = 0;
				}

			}
			else if (currentState == State.Stand)
			{
				//TODO: maybe change this?
				rearThrust.Enabled = false;
				frontThrust.Enabled = false;
				rightThrust.Enabled = false;
				leftThrust.Enabled = false;
				rightTurret.Enabled = true;
				leftTurret.Enabled = true;
				readyToCharge = false;
				readyToLeap = false;

				((IMyTerminalBlock)rightInnerHip).SetValue<bool>("ShareInertiaTensor", true);
				((IMyTerminalBlock)leftInnerHip).SetValue<bool>("ShareInertiaTensor", true);

				//TODO: move frame resets to a generic state changing funtion.
				frame = 0;
				leftFrame = legFrames.Length;
				targetRightHipAngle = standingHip;
				targetLeftHipAngle = -standingHip;

				targetRightKneeAngle = standingKnee;
				targetLeftKneeAngle = -standingKnee;

				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;

				rightWing.TargetVelocityRPM = -60;
				leftWing.TargetVelocityRPM = 60;

				if ((Math.Abs(gravityVec.X) < 0.5) && (Math.Abs(gravityVec.Z) < 0.5))
				{
					if ((Math.Abs(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightInnerHip.Angle) + MathHelper.ToDegrees(rightOuterHip.Angle))) < 0.1) && (Math.Abs(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightInnerKnee.Angle) + MathHelper.ToDegrees(rightOuterKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle))) < 0.1))
					{
						rightMags.ForEach(item => item.Lock());
					}
					else
					{
						rightMags.ForEach(item => item.Unlock());
					}
					if ((Math.Abs(correctError(targetLeftHipAngle, MathHelper.ToDegrees(leftInnerHip.Angle) + MathHelper.ToDegrees(leftOuterHip.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftKneeAngle, MathHelper.ToDegrees(leftInnerKnee.Angle) + MathHelper.ToDegrees(leftOuterKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle))) < 0.1))
					{
						leftMags.ForEach(item => item.Lock());
					}
					else
					{
						leftMags.ForEach(item => item.Unlock());
					}
				}
				else
				{
					rightMags.ForEach(item => item.Unlock());
					leftMags.ForEach(item => item.Unlock());
				}

			}
			else if (currentState == State.Charge)
			{
				rightTurret.Enabled = false;
				leftTurret.Enabled = false;
				readyToLeap = false;

				rightMags.ForEach(item => item.Unlock());
				leftMags.ForEach(item => item.Unlock());

				targetRightHipAngle = 179;
				targetLeftHipAngle = -targetRightHipAngle;

				targetRightKneeAngle = -187;
				targetLeftKneeAngle = -targetRightKneeAngle;

				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;

				if (!readyToCharge && rightMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && leftMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && (Math.Abs(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightInnerHip.Angle) + MathHelper.ToDegrees(rightOuterHip.Angle))) < 0.1) && (Math.Abs(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightInnerKnee.Angle) + MathHelper.ToDegrees(rightOuterKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftHipAngle, MathHelper.ToDegrees(leftInnerHip.Angle) + MathHelper.ToDegrees(leftOuterHip.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftKneeAngle, MathHelper.ToDegrees(leftInnerKnee.Angle) + MathHelper.ToDegrees(leftOuterKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle))) < 0.1))
				{
					readyToCharge = true;
					rightMags.ForEach(item => item.Lock());
					leftMags.ForEach(item => item.Lock());
				}
				else if (readyToCharge)
				{
					rightMags.ForEach(item => item.Unlock());
					leftMags.ForEach(item => item.Unlock());
					// TODO: maybe increase initial thrust (more acceleration) then shorten the times back to 56 & 92?
					if (3 <= frame && frame < 168)
					{
						rightWing.TargetVelocityRPM = 60;
						leftWing.TargetVelocityRPM = -60;
						if (40 <= frame)
						{
							rearThrust.Enabled = true;
							rightThrust.Enabled = true;
							leftThrust.Enabled = true;
							rearThrust.ThrustOverridePercentage = 1;
							rightThrust.ThrustOverridePercentage = (float)0.5;
							leftThrust.ThrustOverridePercentage = (float)0.5;
						}
					}
					// Now uses dampeners?
					else if (168 <= frame && frame < 336)
					{
						//TODO: modify this so that if the lobster is detecting an enemy and spins around, the forward thruster doesnt just throw it further back to explode
						//or even make it so the thrust time and strength is relative to the target?
						// using the sensor: is the target moving away as you move? are you facing the target? if you're moving away and facing the target, the rear thrusters stay on!

						rightWing.TargetVelocityRPM = -60;
						leftWing.TargetVelocityRPM = 60;
						//rearThrust.Enabled = false;
						//rightThrust.Enabled = false;
						//leftThrust.Enabled = false;
						rearThrust.ThrustOverridePercentage = 0;
						rightThrust.ThrustOverridePercentage = 0;
						leftThrust.ThrustOverridePercentage = 0;

						frontThrust.Enabled = true;
						frontThrust.ThrustOverridePercentage = 0;
						remoteControl.DampenersOverride = true;
					}
					else if (frame >= 336)
					{
						//frontThrust.ThrustOverridePercentage = 0;
						remoteControl.DampenersOverride = false;
						currentState = State.Stand;
					}

					frame++;
				}
				else
				{
					rearThrust.Enabled = false;
					frontThrust.Enabled = false;
					rightThrust.Enabled = false;
					leftThrust.Enabled = false;
					//TODO: move frame resets to a generic state changing funtion.
					frame = 0;
					leftFrame = legFrames.Length;
				}
			}
			else if (currentState == State.Leap)
			{
				//TODO: make leap power a factor of the effective gravity magnitude
				//TODO: test if inertial tensors have an effect on leaping...
				rightTurret.Enabled = false;
				leftTurret.Enabled = false;
				readyToCharge = false;

				rightMags.ForEach(item => item.Unlock());
				leftMags.ForEach(item => item.Unlock());

				targetRightHipAngle = -84;
				targetLeftHipAngle = -targetRightHipAngle;

				targetRightKneeAngle = 146;
				targetLeftKneeAngle = -targetRightKneeAngle;

				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;

				if (!readyToLeap && rightMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && leftMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && (Math.Abs(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightInnerHip.Angle) + MathHelper.ToDegrees(rightOuterHip.Angle))) < 0.1) && (Math.Abs(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightInnerKnee.Angle) + MathHelper.ToDegrees(rightOuterKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftHipAngle, MathHelper.ToDegrees(leftInnerHip.Angle) + MathHelper.ToDegrees(leftOuterHip.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftKneeAngle, MathHelper.ToDegrees(leftInnerKnee.Angle) + MathHelper.ToDegrees(leftOuterKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle))) < 0.1))
				{
					readyToLeap = true;
					rightMags.ForEach(item => item.Lock());
					leftMags.ForEach(item => item.Lock());
				}
				else if (readyToLeap)
				{
					rightMags.ForEach(item => item.Unlock());
					leftMags.ForEach(item => item.Unlock());

					//TODO: increase leaping speed in the new paradigm? for some reason it seems at about half the original speed...

					targetRightHipAngle = standingHip;
					targetLeftHipAngle = -standingHip;

					targetRightKneeAngle = standingKnee;
					targetLeftKneeAngle = -standingKnee;

					frontThrust.Enabled = true;
					rightThrust.Enabled = true;
					leftThrust.Enabled = true;

					if (frame < 150)
					{
						//Approximately this in 1g agrav:
						//2000 / remoteControl.CalculateShipMass().PhysicalMass;
						frontThrust.ThrustOverridePercentage = 1;
						rightThrust.ThrustOverridePercentage = 1;
						leftThrust.ThrustOverridePercentage = 1;

						targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle - 10;
						targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle + 10;
					}
					else if (frame >= 150)
					{
						frontThrust.ThrustOverridePercentage = 0;
						rightThrust.ThrustOverridePercentage = 0;
						leftThrust.ThrustOverridePercentage = 0;
						remoteControl.DampenersOverride = true;

						targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
						targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;

						if (rightMags.Any(i => i.LockMode == LandingGearMode.ReadyToLock) || leftMags.Any(i => i.LockMode == LandingGearMode.ReadyToLock))
						{
							rightMags.ForEach(item => item.Lock());
							leftMags.ForEach(item => item.Lock());
							remoteControl.DampenersOverride = false;
							currentState = State.Stand;
						}
					}

					frame++;
				}
				else
				{
					frame = 0;
					leftFrame = legFrames.Length;
				}
			}

			Echo("Hip Target");
			Echo(targetRightHipAngle.ToString());
			Echo("Knee Target");
			Echo(targetRightKneeAngle.ToString());
			Echo("Ankle Target");
			Echo(targetRightAnkleAngle.ToString());
			Echo("Hip Current");
			Echo(((MathHelper.ToDegrees(rightInnerHip.Angle) + MathHelper.ToDegrees(rightOuterHip.Angle))%360).ToString());
			Echo("Hip Error");
			//Echo((targetRightHipAngle - MathHelper.ToDegrees(rightHip.Angle)).ToString());
			//Echo("Corrected Hip Error");
			Echo(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightInnerHip.Angle) + MathHelper.ToDegrees(rightOuterHip.Angle)).ToString());
			Echo("Knee Error");
			//Echo((targetRightKneeAngle - MathHelper.ToDegrees(rightKnee.Angle)).ToString());
			//Echo("Corrected Knee Error");
			Echo(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightInnerKnee.Angle) + MathHelper.ToDegrees(rightOuterKnee.Angle)).ToString());

			//TODO: build the newest lobster legs, add second knee rotors to code, then make THIS section halve the target knee angles - won't need to change values anywhere else, then

			rightInnerHip.TargetVelocityRPM = (float)rightInnerHipPid.Control(correctError(targetRightHipAngle/2, MathHelper.ToDegrees(rightInnerHip.Angle)));
			rightOuterHip.TargetVelocityRPM = (float)rightOuterHipPid.Control(correctError(targetRightHipAngle/2, MathHelper.ToDegrees(rightOuterHip.Angle)));
			rightInnerKnee.TargetVelocityRPM = (float)rightInnerKneePid.Control(correctError(targetRightKneeAngle/2, MathHelper.ToDegrees(rightInnerKnee.Angle)));
			rightOuterKnee.TargetVelocityRPM = (float)rightOuterKneePid.Control(correctError(targetRightKneeAngle/2, MathHelper.ToDegrees(rightOuterKnee.Angle)));
			rightAnkle.TargetVelocityRPM = (float)rightAnklePid.Control(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle)));
			leftInnerHip.TargetVelocityRPM = (float)leftInnerHipPid.Control(correctError(targetLeftHipAngle/2, MathHelper.ToDegrees(leftInnerHip.Angle)));
			leftOuterHip.TargetVelocityRPM = (float)leftOuterHipPid.Control(correctError(targetLeftHipAngle/2, MathHelper.ToDegrees(leftOuterHip.Angle)));
			leftInnerKnee.TargetVelocityRPM = (float)leftInnerKneePid.Control(correctError(targetLeftKneeAngle/2, MathHelper.ToDegrees(leftInnerKnee.Angle)));
			leftOuterKnee.TargetVelocityRPM = (float)leftOuterKneePid.Control(correctError(targetLeftKneeAngle/2, MathHelper.ToDegrees(leftOuterKnee.Angle)));
			leftAnkle.TargetVelocityRPM = (float)leftAnklePid.Control(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle)));
		}

		public double correctError(double target, double current)
		{
			double error = target - current;
			error = error - (360 * Math.Round(error / 360));
			return error;
		}
	}
}
