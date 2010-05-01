function cb() {
	print( 'server called' );
}

var server = http.createServer( cb, 'http://localhost:9980/' ).listen();
print( server.prefix );