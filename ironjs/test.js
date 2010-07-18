puts( "working" );

createNetServer( function( stream ) {
	stream.addListener( 'data', function( data ) {
		print( data );
	});
}).listen( 9982, 'localhost' );
