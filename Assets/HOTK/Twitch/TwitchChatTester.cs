using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.UI;
using Valve.VR;

[RequireComponent(typeof(TwitchIRC), typeof(TextMesh))]
public class TwitchChatTester : MonoBehaviour
{
    public static TwitchChatTester Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<TwitchChatTester>()); }
    }

    private static TwitchChatTester _instance;

    public struct TwitchChat
    {
        public readonly string Name;
        public readonly string Color;
        public readonly string Message;

        public TwitchChat(string name, string color, string message)
        {
            Name = name;
            Color = color;
            Message = message;
        }
    }

    public InputField UsernameBox;
    public InputField OAuthBox;
    public InputField ChannelBox;
    public Button ConnectButton;
    public Text ConnectButtonText;

    public TextMesh TextMesh
    {
        get { return _textMesh ?? (_textMesh = GetComponent<TextMesh>()); }
    }
    private TextMesh _textMesh;

    public TextMesh ViewerCountTextMesh;
    public TextMesh ChannelNameTextMesh;

    public AudioSource IncomingMessageSoundSource1;
    public AudioSource IncomingMessageSoundSource2;
    public AudioSource IncomingMessageSoundSource3;
    public AudioSource IncomingMessageSoundSource4;
    public AudioSource IncomingMessageSoundSource5;
    public AudioSource IncomingMessageSoundSource6;

    public AudioSource NewFollowerSoundSource;

    public TwitchIRC IRC
    {
        get { return _irc ?? (_irc = GetComponent<TwitchIRC>()); }
    }
    private TwitchIRC _irc;

    private readonly List<TwitchChat> _userChat = new List<TwitchChat>();

    public void OnEnable()
    {
        TextMesh.text = "";
        TextMesh.GetComponent<Renderer>().enabled = true;
        IRC.enabled = true;
    }

    public void OnDisable()
    {
        TextMesh.text = "";
        TextMesh.GetComponent<Renderer>().enabled = false;
        IRC.enabled = false;
    }

    public bool Connected
    {
        get; private set;
    }

    public void Awake()
    {
        _instance = this;
    }

    public void Start()
    {
        ClearViewerCountAndChannelName("Disconnected");
        StartCoroutine("SyncWithSteamVR");
    }

    private readonly Stopwatch _messageSoundStopwatch = new Stopwatch(); // Used to prevent message sound spamming
    private readonly Stopwatch _newFollowerSoundStopwatch = new Stopwatch(); // Used to prevent message sound spamming

    public void ToggleConnect()
    {
        if (!Connected)
        {
            if (UsernameBox != null && UsernameBox.text != "")
            {
                if (OAuthBox != null && OAuthBox.text != "")
                {
                    if (ChannelBox != null && ChannelBox.text != "")
                    {
                        if (ChannelBox.text.Contains(" "))
                        {
                            AddSystemNotice("Channel name invalid!", TwitchIRC.NoticeColor.Red);
                            return;
                        }
                        UsernameBox.interactable = false;
                        OAuthBox.interactable = false;
                        ChannelBox.interactable = false;
                        ConnectButtonText.text = "Press to Disconnect";

                        Connected = true;
                        OnChatMsg(TwitchIRC.ToTwitchNotice(string.Format("Logging into #{0} as {1}!", ChannelFirstLetterToUpper(ChannelBox.text), FirstLetterToUpper(UsernameBox.text))));
                        IRC.NickName = UsernameBox.text;
                        IRC.Oauth = OAuthBox.text;
                        IRC.ChannelName = ChannelBox.text.Trim().ToLower();

                        IRC.enabled = true;
                        IRC.MessageRecievedEvent.AddListener(OnChatMsg);
                        IRC.StartIRC();
                        knownFollowers.Clear();
                        StopCoroutine("UpdateViews");
                        StopCoroutine("UpdateFollowers");
                        StopCoroutine("SyncWithSteamVR");
                        gettingInitialFollowers = true;
                        StartCoroutine("UpdateViews");
                        StartCoroutine("UpdateFollowers");
                        StartCoroutine("SyncWithSteamVR");
                    }
                    else AddSystemNotice("Unable to Connect: Enter a Valid Channel Name!", TwitchIRC.NoticeColor.Red);
                }
                else AddSystemNotice("Unable to Connect: Enter a Valid OAuth Key! http://www.twitchapps.com/tmi/", TwitchIRC.NoticeColor.Red);
            }
            else AddSystemNotice("Unable to Connect: Enter a Valid Username!", TwitchIRC.NoticeColor.Red);
        }
        else
        {
            UsernameBox.interactable = true;
            OAuthBox.interactable = true;
            ChannelBox.interactable = true;
            ConnectButtonText.text = "Press to Connect";

            Connected = false;
            IRC.MessageRecievedEvent.RemoveListener(OnChatMsg);
            IRC.enabled = false;
            OnChatMsg(TwitchIRC.ToTwitchNotice("Disconnected!", TwitchIRC.NoticeColor.Red));
            knownFollowers.Clear();
            StopCoroutine("UpdateViews");
            StopCoroutine("UpdateFollowers");
            ClearViewerCountAndChannelName("Disconnected");
        }
    }

    IEnumerator SyncWithSteamVR()
    {
        while (Application.isPlaying)
        {
            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                var trackingSpace = compositor.GetTrackingSpace();
                SteamVR_Render.instance.trackingSpace = trackingSpace;
            }
            yield return new WaitForSeconds(10f);
        }
    }

    private Dictionary<uint, FollowsData> knownFollowers = new Dictionary<uint, FollowsData>();
    private bool gettingInitialFollowers;

    IEnumerator UpdateFollowers()
    {
        while (Connected && IRC.ChannelName.Length > 0)
        {
            WWW www = new WWW(URLAntiCacheRandomizer("https://api.twitch.tv/kraken/channels/" + IRC.ChannelName + "/follows"));
            yield return www;
            FollowsDataFull obj = JsonUtility.FromJson<FollowsDataFull>(www.text);
            if (obj != null)
            {
                if (obj.follows != null)
                {
                    if (obj.follows.Length > 0)
                    {
                        foreach (var follower in obj.follows)
                        {
                            if (!knownFollowers.ContainsKey(follower.user._id))
                            {
                                knownFollowers.Add(follower.user._id, follower);
                                if (!gettingInitialFollowers)
                                {
                                    OnChatMsg(TwitchIRC.ToTwitchNotice(follower.user.display_name + " is now following!", TwitchIRC.NoticeColor.Purple));
                                    PlayNewFollowerSound();
                                }
                            }
                        }
                        gettingInitialFollowers = false;
                    }
                }
            }
            yield return new WaitForSeconds(30f);
        }
    }

    public static string URLAntiCacheRandomizer(string url)
    {
        var r = "";
        r += UnityEngine.Random.Range(1000000, 8000000).ToString();
        r += UnityEngine.Random.Range(1000000, 8000000).ToString();
        var result = url + "?p=" + r;
        return result;
    }

    IEnumerator UpdateViews()
    {
        while (Connected && IRC.ChannelName.Length > 0)
        {
            WWW www = new WWW("https://api.twitch.tv/kraken/streams/" + IRC.ChannelName);
            yield return www;
            ChannelDataFull obj = JsonUtility.FromJson<ChannelDataFull>(www.text);
            if (obj != null)
            {
                if (obj.stream != null)
                {
                    if (obj.stream.channel != null)
                    {
                        if (ChannelNameTextMesh != null)
                        {
                            var text = "";
                            if (!string.IsNullOrEmpty(obj.stream.channel.display_name)) text = string.Format("#{0}", obj.stream.channel.display_name);
                            else if (!string.IsNullOrEmpty(obj.stream.channel.name)) text = string.Format("#{0}", obj.stream.channel.name);
                            else text = "Not Streaming";
                            ChannelNameTextMesh.text = text;
                        }
                        if (ViewerCountTextMesh != null) ViewerCountTextMesh.text = string.Format("Viewers: {0}", obj.stream.viewers);
                    }
                    else
                    {
                        ClearViewerCountAndChannelName();
                    }
                }
                else
                {
                    ClearViewerCountAndChannelName();
                }
            }
            yield return new WaitForSeconds(10f);
        }
    }

    private void ClearViewerCountAndChannelName(string channelText = null)
    {

        if (ChannelNameTextMesh != null) ChannelNameTextMesh.text = (channelText ?? "");
        if (ViewerCountTextMesh != null) ViewerCountTextMesh.text = "";
    }

    private void OnChatMsg(string msg)
    {
        var cmd = msg.Split(' ');
        var nickname = cmd[0].Split('!')[0].Substring(1);
        var mode = cmd[1];
        var channel = cmd[2].Substring(1);
        var len = cmd[0].Length + cmd[1].Length + cmd[2].Length + 4;
        var chat = msg.Substring(len);

        switch (mode)
        {
            case "NOTICE":
                // Compatability with real Twitch System messages
                if (nickname == "tmi.twitch.tv")
                {
                    nickname = "Twitch";
                    if (chat.StartsWith("Error"))
                        channel = "System-Red";
                    else if (chat == "Login unsuccessful")
                        channel = "System-Red";
                }
                // Convert Notice to Name Color
                switch (channel)
                {
                    case "System-Green":
                        AddMsg(nickname, TwitchIRC.ColorToHex(new Color(0f, 1f, 0f)), chat);
                        break;
                    case "System-Red":
                        AddMsg(nickname, TwitchIRC.ColorToHex(new Color(1f, 0f, 0f)), chat);
                        break;
                    case "System-Blue":
                        AddMsg(nickname, TwitchIRC.ColorToHex(new Color(0f, 0.4f, 1f)), chat);
                        break;
                    case "System-Yellow":
                        AddMsg(nickname, TwitchIRC.ColorToHex(new Color(1f, 1f, 0f)), chat);
                        break;
                    case "System-Purple":
                        AddMsg(nickname, TwitchIRC.ColorToHex(new Color(1f, 0f, 1f)), chat);
                        break;
                    default:
                        AddMsg(nickname, TwitchIRC.ColorToHex(new Color(1f, 1f, 1f)), chat);
                        break;
                }
                break;
            case "PRIVMSG":
                AddMsg(FirstLetterToUpper(nickname), TwitchIRC.GetUserColor(nickname), chat);
                PlayMessageSound();
                break;
        }
    }

    /// <summary>
    /// Set the Pitch of the Chat Message sound
    /// </summary>
    /// <param name="pitch"></param>
    public void SetMessagePitch(float pitch)
    {
        IncomingMessageSoundSource1.pitch = pitch;
        IncomingMessageSoundSource2.pitch = pitch;
        IncomingMessageSoundSource3.pitch = pitch;
        IncomingMessageSoundSource4.pitch = pitch;
        IncomingMessageSoundSource5.pitch = pitch;
        IncomingMessageSoundSource6.pitch = pitch;
    }

    /// <summary>
    /// Set the AudioClip to be played when Chat Messages are received
    /// </summary>
    /// <param name="sound"></param>
    public void SetMessageSound(AudioClip sound)
    {
        IncomingMessageSoundSource1.clip = sound;
        IncomingMessageSoundSource2.clip = sound;
        IncomingMessageSoundSource3.clip = sound;
        IncomingMessageSoundSource4.clip = sound;
        IncomingMessageSoundSource5.clip = sound;
        IncomingMessageSoundSource6.clip = sound;
    }

    /// <summary>
    /// Set the Volume of the Chat Message sound
    /// </summary>
    /// <param name="volume"></param>
    public void SetMessageVolume(float volume)
    {
        IncomingMessageSoundSource1.volume = volume;
        IncomingMessageSoundSource2.volume = volume;
        IncomingMessageSoundSource3.volume = volume;
        IncomingMessageSoundSource4.volume = volume;
        IncomingMessageSoundSource5.volume = volume;
        IncomingMessageSoundSource6.volume = volume;
    }

    public void PlayMessageSound()
    {
        // Prevent the message sound from spamming too rapidly
        if (_messageSoundStopwatch.IsRunning)
        {
            if (_messageSoundStopwatch.ElapsedMilliseconds < 50) return;
            _messageSoundStopwatch.Reset();
            _messageSoundStopwatch.Start();
        }
        else
            _messageSoundStopwatch.Start();

        // Play the message sound on an available channel (allows simultaneous message sounds)
        if (IncomingMessageSoundSource1 != null && IncomingMessageSoundSource1.clip != null && !IncomingMessageSoundSource1.isPlaying) IncomingMessageSoundSource1.Play();
        else if (IncomingMessageSoundSource2 != null && IncomingMessageSoundSource2.clip != null && !IncomingMessageSoundSource2.isPlaying) IncomingMessageSoundSource2.Play();
        else if (IncomingMessageSoundSource3 != null && IncomingMessageSoundSource3.clip != null && !IncomingMessageSoundSource3.isPlaying) IncomingMessageSoundSource3.Play();
        else if (IncomingMessageSoundSource4 != null && IncomingMessageSoundSource4.clip != null && !IncomingMessageSoundSource4.isPlaying) IncomingMessageSoundSource4.Play();
        else if (IncomingMessageSoundSource5 != null && IncomingMessageSoundSource5.clip != null && !IncomingMessageSoundSource5.isPlaying) IncomingMessageSoundSource5.Play();
        else if (IncomingMessageSoundSource6 != null && IncomingMessageSoundSource6.clip != null) IncomingMessageSoundSource6.Play();
    }

    /// <summary>
    /// Set the Pitch of the New Follower sound
    /// </summary>
    /// <param name="pitch"></param>
    public void SetNewFollowerPitch(float pitch)
    {
        NewFollowerSoundSource.pitch = pitch;
    }

    /// <summary>
    /// Set the AudioClip to be played when New Followers are received
    /// </summary>
    /// <param name="sound"></param>
    public void SetNewFollowerSound(AudioClip sound)
    {
        NewFollowerSoundSource.clip = sound;
    }

    /// <summary>
    /// Set the Volume of the New Follower sound
    /// </summary>
    /// <param name="volume"></param>
    public void SetNewFollowerVolume(float volume)
    {
        NewFollowerSoundSource.volume = volume;
    }

    public void PlayNewFollowerSound()
    {
        // Prevent the message sound from spamming too rapidly
        if (_newFollowerSoundStopwatch.IsRunning)
        {
            if (_newFollowerSoundStopwatch.ElapsedMilliseconds < 50) return;
            _newFollowerSoundStopwatch.Reset();
            _newFollowerSoundStopwatch.Start();
        }
        else
            _newFollowerSoundStopwatch.Start();
        
        if (NewFollowerSoundSource != null && NewFollowerSoundSource.clip != null) NewFollowerSoundSource.Play();
    }

    public void AddSystemNotice(string msgIn, TwitchIRC.NoticeColor colorEnum = TwitchIRC.NoticeColor.Blue)
    {
        OnChatMsg(TwitchIRC.ToNotice("System", msgIn, colorEnum));
    }

    private void AddMsg(string nickname, string color, string chat)
    {
        _userChat.Add(new TwitchChat(nickname, color, chat));

        while (_userChat.Count > 27)
            _userChat.RemoveAt(0);
        
        WordWrapText(_userChat);
    }

    private void WordWrapText(List<TwitchChat> messages)
    {
        var lines = new List<string>();
        TextMesh.text = "";
        var ren = TextMesh.GetComponent<Renderer>();
        var rowLimit = 0.975f; //find the sweet spot
        foreach (var m in messages)
        {
            TextMesh.text = string.Format("<color=#{0}FF>{1}</color>: ", m.Color, m.Name);
            var builder = "";
            var parts = m.Message.Split(' ');
            foreach (var t in parts)
            {
                builder = TextMesh.text;
                TextMesh.text += t + " ";
                if (ren.bounds.extents.x > rowLimit)
                {
                    lines.Add(builder.TrimEnd() + System.Environment.NewLine);
                    TextMesh.text = t + " ";
                }
                builder = TextMesh.text;
            }
            lines.Add(builder.TrimEnd() + System.Environment.NewLine);
        }
        
        TextMesh.text = lines.Aggregate("", (current, t) => current + t);
    }

    public static string FirstLetterToUpper(string str)
    {
        if (str == null)
            return null;

        if (str.Length > 1)
            return char.ToUpper(str[0]) + str.Substring(1);

        return str.ToUpper();
    }

    // Convert the first letter, and every first letter after an underscore to a Capital Letter
    // This looks a bit nicer before we have the proper format for this channel name
    public static string ChannelFirstLetterToUpper(string str)
    {
        if (str == null)
            return null;

        var endsWith_ = str.EndsWith("_");
        if (endsWith_) str = str.Substring(0, str.Length - 1);

        if (str.Length <= 1) return str.ToUpper();
        var pieces = str.Split('_');
        var st = "";
        for (var i = 0; i < pieces.Length; i++)
        {
            st += char.ToUpper(pieces[i][0]) + pieces[i].Substring(1);
            if (i < pieces.Length - 1)
                st += "_";
        }
        if (endsWith_) st += "_";
        return st;
    }

    internal static Texture2D GenerateBaseTexture()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(0.45f, 0.2f, 0.75f));
        tex.Apply();
        return tex;
    }

    // These are filled by JsonUtility so the compiler is confused
