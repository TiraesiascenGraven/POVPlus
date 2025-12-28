using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;
using FFXIVClientStructs.Havok.Animation.Rig;
using System.Collections.Generic;
using System.Linq;

namespace SamplePlugin
{
    public unsafe class PartialSkeletonn(Skeleton skeleton, int id)
    {
        public int Id { get; } = id;

        public Skeleton Skeleton { get; } = skeleton;

        public List<nint> Poses { get; set; } = [];

        private readonly Dictionary<int, Bone> parambones = [];

        public List<Bone> RootBones { get; set; } = [];



        public Bone? GetBone(int index)
        {
            if (parambones.TryGetValue(index, out var bone))
                return bone;

            return null;
        }

    }
}
