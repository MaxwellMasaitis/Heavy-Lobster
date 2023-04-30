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

		IMyMotorStator rightWing, rightHip, rightKnee, rightAnkle, rightWrist, leftWing, leftHip, leftKnee, leftAnkle, leftWrist;

		double targetRightHipAngle, targetRightKneeAngle, targetRightAnkleAngle, targetLeftHipAngle, targetLeftKneeAngle, targetLeftAnkleAngle;
		int count, frame, leftFrame;
		bool readyToCharge, readyToLeap;

		const double TimeStep = 1.0 / 60.0, pComp = 2, iComp = 0, dComp = 0;
		PID rightHipPid, rightKneePid, rightAnklePid, rightElevPid, rightAzPid, leftHipPid, leftKneePid, leftAnklePid, rollPid, pitchPid, yawPid, leftElevPid, leftAzPid;
		const int frameDivisor = 2;
		const float initialHip = 3, initialKnee = 17, degreesPerFrame = (float)5.625 * frameDivisor;

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
			rightHip = GridTerminalSystem.GetBlockWithName("Lobster Hip Rotor Right") as IMyMotorStator;
			rightKnee = GridTerminalSystem.GetBlockWithName("Lobster Knee Rotor Right") as IMyMotorStator;
			rightAnkle = GridTerminalSystem.GetBlockWithName("Lobster Ankle Rotor Right") as IMyMotorStator;
			rightHipPid = new PID(pComp, iComp, dComp, TimeStep);
			rightKneePid = new PID(pComp, iComp, dComp, TimeStep);
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
			leftHip = GridTerminalSystem.GetBlockWithName("Lobster Hip Rotor Left") as IMyMotorStator;
			leftKnee = GridTerminalSystem.GetBlockWithName("Lobster Knee Rotor Left") as IMyMotorStator;
			leftAnkle = GridTerminalSystem.GetBlockWithName("Lobster Ankle Rotor Left") as IMyMotorStator;
			leftHipPid = new PID(pComp, iComp, dComp, TimeStep);
			leftKneePid = new PID(pComp, iComp, dComp, TimeStep);
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
			count = 0;
			frame = 0;
			leftFrame = 16;

			readyToCharge = false;
			readyToLeap = false;

			rollPid = new PID(10, 0, 0, TimeStep);
			pitchPid = new PID(10, 0, 0, TimeStep);
			//recheck this - 8 0 9 was working ok in space, but it doesnt seem any more effective on land than 10 0 0
			yawPid = new PID(10, 0, 0, TimeStep);
		}

		public void SetState()
		{
			Enum.TryParse(_commandLine.Argument(1), out currentState);
		}

		//TODO: save current state and frame/count data?

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

				leftFrame = frame + 16;
				if (leftFrame > 31)
				{
					leftFrame -= 32;
				}

				Echo("Frame");
				Echo(frame.ToString());

				targetRightHipAngle = initialHip + frame * degreesPerFrame;
				targetLeftHipAngle = -(initialHip + leftFrame * degreesPerFrame);

				targetRightKneeAngle = initialKnee - frame * degreesPerFrame;
				targetLeftKneeAngle = -(initialKnee - leftFrame * degreesPerFrame);

				// -10 degrees on frames 8-19
				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				if (8 <= frame && frame <= 19)
				{
					targetRightAnkleAngle = targetRightAnkleAngle - 10;
				}
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;
				if (8 <= leftFrame && leftFrame <= 19)
				{
					targetLeftAnkleAngle = targetLeftAnkleAngle + 10;
				}

				if (7 <= frame && frame <= 19)
				{
					rightMags.ForEach(item => item.Unlock());
				}
				else if (24 <= frame)
				{
					rightMags.ForEach(item => item.Lock());
				}
				if (7 <= leftFrame && leftFrame <= 19)
				{
					leftMags.ForEach(item => item.Unlock());
				}
				else if (24 <= leftFrame)
				{
					leftMags.ForEach(item => item.Lock());
				}

				if (frame == 10 || leftFrame == 10)
				{
					rightWing.TargetVelocityRPM = 60;
					leftWing.TargetVelocityRPM = -60;
				}
				else if (frame == 14 || leftFrame == 14)
				{
					rightWing.TargetVelocityRPM = -60;
					leftWing.TargetVelocityRPM = 60;
				}

				count += 1;
				if (count > frameDivisor)
				{
					count = 0;
					frame += 1;
					if (frame > 31)
					{
						frame = 0;
					}
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

				//TODO: move frame resets to a generic state changing funtion.
				count = 0;
				frame = 0;
				leftFrame = 16;
				targetRightHipAngle = initialHip;
				targetLeftHipAngle = -initialHip;

				targetRightKneeAngle = initialKnee;
				targetLeftKneeAngle = -initialKnee;

				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;

				rightWing.TargetVelocityRPM = -60;
				leftWing.TargetVelocityRPM = 60;

				if ((Math.Abs(gravityVec.X) < 0.5) && (Math.Abs(gravityVec.Z) < 0.5))
				{
					if ((Math.Abs(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightHip.Angle))) < 0.1) && (Math.Abs(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle))) < 0.1))
					{
						rightMags.ForEach(item => item.Lock());
					}
					else
					{
						rightMags.ForEach(item => item.Unlock());
					}
					if ((Math.Abs(correctError(targetLeftHipAngle, MathHelper.ToDegrees(leftHip.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftKneeAngle, MathHelper.ToDegrees(leftKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle))) < 0.1))
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

				targetRightHipAngle = 149;
				targetLeftHipAngle = -149;

				targetRightKneeAngle = -142;
				targetLeftKneeAngle = 142;

				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;

				if (!readyToCharge && rightMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && leftMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && (Math.Abs(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightHip.Angle))) < 0.1) && (Math.Abs(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftHipAngle, MathHelper.ToDegrees(leftHip.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftKneeAngle, MathHelper.ToDegrees(leftKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle))) < 0.1))
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
					if (1 <= frame && frame < 64)
					{
						rightWing.TargetVelocityRPM = 60;
						leftWing.TargetVelocityRPM = -60;
						if (20 <= frame)
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
					else if (64 <= frame && frame < 108)
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
					else if (frame >= 108)
					{
						//frontThrust.ThrustOverridePercentage = 0;
						remoteControl.DampenersOverride = false;
						currentState = State.Stand;
					}

					count += 1;
					if (count > frameDivisor)
					{
						count = 0;
						frame += 1;
					}
				}
				else
				{
					rearThrust.Enabled = false;
					frontThrust.Enabled = false;
					rightThrust.Enabled = false;
					leftThrust.Enabled = false;
					//TODO: move frame resets to a generic state changing funtion.
					count = 0;
					frame = 0;
					leftFrame = 16;
				}
			}
			else if (currentState == State.Leap)
			{
				rightTurret.Enabled = false;
				leftTurret.Enabled = false;
				readyToCharge = false;

				rightMags.ForEach(item => item.Unlock());
				leftMags.ForEach(item => item.Unlock());

				targetRightHipAngle = -139;
				targetLeftHipAngle = 139;

				targetRightKneeAngle = 176;
				targetLeftKneeAngle = -176;

				targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle;
				targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle;

				if (!readyToLeap && rightMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && leftMags.All(i => i.LockMode == LandingGearMode.ReadyToLock) && (Math.Abs(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightHip.Angle))) < 0.1) && (Math.Abs(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftHipAngle, MathHelper.ToDegrees(leftHip.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftKneeAngle, MathHelper.ToDegrees(leftKnee.Angle))) < 0.1) && (Math.Abs(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle))) < 0.1))
				{
					readyToLeap = true;
					rightMags.ForEach(item => item.Lock());
					leftMags.ForEach(item => item.Lock());
				}
				else if (readyToLeap)
				{
					rightMags.ForEach(item => item.Unlock());
					leftMags.ForEach(item => item.Unlock());

					targetRightHipAngle = initialHip;
					targetLeftHipAngle = -initialHip;

					targetRightKneeAngle = initialKnee;
					targetLeftKneeAngle = -initialKnee;

					frontThrust.Enabled = true;
					rightThrust.Enabled = true;
					leftThrust.Enabled = true;

					if (frame < 30)
					{
						frontThrust.ThrustOverridePercentage = 1;
						rightThrust.ThrustOverridePercentage = 1;
						leftThrust.ThrustOverridePercentage = 1;

						targetRightAnkleAngle = targetRightHipAngle + targetRightKneeAngle - 10;
						targetLeftAnkleAngle = targetLeftHipAngle + targetLeftKneeAngle + 10;
					}
					else if (frame >= 30)
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

					count += 1;
					if (count > frameDivisor)
					{
						count = 0;
						frame += 1;
					}
				}
				else
				{
					count = 0;
					frame = 0;
					leftFrame = 16;
				}
			}

			Echo("Hip Target");
			Echo(targetRightHipAngle.ToString());
			Echo("Knee Target");
			Echo(targetRightKneeAngle.ToString());
			Echo("Ankle Target");
			Echo(targetRightAnkleAngle.ToString());
			Echo("Hip Current");
			Echo(MathHelper.ToDegrees(rightHip.Angle).ToString());
			Echo("Hip Error");
			//Echo((targetRightHipAngle - MathHelper.ToDegrees(rightHip.Angle)).ToString());
			//Echo("Corrected Hip Error");
			Echo((correctError(targetRightHipAngle, MathHelper.ToDegrees(rightHip.Angle))).ToString());
			Echo("Knee Error");
			//Echo((targetRightKneeAngle - MathHelper.ToDegrees(rightKnee.Angle)).ToString());
			//Echo("Corrected Knee Error");
			Echo((correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightKnee.Angle))).ToString());

			rightHip.TargetVelocityRPM = (float)rightHipPid.Control(correctError(targetRightHipAngle, MathHelper.ToDegrees(rightHip.Angle)));
			rightKnee.TargetVelocityRPM = (float)rightKneePid.Control(correctError(targetRightKneeAngle, MathHelper.ToDegrees(rightKnee.Angle)));
			rightAnkle.TargetVelocityRPM = (float)rightAnklePid.Control(correctError(targetRightAnkleAngle, MathHelper.ToDegrees(rightAnkle.Angle)));
			leftHip.TargetVelocityRPM = (float)leftHipPid.Control(correctError(targetLeftHipAngle, MathHelper.ToDegrees(leftHip.Angle)));
			leftKnee.TargetVelocityRPM = (float)leftKneePid.Control(correctError(targetLeftKneeAngle, MathHelper.ToDegrees(leftKnee.Angle)));
			leftAnkle.TargetVelocityRPM = (float)leftAnklePid.Control(correctError(targetLeftAnkleAngle, MathHelper.ToDegrees(leftAnkle.Angle)));
		}

		public double correctError(double target, double current)
		{
			double error = target - current;
			error = error - (360 * Math.Round(error / 360));
			return error;
		}

		public class PID
		{
			public double Kp { get; set; } = 0;
			public double Ki { get; set; } = 0;
			public double Kd { get; set; } = 0;
			public double Value { get; private set; }

			double _timeStep = 0;
			double _inverseTimeStep = 0;
			double _errorSum = 0;
			double _lastError = 0;
			bool _firstRun = true;

			public PID(double kp, double ki, double kd, double timeStep)
			{
				Kp = kp;
				Ki = ki;
				Kd = kd;
				_timeStep = timeStep;
				_inverseTimeStep = 1 / _timeStep;
			}

			protected virtual double GetIntegral(double currentError, double errorSum, double timeStep)
			{
				return errorSum + currentError * timeStep;
			}

			public double Control(double error)
			{
				//Compute derivative term
				double errorDerivative = (error - _lastError) * _inverseTimeStep;

				if (_firstRun)
				{
					errorDerivative = 0;
					_firstRun = false;
				}

				//Get error sum
				_errorSum = GetIntegral(error, _errorSum, _timeStep);

				//Store this error as last error
				_lastError = error;

				//Construct output
				Value = Kp * error + Ki * _errorSum + Kd * errorDerivative;
				return Value;
			}

			public double Control(double error, double timeStep)
			{
				if (timeStep != _timeStep)
				{
					_timeStep = timeStep;
					_inverseTimeStep = 1 / _timeStep;
				}
				return Control(error);
			}

			public virtual void Reset()
			{
				_errorSum = 0;
				_lastError = 0;
				_firstRun = true;
			}
		}
	}
}
