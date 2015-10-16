# Zumo .NET Server SDK

This repo contains the .NET backend for **Azure Mobile Apps**. The Zumo .NET backend is a peer to the Node.js
backend, giving users a choice in backend platform.

## Download Source Code

To get the source code and a sample backend via **git** just type:

    git clone https://github.com/Azure/azure-mobile-apps-net-server.git
    cd ./azure-mobile-apps-net-server/

## Building and Running Tests

The solution must be built in Visual Studio 2013 or 2015.

1. Open the ```ServerSDK.sln``` solution file in Visual Studio 2012.
2. Press ```F7``` or ```Ctrl```+```Shift```+```B``` to build the solution.

### Running the Tests

After you've built the SDK, open the Test Explorer in Visual Studio (Test->Windows->Test Explorer) or search "Test Explorer" in the Quick Launch bar (```Ctrl```+```Q```) in VS 2015.  In the window, press "Run all" or use the dropdown to run a subset of tests.

## Running the Sample App

There is a sample web project in the 'sample' folder in the solution.  It is contains several controllers which excersise several of the features of the SDK.

  * **BrownOnlineController** - Description here
  * **CustomController** - Description here
  * **GreenController** - Description here
  * **PersonController** - Description here
  * **SecuredController** - Description here

To run the sample project:

* Right click on the 'SampleApp' project and select 'Set as startup project'.
* Hit ```F5``` to run the project in the browser of your choice (or ```Ctrl```+```F5``` to start without debugging).
* A welcome page should show, indicating the service is running.
* You can now issue to the backend from your browser.  Try these examples by adding them to the end of the url:
  * EXAMPLE HERE
  * EXAMPLE HERE
  * EXAMPLE HERE
  * EXAMPLE HERE

## Useful Resources

* Tutorials and product overview are available at [Microsoft Azure Mobile Services Developer Center](http://azure.microsoft.com/en-us/develop/mobile).
* Our product team actively monitors the [Mobile Services Developer Forum](http://social.msdn.microsoft.com/Forums/en-US/azuremobile/) to assist you with any troubles.

## Contribute Code or Provide Feedback

If you would like to become an active contributor to this project please follow the instructions provided in [Microsoft Azure Projects Contribution Guidelines](http://azure.github.com/guidelines.html).

If you encounter any bugs with the library please file an issue in the [Issues](https://github.com/Azure/azure-mobile-services/issues) section of the project.