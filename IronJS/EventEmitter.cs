using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Node.Net
{
	public class EventEmitter : IronJS.CommonObject
	{
		public Dictionary<string, ArrayList> Callbacks { get; set; }

		public EventEmitter( IronJS.Environment env ) 
			: base( env, env.Maps.Base, env.Prototypes.Object ) { }
	}
}
