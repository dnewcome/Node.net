function cb( request, response ) {
	
	response.write( '<html><body> Hello world!</body></html>' );
	response.end();
}

var server = http.createServer( cb ).listen( 9980, 'localhost' );
