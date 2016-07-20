[![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=UK5EVMA4DFBWY)

This is a stripped down version of the SteamVR Unity Plugin with a custom Overlay script that displays _almost_ any Desktop Window in VR :D

To use this download the [latest release](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/releases), then click on the dropdown menu at the top left (see the example graphic down below), then configure the overlay to your liking :). You can hold your mouse over the UI elements for some hints on usage. More detailed instructions are down below.

See also my [OpenVRTwitchChat](https://github.com/Hotrian/OpenVRTwitchChat) program that specializes putting Twitch Chat into VR. It is a bit more refined ;]

**Note:** Overlays will always draw ontop of other game geometry. This is less noticable if you attach them to the controller since things rarely come between you and the controllers. Otherwise you may want to attach them to the screen, or put them on a wall behind you or up in the sky.

#### Oculus Rift users:
We're receiving reports that some Rift users find some games are incompatible with the SteamVR Overlay system. You can read more about it in [the issue posted here](https://github.com/Hotrian/OpenVRTwitchChat/issues/4). The jist of things is that some games seem to skip the SteamVR Compositor and draw directly to the Rift instead. Check the SteamVR Display Mirror and see if you can see the Overlays there. If you can see the Overlays in the Mirror but not the Rift, then that game is probably incompatible :(. Please post your findings in [the issue](https://github.com/Hotrian/OpenVRTwitchChat/issues/4).

## Table of Contents
- [Example](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#example)
- [Features](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#features)
- [Demos](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#demos)
- [Instructions](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#instructions)
- [Tested Applications](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#tested-applications)
- [Known Issues](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#known-issues)
- [Additional Notes / Tips & Tricks](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#additional-notes--tips--tricks)
- [How can I help?](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#how-can-i-help)
- [Special Thanks](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#special-thanks)

## Example
![example](http://i.imgur.com/dQHNuGP.png)

## Features
- See your favorite Desktop application in VR! From _almost_ any SteamVR game!
- Easily attach Overlays to the Screen, a Tracked Controller, or drop it in the World.
- Easily snap Controller attached Overlays to a set "Base Position".
- Offset Overlays positionally and rotationally.
- Basic Gaze Detection and Animation support (Fade In/Out and/or Scale Up/Down on Gaze).
- Basic Save/Load Support! Only saves some settings right now.
- Multiple windows! Run the exe multiple times and configure them as desired!

## Demos
Netflix in Tiltbrush:
- ![Netflix in VR](https://thumbs.gfycat.com/TautHopefulFieldmouse-size_restricted.gif) ([Higher Quality Link](https://gfycat.com/TautHopefulFieldmouse))
- VLC in VR [quality check](https://vid.me/x35w) on my weak R7 265. When not recording the lip sync is perfect though :)
- VLC in VR [showing it attached to a controller](https://vid.me/ohee). This one recorded pretty laggy for some reason but still shows that these attach to your controller if desired :) Also demos FPS control.

## Instructions
- The controls [on the top left](http://image.prntscr.com/image/a26fd89c2f81433f942e3f5a76740b3d.png) control which window is being mirrored into VR, as well as the framerate and whether or not "Direct Capture" is being used.
- Direct Capture targets the window before it is processed by the [DWM](https://en.wikipedia.org/wiki/Desktop_Window_Manager). Because of this, some special effects are missing such as the window border (including titlebar). The benefit is that Direct Capture is faster and can capture windows even if they are behind other windows. As far as I know, the only reason not to use Direct Capture is because some programs don't support it. If you select a window and just get a blank display try disabling Direct Capture.
- The little recycle icon [to the right of the application dropdown](http://image.prntscr.com/image/a26fd89c2f81433f942e3f5a76740b3d.png) refreshes the list of windows. You'll have to click this to make the dropdown update with the list of current windows.
- Most applications do not draw internally when they are minimized. Because of this, you can only capture applications that are not minimized in most cases. There is a [toggle button](http://image.prntscr.com/image/011be138eb1c448d993ee513ec9889d9.png) to the right of the four input boxes at the top, that should be able to minimize/restore windows for you.
- The [four input boxes at the top](http://image.prntscr.com/image/011be138eb1c448d993ee513ec9889d9.png) control the size and position of the image being captured. Mouseover them to see which does which, but from left to right they are X, Y, Width, Height. The full image is displayed by default, but you can adjust these values to cut out parts of the window :)
- The controls [on the bottom left](http://image.prntscr.com/image/200693763c494a57a2d74c382bee7038.png) control the Offset, Rotation, and Base Position (for controllers only). The Sliders are linked to the text boxes directly to their left. You can choose to use the sliders or the text boxes to get your overlay exactly where you want it. The base positions just make it a little quicker to attach the overlay to somewhere fancy on your controller, such as behind it or below it.
- Finally, The [bottom right](http://image.prntscr.com/image/61e64d0420f144409345f4a6b96c31f6.png) controls the Alpha and Scale of the overlay. You can also choose an animation (fade in/out and/or scale up/down) that will occur when you look directly at the overlay. Again, mouseover these to see exactly which one does what.
- One last tip is that the window is resizeable and [you can make it pretty small](http://i.imgur.com/Mjy24cv.png) without overlapping any of the controls if you want to :).

## Tested Applications
Moved to the [Compatibility Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility) on the wiki.

## Known Issues
- SteamVR_ControllerManager.cs doesn't correctly auto-identify controllers for me, so I wrote my own manager, HOTK_TrackedDeviceManager.cs. My Device Manager is super pre-alpha but should correctly identify both Controllers as long as at least one of them is assigned to either the left or right hand, and they are both connected. If neither Controller is assigned to a hand, they are assigned on a first come first serve basis. If only one Controller is connected, and it isn't already assigned, it will be assigned to the right hand.
- Oculus Rift Users are reporting some games seem to be incompatible, please see [the issue posted here](https://github.com/Hotrian/OpenVRTwitchChat/issues/4).
- If you launch it and nothing happens in VR, check the Data folder for output_log.txt and look for "Connect to VR Server Failed (301)" if you're getting that error, try restarting your PC, and if that doesn't work [try this solution](https://www.reddit.com/r/Vive/comments/4p9hxg/wip_i_just_released_the_first_build_of_my_cross/d4kmvrj) by /u/GrindheadJim:

>For clarification, what I did was right-click on the .exe file, clicked "Properties", went to the "Compatibility" tab, and checked the box under "Run as Administrator". For the record, I have also done this with Steam and SteamVR. If you're having any issues with any of these programs, I would start there. 

## Additional Notes / Tips & Tricks
- When attaching Overlays to controllers, the offset is reoriented to match the Base Position's orientation. X+ should always move the Overlay to the Right, Y+ should always move Up, and Z+ should always move Forward, relative to the Overlay.
- You can put the Overlay up in the sky and tilt it if you don't like it on the controllers and find it obtrusive in the world. Just set the Base Position to "World", then mess with the middle "Positional Control" slider and the top "Rotational Control" slider until you find a position that works for you :)
- You can stream the Display Mirror if you want your viewers to be able to see the Overlay, or you can stream the game's output if you do not.
- Save currently only saves information about each application (saves Direct Capture on/off status, plus the X/Y/Width/Height info and recalls it the next time you select that application again).
- Smaller windows will run much faster, and look about the same in VR. I have a pretty weak GPU but if you have a 1080 let me know how well it works :).

## How can I help?

If you know how to program, we could always use help! Feel free to fork the repo and improve it in any way you see fit; but if you don't know how but still want to contribute, we always need more beta testers! Download the release and share it around! If you want to do more, donations are always cool too! You'll be funding my programming endeavors, including cool projects like these VR Overlays: [![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=UK5EVMA4DFBWY)

## Special Thanks

(No endorsements are intended to be implied.)

Thanks to [Eric Daily](http://tutsplus.com/authors/eric-daily) for the base [SaveLoad](http://gamedevelopment.tutsplus.com/tutorials/how-to-save-and-load-your-players-progress-in-unity--cms-20934) script! The license file is available [here](../master/SaveLoad-LICENSE.txt).

Thanks to everyone who has tested it so far! The feedback has really helped speed things along!
