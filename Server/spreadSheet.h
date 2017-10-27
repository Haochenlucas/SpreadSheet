#ifndef SPREADSHEET_H
#define SPREADSHEET_H

#include <string>
#include <map>
#include <set>
#include <stack>
#include <pthread.h>

// spreadsheat class.
class SS
{
 private:
  // All edited data
  // <cellname, cell content>
  std::stack< std::pair<std::string,std::string> > editRecored;

  std::string lastEditCell, lastEditContent;
  
 public:
  // Current spreadsheet data
  // <cellname, cell content>
  std::map<std::string,std::string> data;
  pthread_mutex_t mutex;
  std::set<int> sockets;
  
  SS();
  
  void edit(std::string, std::string);
  
  bool undo();
  
  void addSock(int);
  
  void removeSock(int);

  void get_last_edit_cell(std::string&, std::string&);
};

#endif
