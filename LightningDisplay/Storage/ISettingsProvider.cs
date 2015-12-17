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


namespace JDI.Storage
{
	public interface ISettingEnumerator
	{
		string Current { get; }

		void Reset();
		bool MoveNext();
	}


	public interface ISettingsProvider
	{
		// Properties
		bool IsModified { get; }
		string LastErrorMsg { get; }
		object SyncRoot { get; }

		// Methods
		bool ContainsSection(string sectionName);
		bool ContainsSetting(string sectionName, string settingName);
		bool LoadSettings();
		bool SaveSettings();
		string GetSettingValue(string sectionName, string settingName, string defaultValue);
		void SetSettingValue(string sectionName, string settingName, string settingValue);
		void DeleteSection(string sectionName);
		void DeleteSetting(string sectionName, string settingName);
		ISettingEnumerator GetSettingEnumerator();
	}
}
