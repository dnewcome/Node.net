puts( "working" );

var server = net.createServer( function( stream ) {
	stream.addListener( 'data', function( data ) {
		puts( data );
	});
}).listen( 9982, 'localhost' );
