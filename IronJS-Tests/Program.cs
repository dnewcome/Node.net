using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronJS_FS_Tests
{
	class Program
	{
		static void Main( string[] args ) {
			Server instance = new Server();
			instance.evalCommandlineArgument();
			instance.runEventLoop();			
		}
	}
}
