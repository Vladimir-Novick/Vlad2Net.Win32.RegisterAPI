//
//
// Authors:
//	Vladimir Novick (v_novick@yahoo.com)
//
// (C) 2003 Vladimir Novick 
// 
//

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Vlad2Net.Win32.RegisterAPI
{
	/// <summary>
	///	Wrapper class for Windows Registry Entry.
	/// </summary>
	public sealed class RegistryKey : MarshalByRefObject, IDisposable 
	{
		//
		// This represents the backend data, used when creating the
		// RegistryKey object
		//
		internal object Data;
		
		readonly string qname;	// the fully qualified registry key name
		readonly bool isRoot;	// is the an instance of a root key?
		readonly bool isWritable;	// is the key openen in writable mode

		internal bool IsRoot {
			get { return isRoot; }
		}

		internal bool IsWritable {
			get { return isWritable; }
		}
		
		static readonly IRegistryApi RegistryApi;

		static RegistryKey ()
		{
				RegistryApi = new Win32RegistryApi ();
		}

		/// <summary>
		///	Construct an instance of a root registry key entry.
		/// </summary>
        internal RegistryKey(RegistryHive hiveId, string keyName)
        {
			Data = hiveId;
			qname = keyName;
			isRoot = true;
			isWritable = true; // always consider root writable
		}
		
		/// <summary>
		///	Construct an instance of a registry key entry.
		/// </summary>
		internal RegistryKey (object data, string keyName, bool writable)
		{
			Data = data;
			qname = keyName;
			isRoot = false;
			isWritable = writable;
		}

		#region PublicAPI

		/// <summary>
		///	Dispose of registry key object. Close the 
		///	key if it's still open.
		/// </summary>
		void IDisposable.Dispose ()
		{
			GC.SuppressFinalize (this);
			Close ();
		}

		
		/// <summary>
		///	Final cleanup of registry key object. Close the 
		///	key if it's still open.
		/// </summary>
		~RegistryKey ()
		{
			Close ();
		}

		
		/// <summary>
		///	Get the fully qualified registry key name.
		/// </summary>
		public string Name {
			get { return qname; }
		}
	
		
		/// <summary>
		///	Flush the current registry state to disk.
		/// </summary>
		public void Flush()
		{
			RegistryApi.Flush (this);
		}
		
		
		/// <summary>
		///	Close the current registry key and flushes the state of the registry
		/// right away.
		/// </summary>
		public void Close()
		{
			Flush ();

			if (isRoot)
				return;
			
			RegistryApi.Close (this);
			Data = null;
		}
		
		
		/// <summary>
		///	get the number of sub-keys for this key
		/// </summary>
		public int SubKeyCount {
			get {
				AssertKeyStillValid ();

				return RegistryApi.SubKeyCount (this);
			}
		}

		
		/// <summary>
		///	get the number of values for this key
		/// </summary>
		public int ValueCount {
			get {
				AssertKeyStillValid ();

				return RegistryApi.ValueCount (this);
			}
		}

		
		/// <summary>
		///	Set a registry value.
		/// </summary>
		public void SetValue (string name, object value)
		{
			AssertKeyStillValid ();

			if (value == null)
				throw new ArgumentNullException ("value");

			if (!IsWritable)
				throw new UnauthorizedAccessException ("Cannot write to the registry key.");

			RegistryApi.SetValue (this, name, value);
		}


		[ComVisible (false)]
		public void SetValue (string name, object value, RegistryValueKind valueKind)
		{
			AssertKeyStillValid ();
			
			if (value == null)
				throw new ArgumentNullException ();

			if (!IsWritable)
				throw new UnauthorizedAccessException ("Cannot write to the registry key.");

			RegistryApi.SetValue (this, name, value, valueKind);
		}


		/// <summary>
		///	Open the sub key specified, for read access.
		/// </summary>
		public RegistryKey OpenSubKey (string keyName)
		{
			return OpenSubKey (keyName, false);
		}

		
		/// <summary>
		///	Open the sub key specified.
		/// </summary>
		public RegistryKey OpenSubKey (string keyName, bool writtable)
		{
			AssertKeyStillValid ();
			AssertKeyNameNotNull (keyName);

			return RegistryApi.OpenSubKey (this, keyName, writtable);
		}
		
		
		/// <summary>
		///	Get a registry value.
		/// </summary>
		public object GetValue (string name)
		{
			return GetValue (name, null);
		}

		
		/// <summary>
		///	Get a registry value.
		/// </summary>
		public object GetValue (string name, object defaultValue)
		{
			AssertKeyStillValid ();
			
			return RegistryApi.GetValue (this, name, defaultValue,
				RegistryValueOptions.None);
		}



		/// <summary>
		///	Create a sub key.
		/// </summary>
		public RegistryKey CreateSubKey (string subkey)
		{
			AssertKeyStillValid ();
			AssertKeyNameNotNull (subkey);
			if (subkey.Length > 255)
				throw new ArgumentException ("keyName length is larger than 255 characters", subkey);

			if (!IsWritable)
				throw new UnauthorizedAccessException ("Cannot write to the registry key.");
			return RegistryApi.CreateSubKey (this, subkey);
		}
		
		
		/// <summary>
		///	Delete the specified subkey.
		/// </summary>
		public void DeleteSubKey(string subkey)
		{
			DeleteSubKey (subkey, true);
		}
		
		
		/// <summary>
		///	Delete the specified subkey.
		/// </summary>
		public void DeleteSubKey(string subkey, bool throwOnMissingSubKey)
		{
			AssertKeyStillValid ();
			AssertKeyNameNotNull (subkey);

			if (!IsWritable)
				throw new UnauthorizedAccessException ("Cannot write to the registry key.");

			RegistryKey child = OpenSubKey (subkey);
			
			if (child == null) {
				if (throwOnMissingSubKey)
					throw new ArgumentException ("Cannot delete a subkey tree"
						+ " because the subkey does not exist.");
				return;
			}

			if (child.SubKeyCount > 0){
				throw new InvalidOperationException ("Registry key has subkeys"
					+ " and recursive removes are not supported by this method.");
			}
			
			child.Close ();

			RegistryApi.DeleteKey (this, subkey, throwOnMissingSubKey);
		}
		
		
		/// <summary>
		///	Delete a sub tree (node, and values alike).
		/// </summary>
		public void DeleteSubKeyTree(string keyName)
		{
			
			AssertKeyStillValid ();
			AssertKeyNameNotNull (keyName);
			
			RegistryKey child = OpenSubKey (keyName, true);
			if (child == null)
				throw new ArgumentException ("Cannot delete a subkey tree"
					+ " because the subkey does not exist.");

			child.DeleteChildKeysAndValues ();
			child.Close ();
			DeleteSubKey (keyName, false);
		}
		

		/// <summary>
		///	Delete a value from the registry.
		/// </summary>
		public void DeleteValue(string value)
		{
			DeleteValue (value, true);
		}
		
		
		/// <summary>
		///	Delete a value from the registry.
		/// </summary>
		public void DeleteValue(string value, bool shouldThrowWhenKeyMissing)
		{
			AssertKeyStillValid ();
			AssertKeyNameNotNull (value);

			if (!IsWritable)
				throw new UnauthorizedAccessException ("Cannot write to the registry key.");

			RegistryApi.DeleteValue (this, value, shouldThrowWhenKeyMissing);
		}
		
		
		/// <summary>
		///	Get the names of the sub keys.
		/// </summary>
		public string[] GetSubKeyNames()
		{
			AssertKeyStillValid ();

			return RegistryApi.GetSubKeyNames (this);
		}
		
		
		/// <summary>
		///	Get the names of values contained in this key.
		/// </summary>
		public string[] GetValueNames()
		{
			AssertKeyStillValid ();
			return RegistryApi.GetValueNames (this);
		}
		
	
		
		/// <summary>
		///	Build a string representation of the registry key.
		///	Conatins the fully qualified key name, and the Hex
		///	representation of the registry key handle.
		/// </summary>
		public override string ToString()
		{
			return RegistryApi.ToString (this);
		}

		#endregion // PublicAPI

		
		/// <summary>
		/// validate that the registry key handle is still usable.
		/// </summary>
		private void AssertKeyStillValid ()
		{
			if (Data == null)
				throw new ObjectDisposedException ("Vlad2Net.Win32.RegisterAPI");
		}

		
		/// <summary>
		/// validate that the registry key handle is still usable, and
		/// that the 'subKeyName' is not null.
		/// </summary>
		private void AssertKeyNameNotNull (string subKeyName)
		{
			if (subKeyName == null)
				throw new ArgumentNullException ();
		}
		

		/// <summary>
		///	Utility method to delelte a key's sub keys and values.
		///	This method removes a level of indirection when deleting
		///	key node trees.
		/// </summary>
		private void DeleteChildKeysAndValues ()
		{
			if (isRoot)
				return;
			
			string[] subKeys = GetSubKeyNames ();
			foreach (string subKey in subKeys)
			{
				RegistryKey sub = OpenSubKey (subKey, true);
				sub.DeleteChildKeysAndValues ();
				sub.Close ();
				DeleteSubKey (subKey, false);
			}

			string[] values = GetValueNames ();
			foreach (string value in values) {
				DeleteValue (value, false);
			}
		}

		/// <summary>
		///	decode a byte array as a string, and strip trailing nulls
		/// </summary>
		static internal string DecodeString (byte[] data)
		{
			string stringRep = Encoding.Unicode.GetString (data);
			int idx = stringRep.IndexOf ('\0');
			if (idx != -1)
				stringRep = stringRep.TrimEnd ('\0');
			return stringRep;
		}

		static internal IOException CreateMarkedForDeletionException ()
		{
			throw new IOException ("Illegal operation attempted on a"
				+ " registry key that has been marked for deletion.");
		}

	}
}

