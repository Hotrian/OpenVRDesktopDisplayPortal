[![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=UK5EVMA4DFBWY)

**This Readme has not been updated for v1.0.5. Please see the Release notes for v1.0.5 until this has been updated.**

**The following information still largely applies to v1.0.5, but none of the new features are mentioned.**

This is a stripped down version of the SteamVR Unity Plugin with a custom Overlay script that displays _almost_ any Desktop Window in VR :D

To use this download the [latest release](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/releases), then click on the dropdown menu at the top left (see the example graphic down below), then configure the overlay to your liking :). You can hold your mouse over the UI elements for some hints on usage. More detailed instructions are down below.

**Note:** Overlays will always draw ontop of other game geometry. This is less noticable if you attach them to the controller since things rarely come between you and the controllers. Otherwise you may want to attach them to the screen, or put them on a wall behind you or up in the sky.

#### Oculus Rift users:
We're receiving reports that some Rift users find some games are incompatible with the SteamVR Overlay system. Please check the [compatibility](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility) article for a list of tested games. You can read more about it in [the issue posted here](https://github.com/Hotrian/OpenVRTwitchChat/issues/4). The jist of things is that some games seem to skip the SteamVR Compositor and draw directly to the Rift instead. Check the SteamVR Display Mirror and see if you can see the Overlays there. If you can see the Overlays in the Mirror but not the Rift, then that game is probably incompatible :(. Please post your findings in [the issue](https://github.com/Hotrian/OpenVRTwitchChat/issues/4).

