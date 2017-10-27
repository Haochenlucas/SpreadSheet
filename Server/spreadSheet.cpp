#include "spreadSheet.h"

using namespace std;

SS::SS()
{
}

void SS::edit(string cell, string content)
{
  lastEditCell = cell;
  // The cell already have a content
  if (data.find(cell) != data.end() ) {
    lastEditContent = data[cell];
  }
  else{
    lastEditContent = "";
  }
  pair <string,string> histroy (lastEditCell,lastEditContent);
  editRecored.push(histroy);

  lastEditContent = content;
  
  // Change the cell
  data[cell] = content;
}

bool SS::undo()
{
  if(!editRecored.size())
      return false;
  // Pop the history
  pair <string,string> history = editRecored.top();
  editRecored.pop();
  lastEditCell = history.first;
  lastEditContent = history.second;

  // Change the cell
  data[lastEditCell] = lastEditContent;
  return true;
}

void SS::addSock(int sock)
{
  sockets.insert(sock);
}

void SS::removeSock(int sock)
{
  sockets.erase(sock);
}

void SS::get_last_edit_cell(string& cell, string& content)
{
  cell = lastEditCell;
  content = lastEditContent;
}
