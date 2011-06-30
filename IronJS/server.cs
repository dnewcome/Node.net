/**
* Node.net is a .NET implementation of Node.js and is free 
*	software provided under the MIT license.
*
*	See LICENSE file for full text of the license.
*	Copyright 2010 Dan Newcome.
*/

using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using IronJS;
using IronJS.Native;

namespace Node.Net
{
	public class Server
	{
		// temp hack global used to access event queue
		// by other modules
	   public static Server Instance
	   {
	      get { return instance;}
	   }

	   private static readonly Server instance;

		// number of registered callbacks active
		public int Listeners = 0;

		// dispatch queue and signalling primitive for main event loop
		private readonly ManualResetEvent manualResetEvent = new ManualResetEvent( false );
		private readonly Queue workItems = new Queue();

		// toplevel JS execution context used for all JS code
		private static readonly IronJS.Hosting.CSharp.Context ctx = new IronJS.Hosting.CSharp.Context();

		private static readonly net netObj = new net( ctx.Environment );
		// TODO: re-add after httpserver.cs is converted to IJS 0.2
		// private static http httpObj = new http( ctx.Environment );

      static Server()
      {
         instance = new Server();
      }

		public static void Main() {
			// eval js file given as first commandline arg and 
			// run the event loop - runEventLoop() always blocks, unlike node.js
			// which returns if no more callbacks are registered
			Instance.EvalCommandlineArgument();
			Instance.RunEventLoop();
		}

		private static List<string> requireStack = new List<string>();

      /// <summary>
      /// Implements require() for importing js files/namespaces
      /// </summary>
      /// <param name="file">The literal expression passed to require() in js. Might be absolute or relative path + filename.</param>
      /// <returns>
      /// The contents of the <paramref name="file"/>.
      /// </returns>
		public static CommonObject Require( string file ) {
			// TODO: could possibly break this by doing require('undefined');
			if( String.IsNullOrWhiteSpace( file ) || String.IsNullOrEmpty( file ) || file == "undefined" ) {
				throw new Exception( "invalid file spec passed to require()" );
			}

			// for compiled-in functionality, return existing references.
			// TODO: clean up the global namespace, `http' et al. are directly visible
			if( file == "http" ) {
				// TODO: re-add after httpserver.cs is converted to IJS 0.2
				// return httpObj;
			}
			if( file == "net" ) {
				return netObj;
			}
		
			// otherwise invoke CommonJS search rules
			CommonJS.SetRequireStack( file );
			string filename = Path.GetFileName( file );

			string filepath = CommonJS.FindFile( filename );

         using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
         {
            string code = new StreamReader(fs).ReadToEnd();
            // extra semicolon provided after file contents.. just in case
            code = "var exports = {}; " + code + "; exports;";
            CommonObject retval = ctx.Execute<CommonObject>(code);
            // This should probably be inside a finally so it's always executed? [asbjornu]
            CommonJS.RequireStack.Pop();
            return retval;
         }
		}


      /// <summary>
      /// Threadsafe enqueue function.
      /// </summary>
      /// <param name="item">The item.</param>
		public void QueueWorkItem( object item ) {
			log.Trace( "queueWorkItem(): queuing item: " + ( ( Callback )item ).name );
			log.Trace( "queueWorkItem(): acquiring lock: " + workItems );
			Monitor.Enter( workItems );
			try {
				workItems.Enqueue( item );
				log.Trace( "queueWorkItem(): item queued" );
				manualResetEvent.Set();
				log.Trace( "queueWorkItem(): queue set to runnable" );
			}
			finally {
				Monitor.Exit( workItems );
				log.Trace( "queueWorkItem: released lock" );
			}
		}
      
		public void EvalCommandlineArgument() {
			string[] args = System.Environment.GetCommandLineArgs();
			if( args.Length != 2 ) {
				Console.WriteLine( "usage: node <file.js>" );
				System.Environment.Exit( 1 );
			}
			// string script = readFile( args[1] );
			CommonJS.SetRequireStack( args[ 1 ] );
			SetupContext( args[ 1 ] );
		}

		public void RunEventLoop() {
			while( this.Listeners > 0 ) {
				log.Trace( "event loop running" );
				Callback callback = null;

				// TODO: can we improve locking method?
				Monitor.Enter( workItems );
				try {
					log.Trace( "event loop: acquired lock" );
					callback = ( Callback )workItems.Dequeue();
					if( callback == null ) {
						manualResetEvent.Reset();
					}
				}
				catch( Exception e ) {
					log.Trace( e );
					manualResetEvent.Reset();
				}
				finally {
					Monitor.Exit( workItems );
					log.Trace( "event loop: released lock" );
				}

				if( callback != null ) {
					log.Trace( "event loop: dispatching callback: " + callback.name );
					log.Trace( "event loop: dispatching callback: " + callback.callback );
					log.Trace( "event loop: dispatching args: " + callback.args );
					// TODO: not sure what this callback delegate looks like yet
					callback.callback.Invoke( callback.args );
				}
				log.Trace( "event loop waiting" );
				manualResetEvent.WaitOne();
			} // while
		}

      /// <summary>
      /// Sets up the global context and then executes the file given on within that context.
      /// </summary>
      /// <param name="filename">The name of the file to execute.</param>
		public void SetupContext( string filename ) {
			CommonJS.SetRequireStack( filename );

			// string pathpart = Path.GetDirectoryName( in_filename );
			// path = Directory.GetCurrentDirectory() + pathpart;
			// log.Trace( "path of js file: " + path );

			var emit =
			Utils.createHostFunction<Action<BoxedValue>>(
				ctx.Environment, obj => Console.WriteLine( TypeConverter.ToString( obj ) ));
			ctx.SetGlobal( "puts", emit );

			var require = Utils.createHostFunction<Func<string, CommonObject>>( ctx.Environment, Require );
			ctx.SetGlobal( "require", require );

			// Forms the `net" namespace
			ctx.SetGlobal<CommonObject>( "net", netObj );

			// Forms the `http" namespace
			// TODO: re-add after httpserver.cs is converted to IJS 0.2
			// ctx.SetGlobal<IronJS.CommonObject>( "http", httpObj );

			ctx.ExecuteFile( filename );
		}
	} // class

	// note that this encapsulates callbacks on the .net side. The dispatch queue
	// is a Queue of these.
	// We use IFunction for the js callbacks
	public class Callback
	{
		public string name = "";
		public VarDelegate callback;
		public object[] args;
	}

	public delegate void VarDelegate( params object[] args );
}