/*
	Copyright (c), 2011,2012 JASDev International  http://www.jasdev.com  
	All rights reserved.  
	
	Licensed under the Apache License, Version 2.0 (the "License").
	You may not use this file except in compliance with the License.
	You may obtain a copy of the License at
	
		http://www.apache.org/licenses/LICENSE-2.0
	
	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 */

using System;
using Microsoft.SPOT.IO;
using GHI.IO;

using ghiCard = GHI.IO.Storage.SDCard;

namespace JDI.NETMF.Storage
{
	public static class SDCard
	{
		// Member Fields
		private static GHI.IO.Storage.SDCard ps = null;
		private static bool isMounted = false;
		private static string lastErrorMsg = "";

		// Properties
		public static bool IsPresent
		{
            get { return true; }
		}

		public static bool IsMounted
		{
			get { return isMounted; }
		}

		public static bool IsFormatted
		{
			get { if (isMounted) return VolumeInfo.GetVolumes()[0].IsFormatted; return false; }
		}

		public static string RootDirectory
		{
			get { if (isMounted) return VolumeInfo.GetVolumes()[0].RootDirectory; return ""; }
		}

		public static string LastErrorMsg
		{
			get
			{
				string temp = lastErrorMsg;
				lastErrorMsg = "";
				return temp;
			}
		}

		// Methods
		public static bool MountSD()
		{
			lastErrorMsg = "";
			bool returnValue = false;

			try
			{
                ps = new GHI.IO.Storage.SDCard(GHI.IO.Storage.SDCard.SDInterface.MCI);

                ps.Mount();
				isMounted = true;
				returnValue = true;
			}
			catch (Exception e)
			{
				if (ps != null)
				{
					ps.Dispose();
					ps = null;
				}
				lastErrorMsg = e.Message;
			}

			return returnValue;
		}

		public static void UnMountSD()
		{
			if (ps != null)
			{
				ps.Unmount();
				isMounted = false;
				ps.Dispose();
				ps = null;
			}
		}

	//	public static void FormatSD()
	//	{
	//		VolumeInfo.GetVolumes()[0].Format(0);
	//	}
	}
}
