using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Node.Net
{
	public class log
	{
		public static void Trace( object in_message ) {
			if( Config.TraceLogging ) {
				Console.WriteLine( in_message );
				System.Diagnostics.Trace.WriteLine( in_message );
			}
		}
		public static void StdOut( object in_message ) {
			System.Diagnostics.Debug.WriteLine( in_message );
			Console.WriteLine( in_message );
		}
	}
}
