# Development of this Repository has concluded.

**OpenVRDesktopDisplayPortal is now [OVRdrop](https://github.com/Hotrian/OVRdrop-Public)!** The new version only supports Windows 8 or above, but is now GPU accelerated and capable of capturing in 1440p and above at 60+ FPS. A number of new features have been added, including an additional VR overlay that lets you switch target Applications as well as full monitor capture support and more, but the most significant change between this version and the Steam version is the capture method.

**OVRdrop is [now on Steam](http://store.steampowered.com/app/586210)!**

I intend to leave this Repository for the time being. You may use this repo for Educational and Personal uses only.

## OpenVRDesktopDisplayPortal

[![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=UK5EVMA4DFBWY)

OpenVRDesktopDisplayPortal is a Utility Application for SteamVR that can mirror a Desktop Window into a Cross Game SteamVR Overlay. OVRDDP works on SteamVR and should be fully compatible with any SteamVR headset, though you will need tracked controllers for many of the features. It is known to work for the HTC Vive as well as the Oculus Rift, though some games may not be compatible with the Rift; please check the [Compatibility Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility#mirrored-to) for more details. The only major downside is that Cross Game Overlays will always draw on top of game geometry because Depth information is not fed to the Compositor by SteamVR games.

To use this, you can **[download the latest release](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/releases)** and check out the **[Instructions Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Instructions)** to get started. Or, if you are more code savvy, [grab Unity 5.3.6f1](https://unity3d.com/get-unity/download/archive) and roll your own by cloning the repo or [downloading the source as a zip](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/archive/master.zip).

#### Notice regarding 'Launch as Administrator':
- OpenVRDesktopDisplayPortal shouldn't require being launched as an Admin. However, if you are launching Steam or SteamVR as Admin, OVRDDP will also require being launched as Admin. If you are launching an application as Admin, OVRDDP will only be able to send clicks to it if it is also being launched as Admin. Certain applications (such as Task Manager) will require OVRDDP is launched as Admin to send clicks.
- **If you are having any issues with OVRDDP, please try launching it as an Admin.**

#### HTC Vive Users:
- We have not yet found any incompatible games. OVRDDP should have 100% HTC Vive compatibility; though certain applications may break Overlay Interaction (mouse clicks, moving/rotation/scaling from VR); please check the [Compatibility Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility#controller-integration) for more details.

#### Oculus Rift Users:
- Certain games, even when launched through SteamVR, will insist on drawing directly to the Rift instead of through the SteamVR Compositor. Such games cannot utilize the SteamVR Overlay system, so these Overlays will not work there; please check the [Compatibility Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility#mirrored-to) for more details. It may be possible to launch Oculus SDK games through [Revive](https://github.com/LibreVR/Revive) to bring SteamVR compatibility to them, though you will lose Asynchronous Timewarp in trade for Reprojection. This is known to not work for Elite Dangerous due to their launcher system.
- **Elite Dangerous+OVRDDP on Rift is possible using [EDFX](http://edcodex.info/?m=tools&entry=58)** (thanks to [/u/jheggstrife](https://www.reddit.com/user/jheggstrife) for discovering this [here](https://www.reddit.com/r/Vive/comments/4x1pvh/openvrdesktopdisplayportal_now_has_controller/d7fklpq))! It might be possible to use EDFX to launch other games, but this has not been tested directly. The downside is that by running ED through SteamVR you will lose Asynchronous Timewarp for Reprojection. The upside is that you'll gain access to SteamVR Overlays including OpenVRDesktopDisplayPortal! **[/u/Exigeous](https://www.reddit.com/user/Exigeous) has created a guide to set this up [here on Reddit](https://www.reddit.com/r/EliteDangerous/comments/53j9ka/openvrdesktopdisplayportal_overlay_for_oculus?st=itafwp3c&sh=d94e0380)!**

## Features
- Clone almost any Desktop Window into VR.
- Works with _almost_ any SteamVR Compatible game.
  - Should work with any game that utilizes the SteamVR SDKs. On the Rift, certain Rift games will use the Oculus SDK even when you use SteamVR. On the Vive and other SteamVR compatible headsets every game should work.
- Easily Attach the Overlay to the World, the Screen (like a HUD), or one of the Controllers.
- Send Mouse Clicks through to the target application, without leaving VR. (World/Controller attached Overlays only). 
  - Allows for Left, Right, and Middle Click as well as ScrollWheel (Horizontal and Vertical wheels).
- Move, Rotate, and Scale the Overlay without leaving VR. (World attached Overlays only).
- The Overlay can Animate when you look at it, such as changing transparency, scale, or moving out of the way.
- Allows configurable Capture FPS.
  - 24 FPS should be perfect for movies/video, lower FPS recommended for applications such as Instant Messengers.
- Allows configurable Crop Region.
  - You can clone just a subset of a window instead of the whole window if desired.
- Configurable Outline for the Overlay (can disable if desired) to make the Overlay more visible.
- Configurable Quality Settings.
  - Clone a window as is or use Bilinear or Trilinear filtering to smooth edges, increasing perceived quality.
- Options to enable display of the Desktop Cursor on the Overlay when it moves over the target application.
- Haptic Feedback support!
- Save/Load support for Overlay profiles as well as target application capture settings.

## Table of Contents
- [Tutorials](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#tutorials)
- [Previews](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#previews)
- [Instructions](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#instructions)
- [Tested Applications](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#tested-applications)
- [Known Issues](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#known-issues)
- [Additional Notes / Tips & Tricks](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#additional-notes--tips--tricks)
- [How can I help?](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#how-can-i-help)
- [Special Thanks](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#special-thanks)

## Tutorials
- A short [tutorial on Youtube](https://www.youtube.com/watch?v=jjnyjf7RuMU) (Thanks [Bumble](https://www.youtube.com/channel/UCahG62_Yv1IpL2RIOCV88qQ)!)
- [Setup Elite Dangerous for OVRDDP on Rift](https://www.reddit.com/r/EliteDangerous/comments/53j9ka/openvrdesktopdisplayportal_overlay_for_oculus?st=itafwp3c&sh=d94e0380) (Thanks [/u/Exigeous](https://www.reddit.com/user/Exigeous)!)
- More coming soon.

## Previews
Desktop Application Views:
- [Default View](http://i.imgur.com/4fZXIyB.png)
- [Targeting SteamVR Status Window](http://i.imgur.com/viEEqUN.png)
- [Additional Settings Panel View](http://i.imgur.com/6VdnuAz.png)

Views from VR:
- [Netflix in Tiltbrush](https://thumbs.gfycat.com/TautHopefulFieldmouse-size_restricted.gif) ([Higher Quality Link](https://gfycat.com/TautHopefulFieldmouse))

See also the [Tutorials](https://github.com/Hotrian/OpenVRDesktopDisplayPortal#tutorials) section.
- More coming soon.

If there are any complaints regarding the content of these, please [raise an issue](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/issues/new) or contact one of the devs and they will be promptly removed, deleted, and/or replaced.

##Instructions
- Please check the [Instructions Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Instructions) for detailed instructions.

## Tested Applications
- Moved to the [Compatibility Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility) on the wiki.

## Known Issues
- Controllers are sometimes misidentified. 
  - To fix this, click the Swap Controllers button near the bottom to swap which controller is identified as which.
- Oculus Rift Users are reporting some games seem to be incompatible.
  - Please see [the issue posted here](https://github.com/Hotrian/OpenVRTwitchChat/issues/4), and the [Compatibility Article](https://github.com/Hotrian/OpenVRDesktopDisplayPortal/wiki/Compatibility) for more details.
- If you launch it and nothing happens in VR, try launching OVRDDP as an Administator. If it still does not work please try restarting your computer and launching it again.
  - If launching as an Administrator works, you can launch it that way by default by Right Clicking it and going to the Compatibility Tab and checking the box next to "Run this program as an Administrator".
- The current Capture API, the [GDI API](https://msdn.microsoft.com/en-us/library/windows/desktop/dd145203) is kind of slow. For best performance scale your window down. Faster Capture APIs will be Coming Soon™ that are capable of capturing the full resolution of your desktop at full speed.

## Additional Notes / Tips & Tricks
- When attaching Overlays to controllers, the offset is reoriented to match the Base Position's orientation.
  - X+ should always move the Overlay to the Right.
  - Y+ should always move the Overlay Up.
  - Z+ should always move Forward, relative to the Overlay.
- Check out some of the Default Profiles in the top right of the Desktop Application Interface for ideas on various Overlay setups, such as attaching it to one of the walls, up in the sky, or on one of the controllers.
- You can stream the Display Mirror if you want your viewers to be able to see the Overlay, or you can stream the game's output if you do not.
- Smaller windows can be captured faster, and look about the same unless you scale the Overlay up really large.

## How can I help?

If you know how to program, we could always use help! Feel free to fork the repo and improve it in any way you see fit; but if you don't know how but still want to contribute, we always need more beta testers! Download the release and share it around! If you want to do even more, donations are always cool too! You'll be funding my programming endeavors, including cool projects like these VR Overlays: [![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=UK5EVMA4DFBWY)

## Special Thanks

(No endorsements are intended to be implied.)

- Thanks to [Eric Daily](http://tutsplus.com/authors/eric-daily) for the base [SaveLoad](http://gamedevelopment.tutsplus.com/tutorials/how-to-save-and-load-your-players-progress-in-unity--cms-20934) script! The license file is available [here](../master/SaveLoad-LICENSE.txt).
- Thanks to [Bumble](https://www.youtube.com/channel/UCahG62_Yv1IpL2RIOCV88qQ) ([/u/oBumble](https://www.reddit.com/user/oBumble)) for this short [tutorial on Youtube](https://www.youtube.com/watch?v=jjnyjf7RuMU)!
- Thanks to [judah4](https://github.com/judah4) for the base [HSVPicker](https://github.com/judah4/HSV-Color-Picker-Unity) script! The license file is available [here](../master/HSVPicker-LICENSE.txt).
- Thanks to everyone who has tested it so far! The feedback has really helped speed things along!
