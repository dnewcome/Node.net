#include <v8.h>
#include <vector>
#using <System.dll>

using namespace v8;
using namespace System::Net::Sockets;
using namespace System::Net;
using namespace System::Threading;

ref class NetServer {
private:
	TcpListener^ tcpListener;
	std::vector<Handle<Function>>* listeningCallbacks;
	std::vector<Handle<Function>>* connectionCallbacks;
	std::vector<Handle<Function>>* closeCallbacks;
	
public:
	// constructor takes a v8 handle for callback
	NetServer( Handle<Function> in_callback ) {
		listeningCallbacks = new std::vector<Handle<Function>>();
		connectionCallbacks = new std::vector<Handle<Function>>();
		closeCallbacks = new std::vector<Handle<Function>>();
		listeningCallbacks->push_back( in_callback );
	}
	
	NetServer() {}
	
	void Listen( System::Int32 port, System::String^ host ) {
		
		IPHostEntry^ hostInfo = Dns::Resolve( host );
		IPAddress^ ipAddress = hostInfo->AddressList[0];
		
		this->tcpListener = gcnew TcpListener( ipAddress, port );
		// print( 'net.Server.listen(): starting tcp server' );
		this->tcpListener->Start();
		// queueWorkItem( { callback: raiseListeningEvent, args: [] } );
		
		System::AsyncCallback^ ListenerCallbackDelegate = gcnew System::AsyncCallback( this, &NetServer::ListenerCallback );
		
		this->tcpListener->BeginAcceptTcpClient( ListenerCallbackDelegate, NULL );
	}
	
	
	
	void ListenerCallback( System::IAsyncResult^ result ) {
		printf("%s\n", "listener called" );
		
	}
};

class NetStream {

};

int main(int argc, char* argv[]) {
	HandleScope handle_scope;
	
	// Handle<FunctionTemplate> createservertemplate = FunctionTemplate::New( createServerCallback );
	// global_templ->Set( String::New("createServer"), putstemplate );
	
	printf("%s\n", "working" );
	
	NetServer^ netServer = gcnew NetServer();
	netServer->Listen( 9980, "localhost" );
	Thread::Sleep( Timeout::Infinite );
}