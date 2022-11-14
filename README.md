# rRM

Work-in-progress route manager for Windows, with the intended use case being to automatically edit Windows network routes to route specific processes through specific network interfaces.

## How does it work?

The application will use [WinPcap](https://www.winpcap.org/) and [sharppcap](https://github.com/dotpcap/sharppcap) to analyze outgoing and incoming network traffic from all processes, adding a new routing rule for the destination or source IP address if it originates from, or is going to, a specific process. Therefore, the application will work reactively to re-route network traffic through a certain network interface if conditions are met.
