/* A simple server in the internet domain using TCP
 * The port number is passed as an argument
 * This version runs forever, forking off a separate
 * process for each connection
 * Used TCP connection code from http://www.linuxhowtos.org/C_C++/socket.htm
 */
#include <unistd.h>
#include <string.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <pthread.h>
#include <string>
#include <map>
#include <iostream>
#include <fstream>
#include "spreadSheet.h"
#include "server.h"

using namespace std;

static map<string,SS> allss;

static int cur_id = 1;
static pthread_mutex_t id_mutex;
 
int main(int argc, char *argv[])
{
  readAllSS();
  pthread_t e;
  if(pthread_create(&e, NULL, exitFunc, NULL))
    {
      error("ERROR on creating thread for waiting for exit command.");
    }
  
  int sockfd, newsockfd, portno = 2112;
  socklen_t clilen;
  struct sockaddr_in serv_addr, cli_addr;
  /*if (argc < 2) {
    fprintf(stderr,"ERROR, no port provided\n");
    exit(1);
    }*/
  sockfd = socket(AF_INET, SOCK_STREAM, 0);
  if (sockfd < 0)
    error("ERROR opening socket");
  bzero((char *) &serv_addr, sizeof(serv_addr));
  //portno = atoi(argv[1]);
  serv_addr.sin_family = AF_INET;
  serv_addr.sin_addr.s_addr = INADDR_ANY;
  serv_addr.sin_port = htons(portno);
  if (bind(sockfd, (struct sockaddr *) &serv_addr,
	   sizeof(serv_addr)) < 0)
    error("ERROR on binding");
  listen(sockfd,5);
  clilen = sizeof(cli_addr);
  while (1) {
    newsockfd = accept(sockfd, (struct sockaddr *) &cli_addr, &clilen);
    if (newsockfd < 0)
	 error("ERROR on accept");
    pthread_t t;
    if(pthread_create(&t, NULL, handleClient, &newsockfd))
      {
	error("ERROR on creating thread for a new client");
      }
  }
  /* end of while */
  close(sockfd);
  return 0; /* we never get here */
}

void readAllSS()
{
  string line, line2;
  ifstream myfile ("allss.txt");
  if (myfile.is_open())
  {
    while ( getline (myfile,line) )
    {
      SS &ss = allss[line];
      while ( getline (myfile,line) )
	{
	  if(line == "\t")
	    {
	      break;
	    }
	  getline (myfile,line2);
	  ss.data[line] = line2;
	}
    }
    myfile.close();
  }
  else 
    {
      cout << "Unable to open allss.txt" << endl;
    }
}

void writeAllSS()
{
  ofstream myfile;
  myfile.open ("allss.txt");
  for (map<string,SS>::iterator it = allss.begin(); it != allss.end(); ++it)
    {
      myfile << it->first << endl;
      SS &ss = it->second;
      pthread_mutex_lock(&ss.mutex);
      for (map<string,string>::iterator it=ss.data.begin(); it!=ss.data.end(); ++it)
	{
	  myfile << it->first << endl;
	  myfile << it->second << endl;
	}
      myfile << "\t" << endl;
    }
  myfile.close();
}

void *exitFunc(void* p)
{
  string in;
  while(cin >> in)
    {
      if(in[0] == 'q')
	break;
    }
  writeAllSS();
  exit(0);
}

void error(const char *msg)
{
  perror(msg);
  writeAllSS();
  exit(1);
}

static int testint = 0;

void killThread(struct threadInfo &info)
{
  int sock = info.sock;
  SS *ss = info.ss;
  if(ss)
    ss->removeSock(sock);
  close(sock);
  pthread_exit(NULL);
}

void handleConnect(struct threadInfo &info, char *buffer)
{
  int sock = info.sock;
  int i = 8;
  while(true)
    {
      if(!buffer[i])
        killThread(info);
      if(buffer[i] == '\t')
        {
          if(buffer[i+1] == '\n')
            break;
          else
            killThread(info);
        }
      i++;
    }

  string ssName(buffer);
  ssName = ssName.substr(8,i-8);

  pthread_mutex_lock(&id_mutex);
  int clientID = cur_id++;
  pthread_mutex_unlock(&id_mutex);

  info.ss = &allss[ssName];
  SS &ss = *info.ss;
  pthread_mutex_lock(&info.ss->mutex);
  info.ss->addSock(sock);


  string data = "Startup\t";
  data += to_string(clientID);
  data += '\t';
  // Send the data of the spreadsheet
  for (map<string,string>::iterator it=ss.data.begin(); it!=ss.data.end(); ++it)
    {
      data += it->first;
      data += "\t";
      data += it->second;
      data += "\t";
    }
  pthread_mutex_unlock(&ss.mutex);
  data += "\n";
  int ret_size = write(info.sock,data.c_str(),data.length());
  if (ret_size <= 0) killThread(info);
}

void handleEdit(struct threadInfo &info, char* buffer)
{
  int cNameEndIndex = 0;
  int cContentEndIndex = 0;
  int i = 5;
  int tabCount = 0;
  while(true)
    {
      if(!buffer[i])
        killThread(info);
      if(buffer[i] == '\t')
        {
          tabCount ++;

          if (tabCount == 1){
            cNameEndIndex = i;
          }
          if (tabCount == 2){
            cContentEndIndex = i;
            if(buffer[i+1] == '\n')
              break;
            else
              killThread(info);
          }
        }
      i++;
    }

  string cName(buffer);
  cName = cName.substr(5,cNameEndIndex - 5);
  string cContent(buffer);
  cContent = cContent.substr(cNameEndIndex + 1,cContentEndIndex - cNameEndIndex - 1);

  pthread_mutex_lock(&info.ss->mutex);
  info.ss->edit(cName,cContent);
  sendChange(info);
  pthread_mutex_unlock(&info.ss->mutex);
}

