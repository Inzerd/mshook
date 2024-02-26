# mshook
keylogger - mshook:

read all keyboard keys pressed and release and save sequence inside file or exfiltrate via ping.
The data exfiltrate cannot reconduce from correct keys pressed, are encrpted.
the data are sended inside data of IMCP protocol (ping) must have a correct structure that allow recompiler to assembly correct the keyboard workflow.

server - msreceiver:
after recive first ping tools must to read data structure inside ping and start decrypt and parse. 
Data are formed by: "keyAction{separatorTBD}keyCode{separatorTBD}"
Try to save a list of int anr retrieve it correct from KeyBuilder.

this algortihm separete dusty from tools, and in case of NIDS or EDR, ping action send apparently casual value.

TBF:

Maybe there are possibile to save key-flow pressed inside file, and send via ping later, when pc is in standby mode, during launch-time or in other day moments.

another way are to send ping via information to send key-flow
