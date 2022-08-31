using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.Common;
using UnityEngine.UI;
using SIGVerse.RosBridge;

namespace SIGVerse.ExampleScenes.Hsr
{
	public class HumanAvatarBehaviour : MonoBehaviour, IRosReceivingStringMsgHandler
	{
		public enum PointingStep
		{
			PointTarget,
			WaitForPointingTarget,
			Stay1,
			SpeechPickItUp,
			Stay2,
			PointDestination,
			WaitForPointingDestination,
			Stay3,
			SpeechCleanUp,
			Stay4,
			Return,
			WaitForReturn,
		}

		public string avatarName = "Human";

		public GameObject mainMenu;
		public GameObject rosbridgeScripts;

		//-----------------------------

		private const string MsgTellMe  = "Please tell me";
		private const string MsgPointIt = "Please point it";

		private const string MsgPickItUp = "Pick it up!";
		private const string MsgCleanUp  = "Clean up!";

		private const string TagRobot                 = "Robot";
		private const string TagGraspables            = "Graspable";
		private const string TagDestinationCandidates = "DestinationCandidate";

		private const string JudgeTriggerNameOn = "JudgeTriggerOn";
		private const string JudgeTriggerNameIn = "JudgeTriggerIn";

		private Dictionary<string, bool> receivedMessageMap;

		private GameObject robot;
		private List<GameObject> graspables;
		private List<GameObject> destinationCandidates;

		private GameObject graspingTarget;
		private GameObject destination;

		private Rigidbody graspingTargetRigidbody;

		private PointingStep pointingStep = PointingStep.PointTarget;

		private CleanupAvatarHandController avatarHandController;
		private CleanupAvatarHandController avatarLeftHandController;
		private CleanupAvatarHandController avatarRightHandController;
		
		private string taskMessage;

		private PlacementChecker placementChecker;

		private bool isTaskFinished = false;

		private StepTimer stepTimer;

//		public bool go = false;

		void Awake()
		{
			// Get the robot
			this.robot = GameObject.FindGameObjectWithTag(TagRobot);


			// Get the graspables
			this.graspables = GameObject.FindGameObjectsWithTag(TagGraspables).ToList<GameObject>();

			if (graspables.Count == 0){ throw new Exception("Count of Graspables is zero."); }

			// Check the name conflict of graspables.
			if (this.graspables.Count != (from graspable in this.graspables select graspable.name).Distinct().Count())
			{
				throw new Exception("There is the name conflict of graspable objects.");
			}

			SIGVerseLogger.Info("Count of Graspables = " + this.graspables.Count);


			// Get the destination candidates
			this.destinationCandidates = GameObject.FindGameObjectsWithTag(TagDestinationCandidates).ToList<GameObject>();

			if (this.destinationCandidates.Count == 0){ throw new Exception("Count of DestinationCandidates is zero."); }

			// Check the name conflict of destination candidates.
			if (this.destinationCandidates.Count != (from destinations in this.destinationCandidates select destinations.name).Distinct().Count())
			{
				throw new Exception("There is the name conflict of destination candidates objects.");
			}

			SIGVerseLogger.Info("Count of DestinationCandidates = " + this.destinationCandidates.Count);


			this.graspingTarget = this.graspables           [UnityEngine.Random.Range(0, this.graspables.Count)];
			this.destination    = this.destinationCandidates[UnityEngine.Random.Range(0, this.destinationCandidates.Count)];

			SIGVerseLogger.Info("Grasping target is " + graspingTarget.name);
			SIGVerseLogger.Info("Destination is "     + destination.name);

			this.graspingTargetRigidbody = this.graspingTarget.GetComponentInChildren<Rigidbody>();

			this.avatarLeftHandController  = this.GetComponentsInChildren<CleanupAvatarHandController>().Where(item=>item.handType==CleanupAvatarHandController.HandType.LeftHand) .First();
			this.avatarRightHandController = this.GetComponentsInChildren<CleanupAvatarHandController>().Where(item=>item.handType==CleanupAvatarHandController.HandType.RightHand).First();

			this.avatarHandController = this.avatarLeftHandController; // Set default

			this.stepTimer = new StepTimer();
		}


		// Use this for initialization
		void Start()
		{
			this.receivedMessageMap = new Dictionary<string, bool>();
			this.receivedMessageMap.Add(MsgTellMe, false);
			this.receivedMessageMap.Add(MsgPointIt, false);

			// Add Placement checker to triggers
			Transform judgeTriggerOn = this.destination.transform.Find(JudgeTriggerNameOn);
			Transform judgeTriggerIn = this.destination.transform.Find(JudgeTriggerNameIn);

			if (judgeTriggerOn == null && judgeTriggerIn == null) { throw new Exception("No JudgeTrigger. name=" + this.destination.name); }
			if (judgeTriggerOn != null && judgeTriggerIn != null) { throw new Exception("Too many JudgeTrigger. name=" + this.destination.name); }

			if (judgeTriggerOn != null)
			{
				this.placementChecker = judgeTriggerOn.gameObject.AddComponent<PlacementChecker>();
				this.placementChecker.Initialize(PlacementChecker.JudgeType.On);

				this.taskMessage = this.CreateTaskMessage("on");
			}
			if (judgeTriggerIn != null)
			{
				this.placementChecker = judgeTriggerIn.gameObject.AddComponent<PlacementChecker>();
				this.placementChecker.Initialize(PlacementChecker.JudgeType.In);

				this.taskMessage = this.CreateTaskMessage("in");
			}

			SIGVerseLogger.Info("Task Message:" + this.taskMessage);

			StartCoroutine(this.JudgePlacement());
		}