void handleUndo(struct threadInfo &info)
{
  pthread_mutex_lock(&info.ss->mutex);
  if(info.ss->undo())
    sendChange(info);
  pthread_mutex_unlock(&info.ss->mutex);
}

void sendChange(struct threadInfo &info)
{
  string buffer = "Change\t";
  string cName, cContent;
  SS &ss = *info.ss;

  ss.get_last_edit_cell(cName, cContent);
  buffer += cName;
  buffer += "\t";
  buffer += cContent;
  buffer += "\t";
  buffer += "\n";

  sendToAllClient(info, buffer);
}

// isTyping true: IsTyping, isTyping false: DoneTyping
void handleStatus(struct threadInfo &info, char *buffer, bool isTyping)
{
  int i;
  if (isTyping)
    {
      i = 9;
    }
  else
    {
      i = 11;
    }

  int clientEndIndex = 0;
  int cNameEndIndex = 0;
  int tabCount = 0;
  while(true)
    {
      if(!buffer[i])
        killThread(info);
      if(buffer[i] == '\t')
        {
          tabCount ++;

          if (tabCount == 1){
            clientEndIndex = i;
          }
          if (tabCount == 2){
            cNameEndIndex = i;
	    if(buffer[i+1] == '\n')
	      break;
	    else
	      killThread(info);
          }
        }
      i++;
    }

  string clientID(buffer);
  clientID = clientID.substr(isTyping? 9 : 11,clientEndIndex - (isTyping? 9 : 11));

  string cName(buffer);
  cName = cName.substr(clientEndIndex + 1,cNameEndIndex - clientEndIndex - 1);

  SS &ss = *info.ss;

  pthread_mutex_lock(&ss.mutex);

  string data;
  if (isTyping)
    {
      data = "IsTyping\t";
    }
  else
    {
      data = "DoneTyping\t";
    }

  data += clientID;
  data += "\t";
  data += cName;
  data += "\t\n";

  sendToAllClient(info, data);

  pthread_mutex_unlock(&ss.mutex);
}

void sendToAllClient(struct threadInfo &info, string &data)
{
  const char* buffer = data.c_str();
  int length = data.length();
  SS &ss = *info.ss;
  int ret_size;
  set<int> dcSocks;
  for (set<int>::iterator it=ss.sockets.begin(); it!=ss.sockets.end(); ++it)
    {
      ret_size = write(*it, buffer, length);
      if(ret_size <= 0)
	{
	  dcSocks.insert(*it);
	}
    }
  for (set<int>::iterator it=dcSocks.begin(); it!=dcSocks.end(); ++it)
    {
      ss.sockets.erase(*it);
    }
}

void checkConnection(struct threadInfo &info)
{
  SS &ss = *info.ss;
  int sock = info.sock;
  if(ss.sockets.find(sock) == ss.sockets.end())
    killThread(info);
}

void *handleClient (void* arg)
{
  int sock = *((int*)arg);
  char buffer[RECV_BUF_SIZE];
  int ret_size;
  struct threadInfo info;
  info.sock = sock;

  bzero(buffer,RECV_BUF_SIZE);
  ret_size = read(sock,buffer,RECV_BUF_SIZE - 1);

  if (ret_size <= 0) killThread(info);

  //printf("Here is the message: %s\n",buffer);

  // Connect to a SS or create one first
  if(!(buffer[0] == 'C' && buffer[1] == 'o' && buffer[2] == 'n' && 
       buffer[3] == 'n' && buffer[4] == 'e' && buffer[5] == 'c' && 
       buffer[6] == 't' && buffer[7] == '\t'))
    killThread(info);

  handleConnect(info, buffer);
  
  while(1)
    {
      bzero(buffer,RECV_BUF_SIZE);
      ret_size = read(sock,buffer,RECV_BUF_SIZE - 1);
      if (ret_size <= 0) killThread(info);
      //printf("Here is the message: %s\n",buffer);
      switch(buffer[0])
        {
        case 'E':
          if(buffer[1] == 'd' && buffer[2] == 'i' && buffer[3] == 't' &&
             buffer[4] == '\t')
	    handleEdit(info, buffer);
          else
            killThread(info);
          break;
        case 'U':
          if(buffer[1] == 'n' && buffer[2] == 'd' && buffer[3] == 'o' &&
	     buffer[4] == '\t' && buffer[5] == '\n')
	    handleUndo(info);
          else
            killThread(info);
          break;
        case 'I':
          if(buffer[1] == 's' && buffer[2] == 'T' && buffer[3] == 'y' &&
             buffer[4] == 'p' && buffer[5] == 'i' && buffer[6] == 'n' &&
             buffer[7] == 'g' && buffer[8] == '\t')
	    handleStatus(info, buffer, true);
          else
            killThread(info);
          break;
        case 'D':
          if(buffer[1] == 'o' && buffer[2] == 'n' && buffer[3] == 'e' &&
             buffer[4] == 'T' && buffer[5] == 'y' && buffer[6] == 'p' &&
             buffer[7] == 'i' && buffer[8] == 'n' && buffer[9] == 'g' &&
             buffer[10] == '\t')
	    handleStatus(info, buffer, false);
          else
            killThread(info);
          break;
	default:
	  killThread(info);
          break;
        }
      checkConnection(info);
    }
}
