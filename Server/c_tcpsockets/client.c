#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netdb.h>

void error(const char *msg)
{
    perror(msg);
    exit(0);
}

int main(int argc, char *argv[])
{
    int sockfd, portno, n;
    struct sockaddr_in serv_addr;
    struct hostent *server;

    char buffer[256];
    if (argc < 3) {
       fprintf(stderr,"usage %s hostname port\n", argv[0]);
       exit(0);
    }
    portno = atoi(argv[2]);
    sockfd = socket(AF_INET, SOCK_STREAM, 0);
    if (sockfd < 0)
        error("ERROR opening socket");
    server = gethostbyname(argv[1]);
    if (server == NULL) {
        fprintf(stderr,"ERROR, no such host\n");
        exit(0);
    }
    bzero((char *) &serv_addr, sizeof(serv_addr));
    serv_addr.sin_family = AF_INET;
    bcopy((char *)server->h_addr,
         (char *)&serv_addr.sin_addr.s_addr,
         server->h_length);
    serv_addr.sin_port = htons(portno);
    if (connect(sockfd,(struct sockaddr *) &serv_addr,sizeof(serv_addr)) < 0)
        error("ERROR connecting");
    do{
        printf("Please enter the message: ");
        bzero(buffer,256);
        fgets(buffer,255,stdin);

        switch(buffer[0])
	  {
          case '1':
            snprintf(buffer, sizeof(buffer), "Connect\ttestSS\t\n");
	    break;
          case '2':
            snprintf(buffer, sizeof(buffer), "Edit\tA1\t2\t\n");
	    break;
          case '3':
            snprintf(buffer, sizeof(buffer), "Undo\t\n");
	    break;
          case '4':
            snprintf(buffer, sizeof(buffer), "IsTyping\t1\tA1\t\n");
            break;
          case '5':
          snprintf(buffer, sizeof(buffer), "DoneTyping\t1\tA1\t\n");
            break;
	  }
        //n = write(sockfd, "Connect\tF",9);
        printf("Here is the message you send:\n%s\n", buffer);
        n = write(sockfd,buffer,strlen(buffer));
        if (n < 0)
             error("ERROR writing to socket");
        bzero(buffer,256);
        n = read(sockfd,buffer,255);
        if (n <= 0)
             error("ERROR reading from socket");
        printf("%s\n",buffer);

        fgets(buffer,255,stdin);

        printf("%s\n", buffer);
    }while(strlen(buffer) < 5);
    close(sockfd);
    return 0;
}
