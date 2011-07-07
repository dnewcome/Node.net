using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Node.Net;

namespace IronJS_FS_Tests
{
	class Program
	{
		static void Main( string[] args ) {
			Server instance = new Server();
			instance.evalCommandlineArgument( null );
			instance.runEventLoop();			
		}
	}
}
