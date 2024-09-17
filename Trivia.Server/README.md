# Trivia.Server

The `Trivia.Server` is a component of the Trivia application that handles Trivia.Client connections, processes messages, and runs the trivia game. This document provides details on how trivia clients can initiate a connection to the server, the types of messages that can be expected from the server, and the types of messages that the server can accept from trivia clients.

## Table of Contents
- [Getting Started](#getting-started)
- [Connecting to the Trivia.Server](#connecting-to-the-triviaserver)
  - [Messages from the Trivia.Server](#messages-from-the-triviaserver)
  - [Messages to the Trivia.Server](#messages-to-the-triviaserver)
  - [Example](#example)
- [Trivia.Server Components](#triviaserver-components)
  - [ConnectionServer](#connectionserver)
  - [ActionServer](#actionserver)
  - [GameServer](#gameserver)
  - [TriviaServer](#triviaserver-1)

## Getting Started
To start the Trivia.Server, run the following command in the Trivia.Server directory:

```
dotnet run
```

The server will start and listen for incoming connections from chat clients. It will display the IP address and port number of the sender listener, which clients can use to connect to the server.

## Connecting to the Trivia.Server
Trivia clients can initiate a connection to the server by following these steps:

1. Connect to the receiver listener: The trivia client first connects to the receiver listener of the server and sends a `Request` message.

2. Receive setup instructions: The server will respond with a `SetupConnection` message to the client, which includes the sender listener details (IP address and port).

3. Send a message to the sender listener: The client then sends a `Connect` message to the sender listener of the server using the details provided in the setup connection message. This long-lived connection will be used by the server to send messages to the client.

### Messages from the Trivia.Server
The server can send the following types of messages to the client:

Via the receiver listener:
- **SetupConnection**: Sent during the initial connection to provide the trivia client with the sender listener details.
- **Accepted**: Indicates that the client's message has been accepted by the server.
- **Error**: Indicates that an error has occurred while processing the client's message.

Via the sender listener:
- **Accepted**: Indicates that the connection has been established.
- **Error**: Indicates that an error has occurred while establishing a connection.
- **RoundDetails**: Contains the details of the current trivia round.
- **RoundStart**: Indicates that a new trivia round is starting.
- **Question**: Contains the current question for the trivia round.
- **Result**: Contains the result of the client's answer to the current question.
- **RoundEnd**: Contains the result of the trivia round.

### Messages to the Trivia.Server
The server can accept the following types of messages from the trivia client:

For the receiver listener:
- **Request**: Sent to request for the receiver listener details for clients to connect to.
- **RequestRoundDetails**: Sent to request current trivia round details from the server.
- **TriviaAnswer**: Sent to provide an answer to the current trivia question.
- **Disconnect**: Sent to request disconnection to the server.

For the sender listener:
- **Connect**: Sent to establish a long-lived connection for sending messages to the client.

### Example
Here is an example of the message flow between the trivia client and the server:

1. Server: Starts and displays the receiver listener details.
2. Client: Connects to the receiver listener and sends `Request` message with name.
3. Server: Sends `SetupConnection` message with sender listener details and client id via the receiver listener as response.
4. Client: Connects to the sender listener and sends `Connect` message with both name and client id.
5. Server: Sends `Accepted` message via the sender listener as response.
6. Server: Starts a configurable delay timer before starting the round if there is at least 1 client connected.
7. Server: Sends `RoundStart` message to all clients via the sender listener.
8. Server: Sends `Question` message to all clients via the sender listener.
9. Client: Connects to the receiver listener and sends `TriviaAnswer` message with answer details.
10. Server: Processes all answers and marks them after a configurable delay.
11. Server: Sends `Result` message to all clients via the sender listener.
12. Server: Starts a configurable delay timer before sending the next question or ending the round.
13. Repeat steps 8-12 until there are no more questions in the trivia round.
14. Server: Sends `RoundEnd` message via the sender listener.
15. Repeat 6-14 until the server process is terminated.

## Trivia.Server Components

The Trivia.Server is composed of the following components:

### ConnectionServer
This server class handles incoming messages from trivia clients, and generates the corresponding server actions for further processing. It also handles the outgoing messages to the trivia clients.

The server runs in a separate thread and listens to incoming messages on the sender listener and receiver listener ports, responds an `Accepted` message to the client, and generates the server action associated with the client message. These server actions are added to a queue which is processed by the `ActionServer`. It also exposes a method for sending messages to the clients via the sender listener.

> NOTE: The only instance where the sender listener is used to read messages from the client is when the client sends a `Connect` message to it. This is part of establishing the long-lived connectivity to the client for sending server messages (`RoundStart`, `Question`, etc). The sender listener details was provided by the receiver listener as part of the response to the initial `Request` message sent by the client.

### ActionServer
This server class handles the server actions generated by the other servers and initiates the necessary operations on the corresponding servers (i.e.: adding a new participant to the `GameServer`, asking the `ConnectionServer` to send a server message to a client, etc.).

The server runs in a separate thread and processes the server actions in the queue. The server actions are processed in the order they are received, and the server raises events accordingly.

These are the various server actions handled by the server:
- **AddParticipant**
- **RemoveParticipant**
- **SetAnswer**
- **SendClientMessage**
- **MoveGameToNextState**

### GameServer
This server class handles the trivia game logic. It orchestrates the various operations needed for the trivia game. These operations involve starting a new round, sending questions to clients, processing received answers, and sending results back to clients.

The server runs in a separate thread and generates server actions based on the current state of the game (i.e.: sending questions to participants, waiting for participants to respond, evaluating answers, etc). These server actions are added in the same queue that the `ActionServer` is processing.

### TriviaServer
This server class is the main entry point for the Trivia.Server application. It initializes the `ConnectionServer`, `ActionServer`, and `GameServer`, and starts them in separate threads.

It orchestrates the various operations of the other server components, and calls the method of the corresponding server based on the operation. It also serves as the graphical user interface (GUI) for the server, displaying the server status, connected clients, game status, and other relevant information.