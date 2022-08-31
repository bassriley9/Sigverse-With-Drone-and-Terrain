﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGVerse.Common;
using UnityEngine.UI;

#if SIGVERSE_PUN
using Photon.Pun;
#endif

namespace SIGVerse.ExampleScenes.Hsr.HsrCleanupVR
{

	public class PunOwnerChangerForObject : MonoBehaviour
	{
#if SIGVERSE_PUN
		public static string[] OwnerTags = new string[] { "Player", "Robot" };

		private PhotonView photonView;

		void Awake()
		{
		}

		void Start()
		{
			this.photonView = this.GetComponent<PhotonView>();

			if(this.photonView == null)
			{
				SIGVerseLogger.Error("There is no PhotonView. GameObject=" + this.name);
			}
		}

		void OnCollisionEnter(Collision collision)
		{
			if(!this.ShouldChangeOwner(collision)) { return; }

			PhotonView ownerPhotonView = collision.transform.root.GetComponent<PhotonView>();

			if (ownerPhotonView == null)
			{
				SIGVerseLogger.Error("Player does not have PhotonView. Player=" + collision.transform.root.name);
				return;
			}

			if(this.photonView.Owner == ownerPhotonView.Owner)
			{
				return;
			}

			this.photonView.TransferOwnership(ownerPhotonView.Owner);
		}

		private bool ShouldChangeOwner(Collision collision)
		{
			foreach (string ownerTags in OwnerTags)
			{
				if (collision.transform.root.tag == ownerTags)
				{
					return true;
				}
			}

			return false;
		}
#endif
	}
}
