import System.Timers;

package SetTimeout {
class NodeTimer
{
	var timer : System.Timers.Timer;
	var callback;
	
	function raiseCallback() {
		callback();
	}
	
	function SetTimeout( fn, time ) {
		callback = fn;
		timer = new System.Timers.Timer( time );
		timer.add_Elapsed( SetTimeoutCallback );
		timer.AutoReset = false;
		timer.Enabled = true;
	}
	
	function SetTimeoutCallback( obj : Object, args : ElapsedEventArgs ) {
		print('SetTimeoutCallback(): timeout elapsed');
		print( obj );
		queueWorkItem( { callback: raiseCallback, args: [] } );
	}
	
}
}