#### Update for Rift users:
We have at least [one confirmation](https://www.reddit.com/r/EliteDangerous/comments/4tr3gx/you_can_now_watch_netflixyoutubemovies_in_elite/d5khcj0?context=3) that you _might_ be able to use [Revive](https://github.com/LibreVR/Revive) to trick those Rift games into drawing through SteamVR instead of Direct-To-Rift. It will be funny if this turns out to be a solution :). Please test it out if you can!

#### If you can't find the Overlay:
- Some games do not support Overlays. Check the [compatibiltiy article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility) for more information.
- Firstly, you can switch anchor devices using the Dropdown in the very bottom Left. It should say "World", "Screen", "LeftController", or "RightController" which will tell you what device it is anchored to and where you should be looking. If it has identified the wrong controller as left/right, you can click the "Swap Controllers" button to right the labels.
- World Overlays should spawn at the world center, facing backwards in your play area (when you are facing forward you are facing the correct direction to see it). Try circling the room and looking on the floor in the middle of the room. Try switching to another device, if it cannot be found.
- Screen Overlays should spawn at 0x, 0y, 1z. At 0z the Overlay is directly in the middle of your HMD and thus not forward enough to be seen. Try adjusting the position to 0x, 0y, 1z and see if you can find it, or try one of the other devices.
- Controller Overlays should spawn attached to your controller. Try checking the back of your controller or looking below it or above it. The Overlay might be hiding out of sight on one of the back sides of the controller. If it cannot be found, again, try one of the other devices.
- If you have tried each of these steps and the Overlay just can't be found, open an [issue](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/issues) and we'll try to resolve it. If you don't have and don't want to make a GitHub account, message me on Reddit.

## Table of Contents
- [Features](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#features)
- [Tutorials](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#tutorials)
- [Previews](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#previews)
- [Instructions](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#instructions)
- [Tested Applications](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#tested-applications)
- [Known Issues](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#known-issues)
- [Additional Notes / Tips & Tricks](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#additional-notes--tips--tricks)
- [How can I help?](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#how-can-i-help)
- [Special Thanks](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#special-thanks)

## Features
- See your favorite Desktop application in VR! From _almost_ any SteamVR game!
- Easily attach Overlays to the Screen, a Tracked Controller, or drop it in the World.
- Easily snap Controller attached Overlays to a set "Base Position".
- Offset Overlays positionally and rotationally.
- Basic Gaze Detection and Animation support (Fade In/Out and/or Scale Up/Down on Gaze).
- Basic Save/Load Support! Saves most settings right now. Window Cropping and Capture settings are saved per application automatically; Overlay settings are saved by the profile system on the top right.
- Multiple windows! Run the exe multiple times and configure them as desired!

## Tutorials
- A short [tutorial on Youtube](https://www.youtube.com/watch?v=jjnyjf7RuMU) (Thanks [Bumble](https://www.youtube.com/channel/UCahG62_Yv1IpL2RIOCV88qQ)!)
- More coming soon.

## Previews
Desktop Application Views:
- [Default View](http://i.imgur.com/AVUvNsZ.png)

Views from VR:
- [Netflix in Tiltbrush](https://thumbs.gfycat.com/TautHopefulFieldmouse-size_restricted.gif) ([Higher Quality Link](https://gfycat.com/TautHopefulFieldmouse))

See also the [Tutorials](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#tutorials) section.
- More coming soon.

If there are any complaints regarding the content of these, please [raise an issue](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/issues/new) or contact one of the devs and they will be promptly removed, deleted, and replaced.

## Instructions
(Some of these are slightly outdated, but still relevant, new images and up to date instructions coming soon)

(These instructions have not been updated for Controller Integration, please see the [v1.0.5 release notes](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/releases/tag/v1.0.5-alpha) for instructions)
- Mouseover any UI elements for tooltips.
- The controls [on the top left](http://i.imgur.com/798RPdH.png) control which window is being mirrored into VR, as well as the framerate, whether or not "Direct Capture" is being used, and the minimize/restore state of the selected window.
- Direct Capture targets the window before it is processed by the [DWM](https://en.wikipedia.org/wiki/Desktop_Window_Manager). Because of this, some special effects are missing such as the window border (including titlebar). The benefit is that Direct Capture is faster and can capture windows even if they are behind other windows. As far as I know, the only reason not to use Direct Capture is because some programs don't support it. If you select a window and just get a blank display try disabling Direct Capture.
- Most applications do not draw internally when they are minimized. Because of this, you can only capture applications that are not minimized in most cases. The lower toggle button on the top left should be able to control the minimize/restore state of the selected application.
- The little recycle icon to the right of the application dropdown refreshes the list of windows. You'll can click this to make the dropdown update with the list of current windows, or open and close the dropdown which will refresh it.
- Some windows can only be captured while they are on screen and dragging them off screen will cause part of their display to appear blank or stop updating.
- The [four input boxes at the top](http://i.imgur.com/CeWuxvI.png) control the size and position of the image being captured. The full image is displayed by default, but you can adjust these values to cut out parts of the window :). Mouseover them and the tooltip will tell you which is which.
- The controls [on the bottom left](http://i.imgur.com/DyEz9D0.png) control the Offset, Rotation, and Base Position (Base Position for controllers only). The Sliders are linked to the text boxes directly to their left. You can choose to use the sliders or the text boxes to get your overlay exactly where you want it. The base positions just make it a little quicker to attach the overlay to somewhere fancy on your controller, such as behind it or below it.
- The [bottom right](http://i.imgur.com/94vljYe.png) controls the Alpha and Scale of the overlay. You can also choose an animation (fade in/out and/or scale up/down) that will occur when you look directly at the overlay. Again, mouseover these to see exactly which one does what.
- Finally, The [top right](http://i.imgur.com/D1y3vjd.png) controls allow you to Save/Load profiles for the Overlay settings. The window crop settings will save on a per application basis as you adjust them, but the position/rotation/alpha/scale/animation for the overlay are saved and loaded through this interface.
- One last tip is that the window is resizeable and [you can make it pretty small](http://i.imgur.com/7qIhgEr.png) without overlapping any of the controls if you want to :).

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
- Smaller windows will run much faster, and look about the same in VR. I have a pretty weak GPU but if you have a 1080 let me know how well it works :).

## How can I help?

If you know how to program, we could always use help! Feel free to fork the repo and improve it in any way you see fit; but if you don't know how but still want to contribute, we always need more beta testers! Download the release and share it around! If you want to do more, donations are always cool too! You'll be funding my programming endeavors, including cool projects like these VR Overlays: [![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=UK5EVMA4DFBWY)

## Special Thanks

(No endorsements are intended to be implied.)

- Thanks to [Eric Daily](http://tutsplus.com/authors/eric-daily) for the base [SaveLoad](http://gamedevelopment.tutsplus.com/tutorials/how-to-save-and-load-your-players-progress-in-unity--cms-20934) script! The license file is available [here](../master/SaveLoad-LICENSE.txt).
- Thanks to [Bumble](https://www.youtube.com/channel/UCahG62_Yv1IpL2RIOCV88qQ) for this short [tutorial on Youtube](https://www.youtube.com/watch?v=jjnyjf7RuMU)!
- Thanks to [judah4](https://github.com/judah4) for the base [HSVPicker](https://github.com/judah4/HSV-Color-Picker-Unity) script! The license file is available [here](../master/HSVPicker-LICENSE.txt).
- Thanks to everyone who has tested it so far! The feedback has really helped speed things along!
