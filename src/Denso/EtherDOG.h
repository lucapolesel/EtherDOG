'!TITLE "EtherDOG Communication Library Header"
#Include "Variant.h"

' Header structure
' 'E' (Char - Byte)
' 'T' (Char - Byte)
' 'H' (Char - Byte)
' 'E' (Char - Byte)
' 'R' (Char - Byte)
' 'D' (Char - Byte)
' 'O' (Char - Byte)
' 'G' (Char - Byte)
' RequestID (Byte)
' CanReceive (Byte)
' Message Length (UShotr - 4 Bytes)
#define MESSAGE_HEADER_LENGTH	14

#define BYTE_MAX_VALUE			255
#define BYTE_MIN_VALUE			0

#define STATUS_DISCONNECTED		0
#define STATUS_CONNECTED		1

Dim tcpClientLine As Integer = 4
