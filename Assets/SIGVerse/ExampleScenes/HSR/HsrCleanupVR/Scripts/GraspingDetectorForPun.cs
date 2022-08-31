using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SIGVerse.Common;
using UnityEngine.EventSystems;

#if SIGVERSE_PUN
using Photon.Pun;
#endif

namespace SIGVerse.ExampleScenes.Hsr.HsrCleanupVR
{
	public class GraspingDetectorForPun : GraspingDetector
	{
#if SIGVERSE_PUN

		private PhotonView photonView;

		protected override void Start()
		{
			base.Start();

			this.photonView = this.GetComponent<PhotonView>();
		}

		protected override void Grasp(Rigidbody collidedRigidbody)
		{
			base.Grasp(collidedRigidbody);

			this.photonView.RPC("GraspRPC", RpcTarget.Others, collidedRigidbody.name);
		}

		protected override void Release()
		{
			base.Release();

			this.photonView.RPC("ReleaseRPC", RpcTarget.Others);
		}
#endif
	}
}

