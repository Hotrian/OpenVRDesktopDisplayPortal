using System;
using UnityEngine;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class VLCPlayerController : MonoBehaviour
{
    public static VLCPlayerController Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<VLCPlayerController>()); }
    }

    private static VLCPlayerController _instance;

    private Process _vlcProcess;

    private bool _processPlaying;

    private bool _processEnded = true;

    public void OnEnable()
    {
        if (!_processPlaying && _processEnded)
        {
            Debug.Log("Starting VLC");
            _processPlaying = true;
            _processEnded = false;

            string vidPath = "\"drive:\\path\\to\\file\""; // Filename removed for Github Upload

            string options = vidPath + " --ignore-config --no-crashdump -I=dummy --no-mouse-events --no-interact --no-video-deco --no-qt-privacy-ask --qt-minimal-view --play-and-exit --no-keyboard-events --video-title-timeout=0 --no-interact --no-repeat --no-loop";

            _vlcProcess = new Process
            {
                StartInfo =
                {
                    FileName = "C:\\Program Files (x86)\\VLC\\vlc.exe",
                    Arguments = options,
                    //CreateNoWindow = true,
                    //WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            Debug.Log("Started: " + _vlcProcess.Start());
        }
    }

    public void OnDisable()
    {
        StopVideo();
    }

    public void StopVideo()
    {
        if (!_processPlaying) return;
        Debug.Log("Stopping VLC");
        KillVLCProcess();
        _processPlaying = false;
    }


    private void KillVLCProcess()
    {
        try
        {
            Debug.Log("Killing VLC");
            _vlcProcess.Kill();
            _processEnded = true;
        }
        catch (Exception)
        {
        }
    }
}
