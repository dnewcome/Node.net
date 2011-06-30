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
			}
		}
	}
}
