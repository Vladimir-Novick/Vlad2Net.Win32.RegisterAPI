using System;

namespace Vlad2Net.Win32.RegisterAPI
{
	[Flags]
	internal
	enum RegistryValueOptions {
		None,
		DoNotExpandEnvironmentNames,
	}
}
