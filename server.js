/**
* Server is a .net executable written in js, compiled to .net using jsc
*/
import System;
import System.Threading;
import System.Net;
import System.IO;

import Http;
import Net;

var manualResetEvent : ManualResetEvent = new ManualResetEvent( false );
var workItems = [];

function require( file ) {
	// for compiled-in functionality, return existing references.
	// TODO: clean up the global namespace, as it is, `http' et al.
	// are already visible to the user code
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
	var sr : StreamReader = new StreamReader( file + '.js' );
	var fileContents = sr.ReadToEnd();
	// extra semicolon provided after file contents.. just in case
	var code = 'var exports = {}; ' + fileContents + '; exports;';
	sr.Close();
	print( 'evaluating require: ' + code );
	return eval( code, 'unsafe' );
}

// quick hack to add sys.puts();
// TODO: `sys' should be implemented in its own class
var sys = { puts: function( string ){ print( string ); } };

// threadsafe enqueue function
function queueWorkItem( item ) {
		print( 'queueWorkItem(): queuing item: ' + item );
		print( 'queueWorkItem(): acquiring lock: ' + workItems );
	Monitor.Enter( workItems );
	try {
		workItems.push( item );
		print( 'queueWorkItem(): item queued' );
		manualResetEvent.Set();
	}
	finally {
		Monitor.Exit( workItems );
		print( 'queueWorkItem: released lock' );
	}
}

// takes a .js file on the commandline, eval it 
var args : String[] = Environment.GetCommandLineArgs();
var sr : StreamReader = new StreamReader( args[1] );
var script = sr.ReadToEnd();
eval( script, 'unsafe' );
sr.Close();

// main event loop
while( true ) {
	print( 'event loop running' );
	var callback;
	
	// TODO: can we improve locking method?
	Monitor.Enter( workItems );
	try {
		print( 'event loop: acquired lock' );
		callback = workItems.shift();
		if( callback == null ) {
			manualResetEvent.Reset();
		}
	}
	finally {
		Monitor.Exit( workItems );
		print( 'event loop: released lock' );
	}
	
	if( callback != null ) {
		print( 'event loop: dispatching callback' );
		// TODO: figure out what 'this' should be passed, currently passing server
		// which is almost certainly the wrong thing.
		callback.callback.apply( this, callback.args );
	}
	print( 'event loop waiting' );
	manualResetEvent.WaitOne();
} // while
