//
//
// Authors:
//	Vladimir Novick (v_novick@yahoo.com)
//
// (C) 2003 Vladimir Novick 
// 
//

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Vlad2Net.Win32.RegisterAPI
{

	internal interface IRegistryApi {
		RegistryKey CreateSubKey (RegistryKey rkey, string keyname);
		RegistryKey OpenSubKey (RegistryKey rkey, string keyname, bool writtable);
		void Flush (RegistryKey rkey);
		void Close (RegistryKey rkey);

		object GetValue (RegistryKey rkey, string name, object default_value, RegistryValueOptions options);
		void SetValue (RegistryKey rkey, string name, object value);

		int SubKeyCount (RegistryKey rkey);
		int ValueCount (RegistryKey rkey);
		
		void DeleteValue (RegistryKey rkey, string value, bool throw_if_missing);
		void DeleteKey (RegistryKey rkey, string keyName, bool throw_if_missing);
		string [] GetSubKeyNames (RegistryKey rkey);
		string [] GetValueNames (RegistryKey rkey);
		string ToString (RegistryKey rkey);
		void SetValue (RegistryKey rkey, string name, object value, RegistryValueKind valueKind);

	}
}

