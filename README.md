# EtherDOG

**EtherDOG** is a communication protocol I developed for connecting Denso RC8 robots with PLCs or PCs. The goal was to create a flexible communication structure that could be implemented easily without relying on proprietary libraries (such as ORIN2 for DENSO) or writing custom TCP connections from scratch each time.

This protocol was primarily used in scenarios where an **EtherCAT** connection wasn’t available due to various reasons beyond my control, hence the name **EtherDOG**.

## Key Features

- **Simple Communication Structure**: Inspired by EtherCAT, each device sends a data structure that can be parsed and assigned to local variables. This ensures interoperability between devices without needing a proprietary library.
- **Protocol Compatibility**: The data structure is somewhat similar to the **OMRON FINS** protocol but with additional parameters to validate that the message is coming from this protocol. One key feature is the `CanReceive` bit, which indicates whether the device is ready to receive data. This was useful for systems like the OMRON TCP receive datablock, which required some time to process incoming data.

## Why EtherDOG?

EtherDOG was created to address the limitations of EtherCAT in situations where it wasn’t feasible to implement, either due to cost or other constraints. This protocol provides an alternative that is simple yet reliable for machine-to-machine communication.

## Status

Please note that this project is currently **undocumented**, and the PLC implementation is **missing**. However, the protocol is straightforward enough to implement by referring to the **PC library**, which contains the main logic for communication.

I plan to add detailed documentation in the future, as this was a useful project for me, and I believe it could help others who are facing similar challenges—especially when EtherCAT isn’t an option.

## Conclusion

EtherDOG offers a simple and efficient alternative for robot-to-PLC/PC communication when EtherCAT is not viable. Feel free to explore the code, and I hope it helps you in your own projects!

## License

This project is licensed under the MIT License.
