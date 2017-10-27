#ifndef SERVER_H
#define SERVER_H

#include <string>

#define RECV_BUF_SIZE 20000

struct threadInfo {
  int sock;
  SS *ss = NULL;
};

void readAllSS();

void writeAllSS();

void *exitFunc(void*);

void handleClient(int); 

void error(const char*);

void killThread(struct threadInfo&);

void handleConnect(struct threadInfo&, char*);

void handleEdit(struct threadInfo&, char*);

void handleUndo(struct threadInfo&);

void sendChange(struct threadInfo&);

void handleStatus(struct threadInfo&, char*, bool);

void sendToAllClient(struct threadInfo&, std::string&);

void checkConnection(struct threadInfo&);

void *handleClient(void*);

#endif
