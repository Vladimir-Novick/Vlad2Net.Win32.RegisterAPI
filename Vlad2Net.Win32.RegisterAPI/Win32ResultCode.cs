//
//
// Authors:
//	Vladimir Novick (v_novick@yahoo.com)
//
// (C) 2003 Vladimir Novick 
// 
//

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Vlad2Net.Win32.RegisterAPI
{
	internal class Win32ResultCode
	{
		public const int Success = 0;
		public const int FileNotFound = 2;
		public const int AccessDenied = 5;
		public const int InvalidParameter = 87;
		public const int MoreData = 234;
		public const int NoMoreEntries = 259;
		public const int MarkedForDeletion = 1018;

	}
}
