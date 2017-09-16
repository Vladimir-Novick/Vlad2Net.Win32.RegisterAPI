//
//
// Authors:
//	Vladimir Novick (v_novick@yahoo.com)
//
// (C) 2003 Vladimir Novick 
// 
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Vlad2Net.Win32.RegisterAPI
{

	internal class Win32RegistryApi : IRegistryApi
	{

		const int OpenRegKeyRead = 0x00020019; 
		const int OpenRegKeyWrite = 0x00020006; 


		const int Int32ByteSize = 4;

		readonly int NativeBytesPerCharacter = Marshal.SystemDefaultCharSize;

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegCreateKey")]
		static extern int RegCreateKey (IntPtr keyBase, string keyName, out IntPtr keyHandle);
	       
		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegCloseKey")]
		static extern int RegCloseKey (IntPtr keyHandle);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegFlushKey")]
		private static extern int RegFlushKey (IntPtr keyHandle);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegOpenKeyEx")]
		private static extern int RegOpenKeyEx (IntPtr keyBase,
				string keyName, IntPtr reserved, int access,
				out IntPtr keyHandle);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegDeleteKey")]
		private static extern int RegDeleteKey (IntPtr keyHandle, string valueName);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegDeleteValue")]
		private static extern int RegDeleteValue (IntPtr keyHandle, string valueName);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegEnumKey")]
		private static extern int RegEnumKey (IntPtr keyBase, int index, StringBuilder nameBuffer, int bufferLength);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegEnumValue")]
		private static extern int RegEnumValue (IntPtr keyBase, 
				int index, StringBuilder nameBuffer, 
				ref int nameLength, IntPtr reserved, 
				ref RegistryValueKind type, IntPtr data, IntPtr dataLength);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegSetValueEx")]
		private static extern int RegSetValueEx (IntPtr keyBase, 
				string valueName, IntPtr reserved, RegistryValueKind type,
				StringBuilder data, int rawDataLength);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegSetValueEx")]
		private static extern int RegSetValueEx (IntPtr keyBase, 
				string valueName, IntPtr reserved, RegistryValueKind type,
				string data, int rawDataLength);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegSetValueEx")]
		private static extern int RegSetValueEx (IntPtr keyBase, 
				string valueName, IntPtr reserved, RegistryValueKind type,
				byte[] rawData, int rawDataLength);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegSetValueEx")]
		private static extern int RegSetValueEx (IntPtr keyBase, 
				string valueName, IntPtr reserved, RegistryValueKind type,
				ref int data, int rawDataLength);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegQueryValueEx")]
		private static extern int RegQueryValueEx (IntPtr keyBase,
				string valueName, IntPtr reserved, ref RegistryValueKind type,
				IntPtr zero, ref int dataSize);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegQueryValueEx")]
		private static extern int RegQueryValueEx (IntPtr keyBase,
				string valueName, IntPtr reserved, ref RegistryValueKind type,
				[Out] byte[] data, ref int dataSize);

		[DllImport ("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="RegQueryValueEx")]
		private static extern int RegQueryValueEx (IntPtr keyBase,
				string valueName, IntPtr reserved, ref RegistryValueKind type,
				ref int data, ref int dataSize);


		static IntPtr GetHandle (RegistryKey key)
		{
			return key.IsRoot ? new IntPtr ((int) key.Data)
				: (IntPtr) key.Data;
		}

		static bool IsHandleValid (RegistryKey key)
		{
			return key.Data != null;
		}


		public object GetValue (RegistryKey rkey, string name, object defaultValue, RegistryValueOptions options)
		{
			RegistryValueKind type = 0;
			int size = 0;
			object obj = null;
			IntPtr handle = GetHandle (rkey);
			int result = RegQueryValueEx (handle, name, IntPtr.Zero, ref type, IntPtr.Zero, ref size);

			if (result == Win32ResultCode.FileNotFound || result == Win32ResultCode.MarkedForDeletion) {
				return defaultValue;
			}
			
			if (result != Win32ResultCode.MoreData && result != Win32ResultCode.Success ) {
				GenerateException (result);
			}
			
			if (type == RegistryValueKind.String) {
				byte[] data;
				result = GetBinaryValue (rkey, name, type, out data, size);
				obj = RegistryKey.DecodeString (data);
			} else if (type == RegistryValueKind.ExpandString) {
				byte [] data;
				result = GetBinaryValue (rkey, name, type, out data, size);
				obj = RegistryKey.DecodeString (data);
				if ((options & RegistryValueOptions.DoNotExpandEnvironmentNames) == 0)
					obj = Environment.ExpandEnvironmentVariables ((string) obj);
			} else if (type == RegistryValueKind.DWord) {
				int data = 0;
				result = RegQueryValueEx (handle, name, IntPtr.Zero, ref type, ref data, ref size);
				obj = data;
			} else if (type == RegistryValueKind.Binary) {
				byte[] data;
				result = GetBinaryValue (rkey, name, type, out data, size);
				obj = data;
			} else if (type == RegistryValueKind.MultiString) {
				obj = null;
				byte[] data;
				result = GetBinaryValue (rkey, name, type, out data, size);
				
				if (result == Win32ResultCode.Success)
					obj = RegistryKey.DecodeString (data).Split ('\0');
			} else {
				throw new SystemException ();
			}

			if (result != Win32ResultCode.Success)
			{
				GenerateException (result);
			}
			

			return obj;
		}
	
		public void SetValue (RegistryKey rkey, string name, object value)
		{
			Type type = value.GetType ();
			int result;
			IntPtr handle = GetHandle (rkey);

			if (type == typeof (int)) {
				int rawValue = (int)value;
				result = RegSetValueEx (handle, name, IntPtr.Zero, RegistryValueKind.DWord, ref rawValue, Int32ByteSize); 
			} else if (type == typeof (byte[])) {
				byte[] rawValue = (byte[]) value;
				result = RegSetValueEx (handle, name, IntPtr.Zero, RegistryValueKind.Binary, rawValue, rawValue.Length);
			} else if (type == typeof (string[])) {
				string[] vals = (string[]) value;
				StringBuilder fullStringValue = new StringBuilder ();
				foreach (string v in vals)
				{
					fullStringValue.Append (v);
					fullStringValue.Append ('\0');
				}
				fullStringValue.Append ('\0');

				byte[] rawValue = Encoding.Unicode.GetBytes (fullStringValue.ToString ());
			
				result = RegSetValueEx (handle, name, IntPtr.Zero, RegistryValueKind.MultiString, rawValue, rawValue.Length); 
			} else if (type.IsArray) {
				throw new ArgumentException ("Only string and byte arrays can written as registry values");
			} else {
				string rawValue = String.Format ("{0}{1}", value, '\0');
				result = RegSetValueEx (handle, name, IntPtr.Zero, RegistryValueKind.String, rawValue,
							rawValue.Length * NativeBytesPerCharacter);
			}

			if (result == Win32ResultCode.MarkedForDeletion)
				throw RegistryKey.CreateMarkedForDeletionException ();

			// handle the result codes
			if (result != Win32ResultCode.Success)
			{
				GenerateException (result);
			}
		}

		/// <summary>
		///	Get a binary value.
		/// </summary>
		private int GetBinaryValue (RegistryKey rkey, string name, RegistryValueKind type, out byte[] data, int size)
		{
			byte[] internalData = new byte [size];
			IntPtr handle = GetHandle (rkey);
			int result = RegQueryValueEx (handle, name, IntPtr.Zero, ref type, internalData, ref size);
			data = internalData;
			return result;
		}

		
		const int BufferMaxLength = 1024;
		
		public int SubKeyCount (RegistryKey rkey)
		{
			int index;
			StringBuilder stringBuffer = new StringBuilder (BufferMaxLength);
			IntPtr handle = GetHandle (rkey);
			
			for (index = 0; true; index ++) {
				int result = RegEnumKey (handle, index, stringBuffer,
					stringBuffer.Capacity);

				if (result == Win32ResultCode.MarkedForDeletion)
					throw RegistryKey.CreateMarkedForDeletionException ();

				if (result == Win32ResultCode.Success)
					continue;
				
				if (result == Win32ResultCode.NoMoreEntries)
					break;
				
				// something is wrong!!
				GenerateException (result);
			}
			return index;
		}

		public int ValueCount (RegistryKey rkey)
		{
			int index, result, bufferCapacity;
			RegistryValueKind type;
			StringBuilder buffer = new StringBuilder (BufferMaxLength);
			
			IntPtr handle = GetHandle (rkey);
			for (index = 0; true; index ++) {
				type = 0;
				bufferCapacity = buffer.Capacity;
				result = RegEnumValue (handle, index, 
						       buffer, ref bufferCapacity,
						       IntPtr.Zero, ref type, 
						       IntPtr.Zero, IntPtr.Zero);

				if (result == Win32ResultCode.MarkedForDeletion)
					throw RegistryKey.CreateMarkedForDeletionException ();

				if (result == Win32ResultCode.Success || result == Win32ResultCode.MoreData)
					continue;
				
				if (result == Win32ResultCode.NoMoreEntries)
					break;
				
				// something is wrong
				GenerateException (result);
			}
			return index;
		}
		
		public RegistryKey OpenSubKey (RegistryKey rkey, string keyName, bool writable)
		{
			int access = OpenRegKeyRead;
			if (writable) access |= OpenRegKeyWrite;
			IntPtr handle = GetHandle (rkey);
			
			IntPtr subKeyHandle;
			int result = RegOpenKeyEx (handle, keyName, IntPtr.Zero, access, out subKeyHandle);

			if (result == Win32ResultCode.FileNotFound || result == Win32ResultCode.MarkedForDeletion)
				return null;
			
			if (result != Win32ResultCode.Success)
				GenerateException (result);
			
			return new RegistryKey (subKeyHandle, CombineName (rkey, keyName), writable);
		}

		public void Flush (RegistryKey rkey)
		{
			if (!IsHandleValid (rkey))
				return;
			IntPtr handle = GetHandle (rkey);
			RegFlushKey (handle);
		}

		public void Close (RegistryKey rkey)
		{
			if (!IsHandleValid (rkey))
				return;
			IntPtr handle = GetHandle (rkey);
			RegCloseKey (handle);
		}

		public RegistryKey CreateSubKey (RegistryKey rkey, string keyName)
		{
			IntPtr handle = GetHandle (rkey);
			IntPtr subKeyHandle;
			int result = RegCreateKey (handle , keyName, out subKeyHandle);

			if (result == Win32ResultCode.MarkedForDeletion)
				throw RegistryKey.CreateMarkedForDeletionException ();

			if (result != Win32ResultCode.Success) {
				GenerateException (result);
			}
			
			return new RegistryKey (subKeyHandle, CombineName (rkey, keyName),
				true);
		}

		public void DeleteKey (RegistryKey rkey, string keyName, bool shouldThrowWhenKeyMissing)
		{
			IntPtr handle = GetHandle (rkey);
			int result = RegDeleteKey (handle, keyName);

			if (result == Win32ResultCode.FileNotFound) {
				if (shouldThrowWhenKeyMissing)
					throw new ArgumentException ("key " + keyName);
				return;
			}
			
			if (result != Win32ResultCode.Success)
				GenerateException (result);
		}

		public void DeleteValue (RegistryKey rkey, string value, bool shouldThrowWhenKeyMissing)
		{
			IntPtr handle = GetHandle (rkey);
			int result = RegDeleteValue (handle, value);

			if (result == Win32ResultCode.MarkedForDeletion)
				return;

			if (result == Win32ResultCode.FileNotFound){
				if (shouldThrowWhenKeyMissing)
					throw new ArgumentException ("value " + value);
				return;
			}
			
			if (result != Win32ResultCode.Success)
				GenerateException (result);
		}

		public string [] GetSubKeyNames (RegistryKey rkey)
		{
			IntPtr handle = GetHandle (rkey);
			StringBuilder buffer = new StringBuilder (BufferMaxLength);
			ArrayList keys = new ArrayList ();
				
			for (int index = 0; true; index ++) {
				int result = RegEnumKey (handle, index, buffer, buffer.Capacity);

				if (result == Win32ResultCode.Success) {
					keys.Add (buffer.ToString ());
					buffer.Length = 0;
					continue;
				}

				if (result == Win32ResultCode.NoMoreEntries)
					break;

				// should not be here!
				GenerateException (result);
			}
			return (string []) keys.ToArray (typeof(String));
		}


		public string [] GetValueNames (RegistryKey rkey)
		{
			IntPtr handle = GetHandle (rkey);
			ArrayList values = new ArrayList ();
			
			for (int index = 0; true; index ++)
			{
				StringBuilder buffer = new StringBuilder (BufferMaxLength);
				int bufferCapacity = buffer.Capacity;
				RegistryValueKind type = 0;
				
				int result = RegEnumValue (handle, index, buffer, ref bufferCapacity,
							IntPtr.Zero, ref type, IntPtr.Zero, IntPtr.Zero);

				if (result == Win32ResultCode.Success || result == Win32ResultCode.MoreData) {
					values.Add (buffer.ToString ());
					continue;
				}
				
				if (result == Win32ResultCode.NoMoreEntries)
					break;

				if (result == Win32ResultCode.MarkedForDeletion)
					throw RegistryKey.CreateMarkedForDeletionException ();

				GenerateException (result);
			}

			return (string []) values.ToArray (typeof(String));
		}

		/// <summary>
		/// convert a win32 error code into an appropriate exception.
		/// </summary>
		private void GenerateException (int errorCode)
		{
			switch (errorCode) {
				case Win32ResultCode.FileNotFound:
				case Win32ResultCode.InvalidParameter:
					throw new ArgumentException ();
				
				case Win32ResultCode.AccessDenied:
					throw new SecurityException ();

				default:
					// unidentified system exception
					throw new SystemException ();
			}
		}

		public string ToString (RegistryKey rkey)
		{
			IntPtr handle = GetHandle (rkey);
			
			return String.Format ("{0} [0x{1:X}]", rkey.Name, handle.ToInt32 ());
		}

		/// <summary>
		///	utility: Combine the sub key name to the current name to produce a 
		///	fully qualified sub key name.
		/// </summary>
		internal static string CombineName (RegistryKey rkey, string localName)
		{
			return String.Concat (rkey.Name, "\\", localName);
		}
	}
}

