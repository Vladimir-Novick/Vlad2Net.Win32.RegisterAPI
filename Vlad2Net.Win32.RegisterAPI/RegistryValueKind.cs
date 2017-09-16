//
//
// Authors:
//	Vladimir Novick (v_novick@yahoo.com)
//
// (C) 2003 Vladimir Novick 
// 
//

namespace Vlad2Net.Win32.RegisterAPI
{

	public

	enum RegistryValueKind {
		Unknown,
		String,
		ExpandString,
		Binary,
		DWord,
		MultiString = 7,
		QWord = 11,
	}
}
