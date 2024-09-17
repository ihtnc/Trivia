# Trivia.Client

The `Trivia.Client` is a component of the Trivia application that handles trivia participant actions. This document provides details on how trivia clients can initiate a connection to the server, the types of messages that can be expected from the server, and the types of messages that the trivia client can send to the server.

## Table of Contents
- [Getting Started](#getting-started)
- [Connecting to the Trivia.Server](#connecting-to-the-triviaserver)
- [Trivia.Client Components](#triviaclient-components)
  - [TriviaClient](#triviaclient-1)
  - [Console application](#console-application)

## Getting Started
To start the Trivia.Client, run the following command in the Trivia.Client directory:

```
dotnet run
```

The client will need to setup the connection to the Trivia.Server to be able to receive and send messages related to the trivia game.

## Connecting to the Trivia.Server
Requirements for connecting to the Trivia.Server are specified in the Trivia.Server [README](../Trivia.Server/README.md).

## Trivia.Client Components
The Trivia.Client is composed of the following components:

### TriviaClient
This component is responsible for handling the connection to the Trivia.Server application, processing messages from the server regarding the state of the game, and sending messages to the server (i.e.: `TriviaAnswer`, `Disconnect`, etc).

It runs the handling of incoming messages in a separate thread to allow for concurrent processing alongside any user input. It represents the received server messages as events that can be subscribed to by the user facing application (i.e.: a console application). It also exposes a method to allow users to send messages to the server.

### Console application
The console application is the entry point for the Trivia.Client application. It is a simple user interface that allows the user to interact with the `TriviaClient` component.

It subscribes to the various events raised by the TriviaClient component and displays the corresponding details (i.e.: current question and answer options, result of the answer, etc). It also allows the user to specify their answer and submit it to the server.