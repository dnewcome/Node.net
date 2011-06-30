// should exit after a single request
// since all listeners are removed
var server = net.createServer( function( stream ) {
	stream.addListener( 'data', function( data ) {
		puts( data );
		stream.removeAllListeners( 'data' );
		server.removeAllListeners( 'connect' );
	});
}).listen( 9982, 'localhost' );
