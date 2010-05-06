/**
* Server is a .net executable written in js, compiled to .net using jsc
*/
import System;
import System.Threading;
import System.Net;
import System.IO;

import Http;

// takes a .js file on the commandline, eval it 
var args : String[] = Environment.GetCommandLineArgs();
var sr : StreamReader = new StreamReader( args[1] );
var script = sr.ReadToEnd();
eval( script, 'unsafe' );
sr.Close();

var manualResetEvent : ManualResetEvent = new ManualResetEvent( false );
var lock : Object = new Object();
var workItems = [];

// thread safe enqueue method
function queueWorkItem( item ) {
	Monitor.Enter( lock );
	try {
		workItems.push( item );
		print( 'queueWorkItem(): item queued' );
		manualResetEvent.Set();
	}
	finally {
		Monitor.Exit( lock );
		print( 'queueWorkItem: released lock' );
	}
}

// main event loop
while( true ) {
	print( 'event loop running' );
	var callback;
	
	// TODO: can we improve locking method?
	Monitor.Enter( lock );
	try {
		print( 'event loop: acquired lock' );
		callback = workItems.shift();
		if( callback == null ) {
			manualResetEvent.Reset();
		}
	}
	finally {
		Monitor.Exit( lock );
		print( 'event loop: released lock' );
	}
	
	if( callback != null ) {
		// TODO: figure out what 'this' should be passed, currently passing null
		callback.callback.apply( null, callback.args );
	}
	print( 'event loop waiting' );
	manualResetEvent.WaitOne();
} // while