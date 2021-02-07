
# Protocol

- All numeric values are encoded in native byte order.

## Channels

- A) Main subchannel request channel, unidirectional, client → server
- B) Main notification channel, unidirectional, server → client
- C) Subchannel, bidirectional

### A) Main subchannel request channel

Subchannel creation.

The client shall send 1 dummy byte with a unix domain socket fd in the ancillary data.

### B) Main notification channel

Notifications of exited chlid processes.

For each child process that has exited, a ChildExitNotification struct shall be sent.

### C) Subchannel, full-duplex

Every request shall be prefixed with two 32-bit integer. The first specifies a command number.
The second specifies the length of the request body.

The error code is defined as follows:

- 0: Success
- -1: Invalid request
- Positive: errno

#### Spawn Process (Command 0)

`execve` semantics.

Request body:

- Process token (64)
- flags (32) (NOTE: fds must be sent in this order).
    - Redirect stdin (1)
    - Redirect stdout (1)
    - Redirect stderr (1)
- working directory (N)
- file (N)
- argv (N)
- envp (N)

Response:

- Error code (32)
- pid (32)
