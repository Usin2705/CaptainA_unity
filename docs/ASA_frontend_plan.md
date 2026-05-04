# ASA Front-End Development Plan

This document preserves the original front-end development brief that was previously kept in the main README. It is retained for historical project context and implementation notes.

## Front-End Development Project Description

Our main goals for front-end project is to develop a automatic speech asessment (ASA) feature and implement new UI/UX design to the mobile app. Specifically:

- A new **ASA feature** in the mobile app: an interface to display a speaking task (usually with picture and text description - with option for translation). User then speak and record their answer (about 30~60s). The mobile app will then send the speech data to the server. After the server finish, mobile app will receive 5 speech ratings scores: `fluency, pronunciation, range, accuracy, holistic` and display them to the user. Those data will be store on their mobile app, and user can later access them in a different interface to review their progress.
    -  We will also need an interface/function to collect user's consent and some background information. Those will also be sent to the server (with consent).
	-  Some functions (audio record, server connection) are already available, you can reuse them.
- As a real mobile app, we also targeting user experience (**UX**) and user interface (**UI**). Therefore, we also need a nice front end.
  - A spider chart (as we discussed) could be a nice way to display the scores.
  - Unity's Animation System is also nice and surprisingly easy to implement, but not required.
  - Obviously you will need to work closely with back end and also the design team. Some resources may not be available to you until the end of the project (for example, new icon design).
  - The processing time could be from 10~30s, we need to figure out a way to let's user know the server is processing. Some extra feature to collect feedback during waiting, or after getting the score is extremely useful.
  - Remember that the mobile app target both Android and iOS, so the UI must work fine in most smartphone with different screensize.
- Other features that not directly related to ASA features but are also needed (for example, interface for text-to-speech system, we will handle the server). Obviously those extra is not priority and depend on the team and the progress of the main work.

For an example of user interface (we need to make a much better one in production), see: https://www.youtube.com/watch?v=cRskPKsSM3g

See the function `ServerPost` or `NumberGamePost` (at the bottom of https://github.com/Usin2705/CaptainA_unity/blob/main/Assets/Scripts/Managers/NetworkManager.cs) on how to send/receive data to/from the server.

For more information about how the backend would look like, you can look at SaySvenska server: https://github.com/Usin2705/SaySvenska/tree/main/Server

You can look at an example of the API (a bit old now) from SaySuomi Readme file:
https://github.com/Usin2705/CaptainA_unity/tree/main
