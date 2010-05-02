import System;
import System.IO;
import System.Threading;


class Foo {
	function readFileAsync() {
		var fs : FileStream = new FileStream( 'test.txt', FileMode.Open, FileAccess.Read );
		
		var result : IAsyncResult = fs.BeginRead( new byte[8], 0, 8, ListenerCallback, fs );
		Thread.Sleep( Timeout.Infinite );
	}

	function ListenerCallback( result : IAsyncResult ) : void {
		print( 'ListenerCallback():' );
	}

	// we can create the delegate maually also if we want to, and use it in place of the 
	// listenercallback name in the Beginread()
	// var delegate : AsyncCallback = Delegate.CreateDelegate( Type.GetType("System.AsyncCallback"), this, "ListenerCallback" );
	
} // class

var foo = new Foo();
foo.readFileAsync();