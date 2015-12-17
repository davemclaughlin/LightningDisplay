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
using System.IO;
using System.Collections;

namespace JDI.Storage
{
	public class IniSettings : ISettingsProvider, IEnumerable, IDisposable
	{
		#region Constructors

		public IniSettings(string filePath)
		{
			this.filePath = filePath;
			this.settings = new Hashtable();
			this.lastErrorMsg = "";
			this.isModified = false;
		}

		public void Dispose()
		{
			this.filePath = null;
			this.settings.Clear();
			this.settings = null;
			this.lastErrorMsg = null;
		}

		#endregion


		#region Properties

		/// <summary>
		/// Indexer gets or sets a value for the section and setting provided. 
		/// </summary>
		/// <param name="section">Section name</param>
		/// <param name="setting">Setting name</param>
		/// <returns></returns>
		public string this[string section, string setting]
		{
			get
			{
				if (this.settings.Contains(section) && ((Hashtable)this.settings[section]).Contains(setting))
				{
					return (string)((Hashtable)this.settings[section])[setting];
				}
				return "";
			}
			set
			{
				if (this.settings.Contains(section))
				{
					if (((Hashtable)this.settings[section]).Contains(setting))
					{
						((Hashtable)this.settings[section])[setting] = value;
					}
					((Hashtable)this.settings[section]).Add(setting, value);
				}
				else
				{
					Hashtable keyvalues = new Hashtable();
					keyvalues.Add(section, value);
					this.settings.Add(section, keyvalues);
				}
				this.isModified = true;
			}
		}

		public bool IsModified
		{
			get { return this.isModified; }
		}

		public string LastErrorMsg
		{
			get
			{
				string temp = this.lastErrorMsg;
				this.lastErrorMsg = "";
				return temp;
			}
		}

		public object SyncRoot
		{
			get { return this.settings.SyncRoot; }
		}

		#endregion


		#region Methods

		public bool ContainsSection(string sectionName)
		{
			return this.settings.Contains(sectionName);
		}

		public bool ContainsSetting(string sectionName, string settingName)
		{
			if (this.settings.Contains(sectionName) && ((Hashtable)this.settings[sectionName]).Contains(settingName))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Load ini file into memory.
		/// </summary>
		/// <returns>True if successful, False if error.</returns>
		public bool LoadSettings()
		{
			this.isModified = false;
			this.lastErrorMsg = "";
			this.Clear();

			// check that file exists
			if (File.Exists(filePath) == false)
			{
				this.lastErrorMsg = "File not found.";
				return false;
			}

			// read in and parse all settings
			StreamReader streamReader = null;
			try
			{
				Hashtable keyvalues = null;
				string line = "";
				string currentSection = "";
				string key = "";
				string value = "";
				int sectStartPos = 0;
				int sectEndPos = 0;
				int keyvalueSepPos = 0;
				using (streamReader = new StreamReader(filePath))
				{
                    do
                    {
                        // get next line
                        // line = streamReader.ReadLine().Trim();
                        line = ReadLineEx(ref streamReader).Trim();

                        if (line != null)
                        {
                            // check for empty line
                            if (line.Length == 0)
                                continue;

                            // check for comment
                            if (line.IndexOf(constCommentChar) == 0)
                                continue;

                            // check for start of new section
                            sectStartPos = line.IndexOf(constSectionStart);
                            sectEndPos = line.IndexOf(constSectionEnd);
                            if (sectStartPos == 0 && sectEndPos == line.Length - 1)
                            {
                                // create new section
                                currentSection = line.Substring(sectStartPos + 1, sectEndPos - 1);
                                keyvalues = new Hashtable();
                                this.settings.Add(currentSection, keyvalues);
                                continue;
                            }

                            // check for valid key-value pair
                            keyvalueSepPos = line.IndexOf(constKeyValueSeparator);
                            if (keyvalueSepPos > 0)
                            {
                                // add key-value pair
                                key = line.Substring(0, keyvalueSepPos).Trim();
                                value = line.Substring(keyvalueSepPos + 1).Trim();
                                keyvalues.Add(key, value);
                            }
                        }
                    }
                    while (line != null); // streamReader.EndOfStream == false);

					// cleanup
					if (streamReader != null)
					{
						streamReader.Close();
						streamReader = null;
					}
				}
			}
			catch (Exception ex)
			{
				this.lastErrorMsg = ex.Message;
				return false;
			}
			finally
			{
				if (streamReader != null)
				{
					streamReader.Close();
					streamReader = null;
				}
			}

			return true;
		}

		/// <summary>
		/// Saves settings to a file.  Will overwrite an existing file.
		/// </summary>
		/// <returns>True if successful, False if error.</returns>
		public bool SaveSettings()
		{
			this.lastErrorMsg = "";

			// save all settings
			Hashtable keyvalues = null;
			StreamWriter streamWriter = null;
			try
			{
				streamWriter = new StreamWriter(filePath, false);
				foreach (DictionaryEntry section in this.settings)
				{
					// write section header
					streamWriter.WriteLine(constSectionStart + (string)section.Key + constSectionEnd);

					// write key-value pairs
					keyvalues = (Hashtable)section.Value;
					foreach (DictionaryEntry keyvaluePair in keyvalues)
					{
						streamWriter.WriteLine((string)keyvaluePair.Key + " " + constKeyValueSeparator + " " + (string)keyvaluePair.Value);
					}
				}
			}
			catch (Exception ex)
			{
				this.lastErrorMsg = ex.Message;
				return false;
			}
			finally
			{
				if (streamWriter != null)
				{
					streamWriter.Flush();
					streamWriter.Close();
					streamWriter = null;
				}
			}

			this.isModified = false;
			return true;
		}

		public string GetSettingValue(string sectionName, string settingName, string defaultValue = "")
		{
			if (this.settings.Contains(sectionName) && ((Hashtable)this.settings[sectionName]).Contains(settingName))
			{
				return (string)((Hashtable)this.settings[sectionName])[settingName];
			}
			return defaultValue;
		}

		public void SetSettingValue(string sectionName, string settingName, string settingValue)
		{
			// check for existing section
			if (this.settings.Contains(sectionName))
			{
				// check for existing setting
				if (((Hashtable)this.settings[sectionName]).Contains(settingName))
				{
					((Hashtable)this.settings[sectionName])[settingName] = settingValue;
				}
				else
				{
					((Hashtable)this.settings[sectionName]).Add(settingName, settingValue);
				}
			}
			else
			{
				// add new section and setting
				Hashtable keyvalues = new Hashtable();
				keyvalues.Add(settingName, settingValue);
				this.settings.Add(sectionName, keyvalues);
			}
			this.isModified = true;
		}

		public void DeleteSetting(string sectionName, string settingName)
		{
			if (this.settings.Contains(sectionName))
			{
				((Hashtable)this.settings[sectionName]).Remove(settingName);
			}
			this.isModified = true;
		}

		public void DeleteSection(string sectionName)
		{
			this.settings.Remove(sectionName);
			this.isModified = true;
		}

		/// <summary>
		/// Removes all entries.
		/// </summary>
		public void Clear()
		{
			foreach (object value in this.settings.Values)
			{
				((Hashtable)value).Clear();
			}
			this.settings.Clear();
			this.isModified = true;
		}

		public ISettingEnumerator GetSettingEnumerator()
		{
			return new SettingEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)this.GetSettingEnumerator();
		}

