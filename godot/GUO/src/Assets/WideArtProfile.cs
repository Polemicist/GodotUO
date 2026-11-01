// SPDX-License-Identifier: BSD-2-Clause
using System;

namespace GUO.Assets
{
    /// <summary>
    /// Opt-in wide static art (data_formats section 53): static art ids above 0xFFFF, as a
    /// shard whose install carries more than 0x10000 static slots defines them. Off unless
    /// <c>GUO_WIDE_ART</c> names a supported profile, so a stock install reads exactly as before.
    /// </summary>
    public static class WideArtProfile
    {
        /// <summary>The one supported profile: 9-byte wide statics files that begin with <see cref="StaticsMagic"/>.</summary>
        public const string WideStatics1 = "widestatics1";

        public const int LegacyStaticCapacity = 0x10000;

        /// <summary>ADR-0047's bound: capacity is a multiple of 32 and at most 0x40000 slots.</summary>
        public const int MaxStaticCapacity = 0x40000;

        /// <summary>Wide statics record: uint32 art, byte X, byte Y, sbyte Z, uint16 hue (ADR-0047 Static9).</summary>
        public const int StaticRecordSize = 9;

        public const int LegacyStaticRecordSize = 7;

        public static ReadOnlySpan<byte> StaticsMagic => "SOSWIDE1"u8;

        public static bool Enabled { get; } = Parse(Environment.GetEnvironmentVariable("GUO_WIDE_ART"));

        internal static bool Parse(string value) =>
            string.Equals(value?.Trim(), WideStatics1, StringComparison.OrdinalIgnoreCase);

        /// <summary>Static slots the art index holds: legacy 0x10000 when off, else the index's own count.</summary>
        public static int StaticCapacity(int artIndexEntries, int landCount) =>
            Enabled ? CapacityOf(artIndexEntries - landCount) : LegacyStaticCapacity;

        internal static int CapacityOf(int staticSlots) =>
            Math.Clamp(staticSlots & ~31, LegacyStaticCapacity, MaxStaticCapacity);

        /// <summary>True when a statics file starts with the wide magic.</summary>
        public static bool IsWideStatics(ReadOnlySpan<byte> head) =>
            head.Length >= StaticsMagic.Length && head[..StaticsMagic.Length].SequenceEqual(StaticsMagic);
    }
}
