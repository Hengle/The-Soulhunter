#include <Godot.hpp>
#include <Node.hpp>
#include <Reference.hpp>
#include <TCP_Server.hpp>
#include <StreamPeerTCP.hpp>

#include <thread>
#include <vector>

using namespace godot;
using namespace std;

class Server : public Node {
	GODOT_CLASS(Server, Node)

private:
	TCP_Server* server;
	vector<Ref<StreamPeerTCP>> peers;

	thread main_thread;

public:
	static void _register_methods();

	Server();
	~Server();

	void _init();
	void _ready();

	void _process(float delta);
	void set_speed(float p_speed);
	float get_speed();
	void listen();
};