#pragma warning disable 649
    // ReSharper disable InconsistentNaming
    [Serializable]
    private class ChannelDataFull
    {
        public ChannelLinksData _links;
        public StreamData stream;
    }

    [Serializable]
    private class ChannelLinksData
    {
        public string channel;
        public string self;
    }

    [Serializable]
    private class StreamData
    {
        public string game;
        public uint viewers;
        public float average_fps;
        public uint delay;
        public uint video_height;
        public bool is_playlist;
        public string created_at;
        public uint _id;
        public StreamChannelData channel;
        public StreamPreviewData preview;
        public StreamLinksData _links;
    }
    
    [Serializable]
    private class StreamChannelData
    {
        public bool mature;
        public string status;
        public string broadcaster_language;
        public string display_name;
        public string game;
        public string delay;
        public string language;
        public uint _id;
        public string name;
        public string created_at;
        public string updated_at;
        public string logo;
        public string banner;
        public string video_banner;
        public string background;
        public string profile_banner;
        public string profile_banner_background_color;
        public bool partner;
        public string url;
        public uint views;
        public uint followers;
        public StreamChanneLinksData _links;
    }

    [Serializable]
    private class StreamChanneLinksData
    {
        public string self;
        public string follows;
        public string commercial;
        public string stream_key;
        public string chat;
        public string features;
        public string subscriptions;
        public string editors;
        public string teams;
        public string videos;
    }

    [Serializable]
    private class StreamPreviewData
    {
        public string small;
        public string medium;
        public string large;
        public string template;
    }

    [Serializable]
    private class StreamLinksData
    {
        public string self;
    }

    [Serializable]
    private class FollowsDataFull
    {
        public uint _total;
        public FollowsLinksData _links;
        public FollowsData[] follows;
    }

    [Serializable]
    private class FollowsLinksData
    {
        public string next;
        public string self;
    }

    [Serializable]
    private class FollowsData
    {
        public string created_at;
        public FollowerLinks _links;
        public bool notifications;
        public FollowerData user;
    }

    [Serializable]
    private class FollowerData
    {
        public FollowerLinks _links;
        public bool staff;
        public string logo;
        public string display_name;
        public string created_at;
        public string updated_at;
        public uint _id;
        public string name;
    }

    [Serializable]
    private class FollowerLinks
    {
        public string self;
    }

#pragma warning restore 649
    // ReSharper restore InconsistentNaming
}