		private string CreateTaskMessage(string preposition)
		{
			return "Grasp the " + this.graspingTarget.name.Split('#')[0] + ", and put it "+preposition+" the " + this.destination.name.Split('#')[0] + ".";
		}


		// Update is called once per frame
		void Update()
		{
			//if (this.go)
			//{
			//	this.receivedMessageMap[MsgPointIt] = true;

			//	this.go = false;
			//}

			if (this.receivedMessageMap[MsgTellMe])
			{
				StartCoroutine(this.SendMessage(this.taskMessage, 0.0f));

				this.receivedMessageMap[MsgTellMe] = false;
			}

			if (this.receivedMessageMap[MsgPointIt])
			{
				switch (this.pointingStep)
				{
					case PointingStep.PointTarget:
					{
						if (UnityEngine.Random.Range(0, 2) == 0)
						{
							// Left Hand
							this.avatarHandController = this.avatarLeftHandController;
						}
						else
						{
							// Right Hand
							this.avatarHandController = this.avatarRightHandController;
						}

						this.avatarHandController.PointTarget(this.graspingTarget);

						this.pointingStep++;
						break;
					}
					case PointingStep.WaitForPointingTarget:
					{
						if(this.avatarHandController.IsWaiting())
						{
							this.pointingStep++;
						}

						break;
					}
					case PointingStep.Stay1:
					{
						if (this.stepTimer.IsTimePassed((int)this.pointingStep, 3000))
						{
							this.pointingStep++;
						}
						break;
					}
					case PointingStep.SpeechPickItUp:
					{
						StartCoroutine(this.SendMessage(MsgPickItUp, 0.0f));
						this.pointingStep++;
						
						break;
					}
					case PointingStep.Stay2:
					{
						if (this.stepTimer.IsTimePassed((int)this.pointingStep, 3000))
						{
							this.pointingStep++;
						}
						break;
					}
					case PointingStep.PointDestination:
					{
						this.avatarHandController.PointDestination(this.destination);

						this.pointingStep++;
						break;
					}
					case PointingStep.WaitForPointingDestination:
					{
						if (this.avatarHandController.IsWaiting())
						{
							this.pointingStep++;
						}

						break;
					}
					case PointingStep.Stay3:
					{
						if (this.stepTimer.IsTimePassed((int)this.pointingStep, 3000))
						{
							this.pointingStep++;
						}
						break;
					}
					case PointingStep.SpeechCleanUp:
					{
						StartCoroutine(this.SendMessage(MsgCleanUp, 0.0f));
						this.pointingStep++;

						break;
					}
					case PointingStep.Stay4:
					{
						if (this.stepTimer.IsTimePassed((int)this.pointingStep, 3000))
						{
							this.pointingStep++;
						}
						break;
					}
					case PointingStep.Return:
					{
						this.avatarHandController.Return();

						this.pointingStep++;
						break;
					}
					case PointingStep.WaitForReturn:
					{
						if (this.avatarHandController.IsWaiting())
						{
							this.avatarHandController.Wait();

							this.pointingStep = PointingStep.PointTarget;

							this.receivedMessageMap[MsgPointIt] = false;
						}
						break;
					}
				}
			}
		}


		private void SendRosMessage(string message)
		{
			ExecuteEvents.Execute<IRosSendingStringMsgHandler>
			(
				target: this.rosbridgeScripts,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosStringMsg(message)
			);
		}

		private void SendPanelNotice(string message, Color panelColor)
		{
			PanelNoticeStatus noticeStatus = new PanelNoticeStatus(this.avatarName, message, panelColor);

			// For changing the notice of the panel
			ExecuteEvents.Execute<IPanelNoticeHandler>
			(
				target: this.mainMenu,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnPanelNoticeChange(noticeStatus)
			);
		}

		private IEnumerator JudgePlacement()
		{
			while(true)
			{
				yield return new WaitForSeconds(1.0f);

				if(this.IsPlaced())
				{
					StartCoroutine(this.SendMessage("Good Job!", 1.0f));
					StartCoroutine(this.SendMessage("Task Finished!", 3.0f));

					this.isTaskFinished = true;
					break;
				}
			}
		}

		private IEnumerator SendMessage(string message, float waitingTime)
		{
			yield return new WaitForSeconds(waitingTime);

			this.SendRosMessage(message);
			this.SendPanelNotice(message, PanelNoticeStatus.Green);
		}

		private bool IsPlaced()
		{
			if(Time.time <= 0){ return false; }

			if (this.graspingTarget.transform.root == this.robot.transform.root)
			{
				return false;
			}
			else
			{
				return this.placementChecker.IsPlacedNow(this.graspingTargetRigidbody);
			}
		}

		public void OnReceiveRosStringMsg(SIGVerse.RosBridge.std_msgs.String rosMsg)
		{
			if(this.isTaskFinished)
			{
				this.SendRosMessage("Task was finished");
				return;
			}

			if(rosMsg.data == MsgTellMe)
			{
				this.receivedMessageMap[rosMsg.data] = true;
			}
			else if(rosMsg.data == MsgPointIt)
			{
				if(this.receivedMessageMap[MsgPointIt])
				{
					SIGVerseLogger.Warn("Now pointing");
					return;
				}

				this.receivedMessageMap[rosMsg.data] = true;
			}
			else
			{
				SIGVerseLogger.Warn("Received Illegal message : " + rosMsg.data);
				return;
			}
		}
	}
}

