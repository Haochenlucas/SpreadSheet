lib-opt = -pthread -std=c++0x
all: server spreadSheet
	g++ *.o -o server $(lib-opt)

server: server.cpp
	g++ -c server.cpp $(lib-opt)

spreadSheet: spreadSheet.cpp
	g++ -c spreadSheet.cpp $(lib-opt)

clean: 
	rm server *.o allss.txt

