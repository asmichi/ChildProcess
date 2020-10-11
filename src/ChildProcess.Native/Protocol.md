
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

Upon creation, the server shall send `errno` as the response.

`execve` semantics.

Request:

- Message body length (32)
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

- errno (32)
    - 0 on success
- pid (32)
