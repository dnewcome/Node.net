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

		public void emit( string eventname, params IronJS.BoxedValue[] args ) {
			foreach( object callback in Callbacks[eventname] ) {
				log.Trace( "calling js function callback: " + eventname );
				IronJS.FunctionObject func = ( IronJS.FunctionObject )callback;
				func.Call( this, args );
			}
		}

		public void on( string eventname, IronJS.FunctionObject callback ) {
			log.Trace( "EventEmitter - adding listener: " + eventname );
			// TODO: move listener counts to static field on the emitter
			Server.instance.Listeners++;
			// TODO: listen is not an event .. remove this
			if( Callbacks.ContainsKey( eventname ) ) {
				Callbacks[ eventname ].Add( callback );
			}
			// TODO: I think that node allows any event to be registered, 
			// so it probably isn't correct to throw on unexpected event
			else {
				throw new Exception( "on called for unsupported event" );
			}
		}

		public void removeAllListeners( string eventname ) {
			if( Callbacks.ContainsKey( eventname ) ) {
				// TODO: move listener counts to static field on the emitter
				Server.instance.Listeners -= Callbacks[ eventname ].Count;
				Callbacks[ eventname ].Clear();
			}
			else {
				throw new Exception( "tried to remove invalid event handlers: " + eventname );
			}
		}

	} // class
} // namespace 
