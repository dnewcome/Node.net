/**
* Node.net is a .NET implementation of Node.js and is free 
*	software provided under the MIT license.
*
*	See LICENSE file for full text of the license.
*	Copyright 2010 Dan Newcome.
*/

using System.Threading;
using System.IO;
using System.Text;
using System.Collections;
using System;
using IronJS.Runtime;
using IronJS.Compiler;
using IronJS.Runtime.Js;
using IronJS.Runtime.Utils;


// TODO: would like to import Node.net API namespaces using require()
// import SetTimeout;
// import Http;
// import Net;

public class Server 
{
	// temp hack global
	public static Server instance;
	
	// dispatch queue and signalling primitive for main event loop
	private ManualResetEvent manualResetEvent = new ManualResetEvent( false );
	private Queue workItems = new Queue();

	public static void Main() {
		// eval js file given as first commandline arg and 
		// run the event loop - runEventLoop() always blocks, unlike node.js
		// which returns if no more callbacks are registered
		instance = new Server();
		instance.evalCommandlineArgument();
		instance.runEventLoop();
	}
	
	/*
	function setTimeout( fn, time ) {
		print( 'registering timeout' );
		var timer = new NodeTimer();
		timer.SetTimeout( fn, time );
	}
	*/

	// implements require() for importing js files/namespaces
	/*
	function require( file ) {
		// for compiled-in functionality, return existing references.
		// TODO: clean up the global namespace, `http' et al. are directly visible
		if( file == 'http' ) {
			return http;
		}
		if( file == 'net' ) {
			return net;
		}
		if( file == 'sys' ) {
			return sys;
		}
		print( 'requiring: ' + file );
		var fileContents = readFile( file + '.js' );
		// extra semicolon provided after file contents.. just in case
		var code = 'var exports = {}; ' + fileContents + '; exports;';
		print( 'evaluating require: ' + code );
		return eval( code, 'unsafe' );
	}
	*/

	// quick hack to add sys.puts();
	// TODO: `sys' should be implemented in its own class
	// var sys = { puts: function( string ){ print( string ); } };

	// threadsafe enqueue function
	public void queueWorkItem( object item ) {
			Console.WriteLine( "queueWorkItem(): queuing item: " + item );
			Console.WriteLine( "queueWorkItem(): acquiring lock: " + workItems );
		Monitor.Enter( workItems );
		try {
			workItems.Enqueue( item );
			Console.WriteLine( "queueWorkItem(): item queued" );
			manualResetEvent.Set();
		}
		finally {
			Monitor.Exit( workItems );
			Console.WriteLine( "queueWorkItem: released lock" );
		}
	}

	private string readFile( string filename ) {
		string fileContents;
		StreamReader streamReader = new StreamReader( filename );
		try {
			fileContents = streamReader.ReadToEnd();
			return fileContents;
		}
		finally {
			streamReader.Close();
		}
	}

	public void evalCommandlineArgument() {
		String[] args = Environment.GetCommandLineArgs();
		if( args.Length != 2 ) {
			Console.WriteLine( "usage: node <file.js>" );
			Environment.Exit(1);
		}
		// string script = readFile( args[1] );
		ReadJsFile( args[1] );
	}

	public void runEventLoop() {
		while( true ) {
			Console.WriteLine( "event loop running" );
			Callback callback = null;
			
			// TODO: can we improve locking method?
			Monitor.Enter( workItems );
			try {
				Console.WriteLine( "event loop: acquired lock" );
				callback = ( Callback )workItems.Dequeue();
				if( callback == null ) {
					manualResetEvent.Reset();
				}
			}
			finally {
				Monitor.Exit( workItems );
				Console.WriteLine( "event loop: released lock" );
			}
			
			if( callback != null ) {
				Console.WriteLine( "event loop: dispatching callback" );
				// TODO: not sure what this callback delegate looks like yet
				// callback.callback.Invoke( this, callback.args );
			}
			Console.WriteLine( "event loop waiting" );
			manualResetEvent.WaitOne();
		} // while
	}
	
	public void ReadJsFile( string in_filename ) {
		var context = Context.Create();
		var astBuilder = new AstGenerator();
		var etGenerator = new EtGenerator();
		var astNodes = astBuilder.Build( in_filename, Encoding.UTF8);
		Action<Scope> compiled = etGenerator.Build(astNodes, context);

		var globals = Scope.CreateGlobal(context);

		context.SetupGlobals(globals);

		Action<object> emit = ( obj ) => { Console.WriteLine( JsTypeConverter.ToString( obj ) ); };
		globals.Global( "puts", emit );
		
		Func<VarDelegate, Net.NetServer> createNetServer = ( callback ) => { Net.NetServer ns = new Net.NetServer( callback ); return ns; };
		// Func<object, object> createNetServer = ( callback ) => { Net.NetServer ns = new Net.NetServer( callback ); return ns; };
		globals.Global( "createNetServer ", createNetServer );		

		compiled( globals );

		// Pull reaches into js script and gets obj by name
		// Console.WriteLine( globals.Pull( "x" ) );

		}
} // class

public class Callback 
{
	public VarDelegate callback;
	public object args;
}

public delegate void VarDelegate( params object[] args );