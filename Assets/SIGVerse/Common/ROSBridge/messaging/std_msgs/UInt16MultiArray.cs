// Generated by gencs from std_msgs/UInt16MultiArray.msg
// DO NOT EDIT THIS FILE BY HAND!

using System;
using System.Collections;
using System.Collections.Generic;
using SIGVerse.RosBridge;
using UnityEngine;

using SIGVerse.RosBridge.std_msgs;

namespace SIGVerse.RosBridge 
{
	namespace std_msgs 
	{
		[System.Serializable]
		public class UInt16MultiArray : RosMessage
		{
			public std_msgs.MultiArrayLayout layout;
			public System.Collections.Generic.List<System.UInt16>  data;


			public UInt16MultiArray()
			{
				this.layout = new std_msgs.MultiArrayLayout();
				this.data = new System.Collections.Generic.List<System.UInt16>();
			}

			public UInt16MultiArray(std_msgs.MultiArrayLayout layout, System.Collections.Generic.List<System.UInt16>  data)
			{
				this.layout = layout;
				this.data = data;
			}

			new public static string GetMessageType()
			{
				return "std_msgs/UInt16MultiArray";
			}

			new public static string GetMD5Hash()
			{
				return "52f264f1c973c4b73790d384c6cb4484";
			}
		} // class UInt16MultiArray
	} // namespace std_msgs
} // namespace SIGVerse.ROSBridge

