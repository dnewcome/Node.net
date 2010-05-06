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
var lock : Object = new Object();
var workItems = [];

// thread safe enqueue method
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