        public static string ReadLineEx(ref StreamReader sr)
        {
            int newChar = 0;
            int bufLen = 512; // NOTE: the smaller buffer size.
            char[] readLineBuff = new char[bufLen];
            int growSize = 512;
            int curPos = 0;
            while ((newChar = sr.Read()) != -1)
            {
                if (curPos == bufLen)
                {
                    if ((bufLen + growSize) > 0xffff)
                    {
                        throw new Exception();
                    }
                    char[] tempBuf = new char[bufLen + growSize];
                    Array.Copy(readLineBuff, 0, tempBuf, 0, bufLen);
                    readLineBuff = tempBuf;
                    bufLen += growSize;
                }
                readLineBuff[curPos] = (char)newChar;
                if (readLineBuff[curPos] == '\n')
                {
                    return new string(readLineBuff, 0, curPos);
                }
                if (readLineBuff[curPos] == '\r')
                {
                    sr.Read();
                    return new string(readLineBuff, 0, curPos);
                }
                curPos++;
            }

            if (curPos == 0) return null; // Null fix.
            return new string(readLineBuff, 0, curPos);
        }

		#endregion


		#region Member Fields and Constants

		private const char constCommentChar = '*';
		private const char constSectionStart = '[';
		private const char constSectionEnd = ']';
		private const char constKeyValueSeparator = '=';

		private string filePath;
		private Hashtable settings;
		private string lastErrorMsg;
		private bool isModified;

		#endregion



		#region SettingEnumerator Class

		/// <summary>
		/// SettingEnumerator Class
		/// </summary>
		private class SettingEnumerator : ISettingEnumerator, IEnumerator
		{
			private IEnumerator sectionEnumerator;
			private IEnumerator settingEnumerator;
			private bool moveSection;
			private bool showSection;

			public SettingEnumerator(IniSettings settingsCollection)
			{
				this.sectionEnumerator = settingsCollection.settings.GetEnumerator();
				this.settingEnumerator = null;
				this.moveSection = true;
				this.showSection = true;
			}

			object IEnumerator.Current
			{
				get { return this.Current; }
			}

			public string Current
			{
				get
				{
					if (this.showSection == true)
					{
						DictionaryEntry sectionEntry = (DictionaryEntry)this.sectionEnumerator.Current;
						return constSectionStart + (string)sectionEntry.Key + constSectionEnd;
					}
					DictionaryEntry settingEntry = (DictionaryEntry)this.settingEnumerator.Current;
					return (string)settingEntry.Key + " " + constKeyValueSeparator + " " + (string)settingEntry.Value;
				}
			}

			public void Reset()
			{
				this.sectionEnumerator.Reset();
				this.settingEnumerator = null;
				this.moveSection = true;
				this.showSection = true;
			}

			public bool MoveNext()
			{
				// move to next section
				if (this.moveSection == true)
				{
					if (this.sectionEnumerator.MoveNext() == true)
					{
						DictionaryEntry sectionEntry = (DictionaryEntry)this.sectionEnumerator.Current;
						this.settingEnumerator = ((Hashtable)sectionEntry.Value).GetEnumerator();
						this.moveSection = false;
						this.showSection = true;
						return true;
					}

					// reached end of last section
					this.settingEnumerator = null;
					this.moveSection = true;
					this.showSection = true;
					return false;
				}
				
				// we are within a section, move to next setting
				if (this.settingEnumerator.MoveNext() == true)
				{
					this.showSection = false;
					return true;
				}

				// reached end of settings, move to next section
				if (this.sectionEnumerator.MoveNext() == true)
				{
					DictionaryEntry sectionEntry = (DictionaryEntry)this.sectionEnumerator.Current;
					this.settingEnumerator = ((Hashtable)sectionEntry.Value).GetEnumerator();
					this.moveSection = false;
					this.showSection = true;
					return true;
				}

				// reached end of last section
				this.settingEnumerator = null;
				this.moveSection = true;
				this.showSection = true;
				return false;
			}
		}

		#endregion
	}
}
