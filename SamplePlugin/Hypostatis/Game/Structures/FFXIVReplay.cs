using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hypostasis.Game.Structures;

// .dat Info
// 0x36C is the start of the replay data, everything before this is the Header + ChapterArray
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct FFXIVReplay
{
    public const short CurrentReplayFormatVersion = 5;

    [StructLayout(LayoutKind.Explicit, Size = 0x68)]
    public struct Header
    {
        private static readonly byte[] validBytes = "FFXIVREPLAY"u8.ToArray();

        [FieldOffset(0x0)] public fixed byte FFXIVREPLAY[12]; // FFXIVREPLAY
        [FieldOffset(0xC)] public short replayFormatVersion;
        [FieldOffset(0xE)] public short operatingSystemType; // 3 = Windows, 5 = Mac
        [FieldOffset(0x10)] public int gameBuildNumber; // Has to match
        [FieldOffset(0x14)] public uint timestamp; // Unix timestamp
        [FieldOffset(0x18)] public uint totalMS; // MS including time before the first chapter
        [FieldOffset(0x1C)] public uint displayedMS; // MS excluding time before the first chapter
        [FieldOffset(0x20)] public ushort contentID;
        [FieldOffset(0x28)] public byte info; // Bitfield, 1 = Up to date, 2 = Locked, 4 = Duty completed
        [FieldOffset(0x30)] public ulong localCID; // ID of the recorder (Has to match the logged in character)
        [FieldOffset(0x38)] public fixed byte jobs[8]; // Job ID of each player
        [FieldOffset(0x40)] public byte playerIndex; // The index of the recorder in the jobs array
        [FieldOffset(0x44)] public int u0x44; // Always 772? Seems to be unused
        [FieldOffset(0x48)] public int replayLength; // Number of bytes in the replay
        [FieldOffset(0x4C)] public short u0x4C; // Padding? Seems to be unused
        [FieldOffset(0x4E)] public fixed ushort npcNames[7]; // Determines displayed names using the BNpcName sheet
        [FieldOffset(0x5C)] public int u0x5C; // Padding? Seems to be unused
        [FieldOffset(0x60)] public long u0x60; // New in DT

        public bool IsValid
        {
            get
            {
                for (int i = 0; i < validBytes.Length; i++)
                {
                    if (validBytes[i] != FFXIVREPLAY[i])
                        return false;
                }
                return true;
            }
        }

        public bool IsPlayable => gameBuildNumber == Common.ContentsReplayModule->gameBuildNumber && IsCurrentFormatVersion;

        public bool IsCurrentFormatVersion => replayFormatVersion == CurrentReplayFormatVersion;

        public bool IsLocked => IsValid && IsPlayable && (info & 2) != 0;

        public Lumina.Excel.Sheets.ContentFinderCondition ContentFinderCondition => DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>().GetRow(contentID);

        public Lumina.Excel.Sheets.ClassJob LocalPlayerClassJob => DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().GetRow(jobs[playerIndex]);

        private byte GetJobSafe(int i) => jobs[i];

        public IEnumerable<Lumina.Excel.Sheets.ClassJob> ClassJobs => Enumerable.Range(0, 8)
            .Select(GetJobSafe).TakeWhile(id => id != 0)
            .Select(id => DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().GetRow(id));
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x4 + 0xC * 64)]
    public struct ChapterArray
    {
        [FieldOffset(0x0)] public int length;

        [StructLayout(LayoutKind.Sequential, Size = 0xC)]
        public struct Chapter
        {
            public int type; // 1 = Countdown, 2 = Start/Restart, 3 = ??? (displayed as Countdown), 4 = Event Cutscene, 5 = Barrier down (displayed as Start/Restart)
            public uint offset; // byte offset
            public uint ms; // ms from the start of the instance
        }

        public Chapter* this[int i]
        {
            get
            {
                if (i is < 0 or > 63)
                    return null;

                fixed (void* ptr = &this)
                    return (Chapter*)((nint)ptr + 4) + i;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DataSegment
    {
        public ushort opcode;
        public ushort dataLength;
        public uint ms;
        public uint objectID;

        public uint Length => (uint)sizeof(DataSegment) + dataLength;

        public byte* Data
        {
            get
            {
                fixed (void* ptr = &this)
                    return (byte*)ptr + sizeof(DataSegment);
            }
        }
    }

    public Header header;
    public ChapterArray chapters;

    public byte* Data
    {
        get
        {
            fixed (void* ptr = &this)
                return (byte*)ptr + sizeof(Header) + sizeof(ChapterArray);
        }
    }

    public DataSegment* GetDataSegment(uint offset) => offset < header.replayLength ? (DataSegment*)(Data + offset) : null;
}