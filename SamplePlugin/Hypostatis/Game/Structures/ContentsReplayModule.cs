using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit, Size = 0x730), GameStructure("48 89 5C 24 ?? 57 48 83 EC 20 33 FF 48 8B D9 89 39 48 89 79 08")]
public unsafe partial struct ContentsReplayModule : IHypostasisStructure
{
    [StructLayout(LayoutKind.Explicit, Size = 0x70)]
    public struct InitZonePacket
    {
        [FieldOffset(0x0)] public ushort u0x0;
        [FieldOffset(0x2)] public ushort territoryType; // Used to determine if you can play a recording (possibly as well as if it can be recorded?)
        [FieldOffset(0x4)] public ushort u0x4;
        [FieldOffset(0x6)] public ushort contentFinderCondition; // Stops recording if 0
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xC0)]
    public struct UnknownPacket;

    [FieldOffset(0x0)] public int gameBuildNumber;
    [FieldOffset(0x8)] public nint fileStream; // Start of save/read area
    [FieldOffset(0x10)] public nint fileStreamNextWrite; // Next area to be written to while recording
    [FieldOffset(0x18)] public nint fileStreamEnd; // End of save/read area
    [FieldOffset(0x20)] public long u0x20;
    [FieldOffset(0x28)] public long u0x28;
    [FieldOffset(0x30)] public long dataOffset; // Next? offset of bytes to read from the save/read area (up to 1MB)
    [FieldOffset(0x38)] public long overallDataOffset; // Overall (next?) offset of bytes to read
    [FieldOffset(0x40)] public long lastDataOffset; // Last? offset read
    [FieldOffset(0x48)] public FFXIVReplay.Header replayHeader;
    [FieldOffset(0xB0)] public FFXIVReplay.ChapterArray chapters; // ms of the first chapter determines the displayed time, but doesn't affect chapter times
    [FieldOffset(0x3B8)] public Utf8String contentTitle; // Current content name
    [FieldOffset(0x420)] public long nextDataSection; // 0x100000 if the lower half of the save/read area is next to be loaded into, 0x80000 if the upper half is
    [FieldOffset(0x428)] public long numberBytesRead; // How many bytes have been read from the file
    [FieldOffset(0x430)] public int currentFileSection; // Currently playing section starting at 1 (each section is 512 kb)
    [FieldOffset(0x434)] public int dataLoadType; // 5 = save replay?, 6 = save replay header + chapters?, 7 = Load header + chapters, 8 = Load section, 10 = Load header (3-6 and 11 are used for saving?)
    [FieldOffset(0x438)] public long dataLoadOffset; // Starting offset to load the next section into
    [FieldOffset(0x440)] public long dataLoadLength; // 0x100000 or replayLength if initially loading, 0x80000 afterwards
    [FieldOffset(0x448)] public long dataLoadFileOffset; // Offset to begin loading data from
    [FieldOffset(0x450)] public long localCID;
    [FieldOffset(0x458)] public byte currentReplaySlot; // 0-2 depending on which replay it is, 255 otherwise
    // 0x459-0x460 Padding?
    [FieldOffset(0x460)] public Utf8String characterRecordingName; // "<Character name> Duty Record #<Slot>" but only when recording, not when replaying
    [FieldOffset(0x4C8)] public Utf8String replayTitle; // contentTitle + the date and time, but only when recording, not when replaying
    [FieldOffset(0x530)] public Utf8String u0x530;
    [FieldOffset(0x598)] public float recordingTime; // Only used when recording
    [FieldOffset(0x5A0)] public long recordingLength; // Only used when recording
    [FieldOffset(0x5A8)] public int u0x5A8;
    [FieldOffset(0x5AC)] public byte u0x5AC;
    [FieldOffset(0x5AD)] public byte nextReplaySaveSlot;
    [FieldOffset(0x5B0)] public FFXIVReplay.Header* savedReplayHeaders; // Pointer to the three saved replay headers
    [FieldOffset(0x5B8)] public nint u0x5B8; // Pointer right after the file headers
    [FieldOffset(0x5C0)] public nint u0x5C0; // Same as above?
    [FieldOffset(0x5C8)] public byte u0x5C8;
    [FieldOffset(0x5CC)] public uint localPlayerObjectID;
    [FieldOffset(0x5D0)] public InitZonePacket initZonePacket; // The last received InitZone is saved here
    [FieldOffset(0x640)] public long u0x640;
    [FieldOffset(0x648)] public UnknownPacket u0x648; // Probably a packet
    [FieldOffset(0x708)] public int u0x708;
    [FieldOffset(0x70C)] public float seek; // Determines current time, but always seems to be slightly ahead
    [FieldOffset(0x710)] public float seekDelta; // Stores how far the seek moves per second
    [FieldOffset(0x714)] public float speed;
    [FieldOffset(0x718)] public float u0x718; // Seems to be 1 or 0, depending on if the speed is greater than 1 (Probably sound timescale)
    [FieldOffset(0x71C)] public byte selectedChapter; // 64 when playing, otherwise determines the current chapter being seeked to
    [FieldOffset(0x720)] public uint startingMS; // The ms considered 00:00:00, is NOT set if seek would be below the value (as in currently replaying the zone in packets)
    [FieldOffset(0x724)] public int u0x724;
    [FieldOffset(0x728)] public short u0x728;
    [FieldOffset(0x72A)] public byte status; // Bitfield determining the current status of the system (1 Just logged in?, 2 Can record, 4 Saving packets, 8 ???, 16 Record Ready Checked?, 32 Save recording?, 64 Barrier down, 128 In playback after barrier drops?)
    [FieldOffset(0x72B)] public byte playbackControls; // Bitfield determining the current playback controls (1 Waiting to enter playback, 2 Waiting to leave playback, 4 In playback (blocks packets), 8 Paused, 16 Chapter???, 32 Chapter???, 64 In duty?, 128 In playback???)
    [FieldOffset(0x72C)] public byte u0x72C; // Bitfield? (1 Used to apply the initial chapter the moment the barrier drops while recording)
    // 0x72D-0x730 is padding

    public bool InPlayback => (playbackControls & 4) != 0;
    public bool IsPaused => (playbackControls & 8) != 0;
    public bool IsSavingPackets => (status & 4) != 0;
    public bool IsRecording => (status & 0x74) == 0x74;
    public bool IsLoadingChapter => selectedChapter < 0x40;

    // E8 ?? ?? ?? ?? 48 8D 8B 48 0B 00 00 E8 ?? ?? ?? ?? 48 8D 8B 38 0B 00 00 dtor
    // 40 53 48 83 EC 20 80 A1 ?? ?? ?? ?? F3 Initialize
    // 40 53 48 83 EC 20 0F B6 81 ?? ?? ?? ?? 48 8B D9 A8 04 75 09 Update
    // 48 83 EC 38 0F B6 91 ?? ?? ?? ?? 0F B6 C2 RequestEndPlayback
    // E8 ?? ?? ?? ?? EB 10 41 83 78 04 00 EndPlayback
    // 48 89 5C 24 10 55 48 8B EC 48 81 EC 80 00 00 00 48 8B 05 Something to do with loading
    // E8 ?? ?? ?? ?? 3C 40 73 4A GetCurrentChapter
    // E9 ?? ?? ?? ?? 48 83 4B 70 04 <ContentsReplayModule*, byte, byte> addRecordingChapter
    // 40 53 48 83 EC 20 0F B6 81 ?? ?? ?? ?? 48 8B D9 24 06 3C 04 75 5D 83 B9 <ContentsReplayModule*, void> resetPlayback
    // F6 81 ?? ?? ?? ?? 04 74 11 SetTimescale (No longer used by anything)
    // 40 53 48 83 EC 20 F3 0F 10 81 ?? ?? ?? ?? 48 8B D9 F3 0F 10 0D SetSoundTimescale1? Doesn't seem to work (Last function)
    // E8 ?? ?? ?? ?? 44 0F B6 D8 C7 03 02 00 00 00 Function handling the UI buttons

    public delegate void BeginRecordingDelegate(ContentsReplayModule* contentsReplayModule, Bool saveRecording);
    public static readonly GameFunction<BeginRecordingDelegate> beginRecording = new("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 83 C4 20 41 5C");
    public void BeginRecording(bool saveRecording = true)
    {
        fixed (ContentsReplayModule* ptr = &this)
            beginRecording.Invoke(ptr, saveRecording);
    }

    public delegate void EndRecordingDelegate(ContentsReplayModule* contentsReplayModule);
    public static readonly GameFunction<EndRecordingDelegate> endRecording = new("E8 ?? ?? ?? ?? 32 C0 EB B2");
    public void EndRecording()
    {
        fixed (ContentsReplayModule* ptr = &this)
            endRecording.Invoke(ptr);
    }

    public delegate void OnZoneInPacketDelegate(ContentsReplayModule* contentsReplayModule, uint objectID, nint packet);
    public static readonly GameFunction<OnZoneInPacketDelegate> onZoneInPacket = new("48 89 5C 24 08 48 89 74 24 18 57 48 83 EC 20 49 8B F8");

    public delegate Bool OnLoginDelegate(ContentsReplayModule* contentsReplayModule);
    public static readonly GameFunction<OnLoginDelegate> onLogin = new("40 53 48 83 EC 20 F6 81 ?? ?? ?? ?? ?? 48 8B D9 0F 85 ?? ?? ?? ?? F6 81 ?? ?? ?? ?? ??"); // Inlined, hook onZoneInPacket instead
    public bool OnLogin()
    {
        fixed (ContentsReplayModule* ptr = &this)
            return onLogin.Invoke(ptr);
    }

    public delegate void InitializeRecordingDelegate(ContentsReplayModule* contentsReplayModule);
    public static readonly GameFunction<InitializeRecordingDelegate> initializeRecording = new("40 55 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 27 F6 81");
    public void InitializeRecording()
    {
        fixed (ContentsReplayModule* ptr = &this)
            initializeRecording.Invoke(ptr);
    }

    public delegate Bool RequestPlaybackDelegate(ContentsReplayModule* contentsReplayModule, byte slot);
    public static readonly GameFunction<RequestPlaybackDelegate> requestPlayback = new("48 89 5C 24 08 57 48 83 EC 30 F6 81 ?? ?? ?? ?? 04"); // E8 ?? ?? ?? ?? EB 2B 48 8B CB 89 53 2C (+0x14)
    public bool RequestPlayback(byte slot = 0)
    {
        fixed (ContentsReplayModule* ptr = &this)
            return requestPlayback.Invoke(ptr, slot);
    }

    public delegate void ReceiveActorControlPacketDelegate(ContentsReplayModule* contentsReplayModule, uint objectID, nint packet);
    public static readonly GameFunction<ReceiveActorControlPacketDelegate> receiveActorControlPacket = new("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 30 33 FF 48 8B D9");

    public delegate void BeginPlaybackDelegate(ContentsReplayModule* contentsReplayModule, Bool allowed);
    public static readonly GameFunction<BeginPlaybackDelegate> beginPlayback = new("40 53 48 83 EC 30 0F B6 81 ?? ?? ?? ?? 48 8B D9 A8 01 0F 84 ?? ?? ?? ?? 24 FE"); // Inlined, use receiveActorControlPacket and check *(ushort*)packet == 931 to hook instead
    public void BeginPlayback(bool allowed = true)
    {
        fixed (ContentsReplayModule* ptr = &this)
            beginPlayback.Invoke(ptr, allowed);
    }

    public static readonly GameFunction<InitializeRecordingDelegate> playbackUpdate = new("E8 ?? ?? ?? ?? F6 83 ?? ?? ?? ?? 04 74 38 F6 83 ?? ?? ?? ?? 01");
    public void PlaybackUpdate()
    {
        fixed (ContentsReplayModule* ptr = &this)
            playbackUpdate.Invoke(ptr);
    }

    public delegate FFXIVReplay.DataSegment* GetReplayDataSegmentDelegate(ContentsReplayModule* contentsReplayModule);
    /*public static readonly GameFunction<GetReplayDataSegmentDelegate> getReplayDataSegment = new(""); // No longer exists, inlined at 48 39 43 38 0F 83 ?? ?? ?? ?? 48 8B 4B 30
    public FFXIVReplay.DataSegment* GetReplayDataSegment()
    {
        fixed (ContentsReplayModule* ptr = &this)
            return getReplayDataSegment.Invoke(ptr);
    }*/

    public delegate Bool SetChapterDelegate(ContentsReplayModule* contentsReplayModule, byte chapter);
    public static readonly GameFunction<SetChapterDelegate> setChapter = new("E8 ?? ?? ?? ?? 84 C0 E9 ?? ?? ?? ?? 48 8D 4F 10");
    public bool SetChapter(byte chapter)
    {
        fixed (ContentsReplayModule* ptr = &this)
            return setChapter.Invoke(ptr, chapter);
    }

    public delegate void OnSetChapterDelegate(ContentsReplayModule* contentsReplayModule, byte chapter);
    public static readonly GameFunction<OnSetChapterDelegate> onSetChapter = new("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 30 48 8B F1 0F B6 EA");
    public void OnSetChapter(byte chapter)
    {
        fixed (ContentsReplayModule* ptr = &this)
            onSetChapter.Invoke(ptr, chapter);
    }

    public delegate Bool WritePacketDelegate(ContentsReplayModule* contentsReplayModule, uint objectID, ushort opcode, byte* data, ulong length);
    public static readonly GameFunction<WritePacketDelegate> writePacket = new("E8 ?? ?? ?? ?? 84 C0 74 60 33 C0");
    public bool WritePacket(uint objectID, ushort opcode, byte* data, ulong length)
    {
        fixed (ContentsReplayModule* ptr = &this)
            return writePacket.Invoke(ptr, objectID, opcode, data, length);
    }

    public bool WritePacket(uint objectID, ushort opcode, byte[] data)
    {
        fixed (ContentsReplayModule* ptr = &this)
        fixed (byte* dataPtr = data)
            return writePacket.Invoke(ptr, objectID, opcode, dataPtr, (ulong)data.Length);
    }

    public delegate Bool ReplayPacketDelegate(ContentsReplayModule* contentsReplayModule, FFXIVReplay.DataSegment* segment, byte* data);
    public static readonly GameFunction<ReplayPacketDelegate> replayPacket = new("E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 77 9A");
    public bool ReplayPacket(FFXIVReplay.DataSegment* segment)
    {
        fixed (ContentsReplayModule* ptr = &this)
            return replayPacket.Invoke(ptr, segment, segment->Data);
    }

    public bool ReplayPacket(FFXIVReplay.DataSegment segment, byte[] data)
    {
        fixed (ContentsReplayModule* ptr = &this)
        fixed (byte* dataPtr = data)
            return replayPacket.Invoke(ptr, &segment, dataPtr);
    }

    public bool Validate() => true;
}