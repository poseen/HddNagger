# HddNagger
A small windows service which prevents your "sleepy" hard driver from sleeping.

## How does it work?
It is a simple windows service. You install the executable as a service and in the service's properties you set the (preferably) root folder where the service can create and immediately delete a temporary file with a random file name.

Example for the argument:

D:\

## Why would I need this?
  * Just an example code
    It is a short example how a windows service can be done, although in this project the code quality isn't the best.

  * To solve an issue
    It was just an interesting experiment for myself and also a solution to a problem which was bugging me for a while and couldn't find an easy solution. The problem was that I have replaced my DVD drive in my laptop and put a HDD in the place of it. Unfortunately for some reasons the laptop was handling the HDD as a DVD drive so if there were no read/write actions then it has put it to sleep. If I was playing and the game wanted to load a cutscene it took several seconds while the HDD started spinning. That is solved by this windows service because it writes a temporary file on it in every second and then deletes it.

## License
MIT license.
