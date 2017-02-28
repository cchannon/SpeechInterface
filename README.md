# SpeechInterface
A basic IOT speech interface for Bot Framework chatbots

## Overview
This UWP App is a very basic example of how you could create a simple IOT speech interface for a chatbot built on Microsoft Bot Framework.

The solution, which mostly consists of two C# class files, uses basic speech-to-text and text-to-speech capabilities built into Win 10 IOT Core.

Using this, you can create your own personal assistant that responds to whatever voice commands you want. "Hey bot, unlock the door." "Hey bot, start the coffee maker." All you need is a bot and a bit of imagination.

## Setup
### The Bot:
I have not included any instructions or code for creating a bot. You'll have to set one of those up yourself, but there's plenty of resources:
* Main documentation page: https://docs.botframework.com/en-us/
* Examples for Node.js: https://docs.botframework.com/en-us/node/builder/guides/examples/
* Walkthrough of bot basics in .Net: https://docs.botframework.com/en-us/azure-bot-service/templates/basic/

More importantly, there are tons of people who have created bots and put the source out on Github and other places, just look around.

Once you've created your bot, this project will allow you to connect to it through the DirectLine Rest API. You'll have to register your bot and enable DirectLine from the bot management console. (really very easy, just go to MyBots --> select your bot --> click DirectLine and enable.

With DirectLine enabled, it should give you a key to access the bot. Paste that in to the DLConnector.cs class file in this repo - and presto! you're ready to push to the Pi.

---
### The Pi
For my prototype, I used a Raspberry Pi 3, a webcam (as the microphone) and an old portable speaker I had lying around. I also added a breadboard with two buttons, just because I like the tactile experience of using real buttons, but you could just as easily create buttons in the app interface and click them instead.

There's a Fritzing diagram in the root of my GitHub repo, but the wiring is very simple; the buttons are connected to Ground and GPIO 19 and 26. 26 starts a new Bot copnversation (I added this to make debugging dialogs easier) and 19 just continues in the same conversation.

---
### Win 10 IOT Core
If you've never pushed a UWP app to a Win 10 IOT Core device before, here's pretty much all the steps you will need:
* Start with a SD card with NOOBS
* Install latest Win 10 IOT Core from NOOBS (yes, it's free)
* Once Win 10 IOT Core boots (it can take a long time on the first boot), connect it to your wifi network
* Connect to your Pi from the computer where you're managing this repo's code using Powershell
* * Set the name, trusted hosts, reset the password, etc. (https://developer.microsoft.com/en-us/windows/iot/docs/powershell)
* If you have Visual Studio installed, open this repo solution and Release - to ARM architecture - to Device and select the Raspberry Pi you just set up.

Now, to run the app, navigate to the site your bot is broadcasting on your local network (should be ) click Apps and click Play next to the SpeechInterface app.

## Operation
If you've connected a monitor to the Pi, you should see a screen come up that will show the status of the device and what it believes you are saying. Click the New button to start a new conversation and the Continue button to continue in the same conversation.

Enjoy - and please keep in mind this is a very rough version. As you can see in the source code I still have a lot of TODO items. Please contribute if you are interested!