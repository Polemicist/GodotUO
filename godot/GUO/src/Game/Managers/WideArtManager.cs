// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using GUO.Assets;
using GUO.Game.GameObjects;
using GUO.Game.UI.Gumps;
using GUO.IO;
using GUO.Network;

namespace GUO.Game.Managers
{
    /// <summary>
    /// Item art past what a stock packet carries (data_formats section 53). World, container and
    /// equip packets hold a 16-bit art number and 0x78's worn items 15 bits. A shard using this
    /// extension keeps the stock packets and, right after one carrying such an item, sends
    /// 0xBF sub 0x7E55 with the item's real art. Only a client that said hello gets it, and only
    /// under the wide art profile does this client say hello or read the reply.
    /// <code>
    /// client to shard   0 HELLO (version byte)
    /// shard to client   1 ART (serial u32, art u32), 4 HELLO REQUEST
    /// </code>
    /// </summary>
    internal static class WideArtManager
    {
        public const ushort SUB_COMMAND = 0x7E55;
        public const byte VERSION = 1;

        private const byte OP_HELLO = 0;
        private const byte IN_ART = 1, IN_HELLO_REQUEST = 4;

        public static void SendHello()
        {
            if (!WideArtProfile.Enabled || NetClient.Socket == null || !NetClient.Socket.IsConnected)
            {
                return;
            }

            var w = new StackDataWriter(16);
            w.WriteUInt8(0xBF);
            w.WriteZero(2);
            w.WriteUInt16BE(SUB_COMMAND);
            w.WriteUInt8(OP_HELLO);
            w.WriteUInt8(VERSION);
            w.Seek(1, SeekOrigin.Begin);
            w.WriteUInt16BE((ushort)w.BytesWritten);
            NetClient.Socket.Send(w.BufferWritten);
            w.Dispose();
        }

        public static void OnPacket(World world, ref StackDataReader p)
        {
            byte op = p.ReadUInt8();

            if (op == IN_HELLO_REQUEST)
            {
                SendHello();
                return;
            }

            if (op != IN_ART)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            uint art = p.ReadUInt32BE();
            Item item = world.Items.Get(serial);

            // An id past this install's tiledata would index past it everywhere the item is drawn.
            if (item == null || item.Graphic == art || art >= Client.Game.UO.FileManager.TileData.StaticData.Length)
            {
                return;
            }

            item.Graphic = art;
            item.CheckGraphicChange();

            if (item.OnGround)
            {
                item.AddToTile(); // height and draw order come from the new art's tiledata
            }
            else if (SerialHelper.IsMobile(item.Container))
            {
                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }
            else if (SerialHelper.IsItem(item.Container))
            {
                UIManager.GetGump<ContainerGump>(item.Container)?.RequestUpdateContents();

                Item box = world.Items.Get(item.Container);

                if (box != null)
                {
                    UIManager.GetTradingGump(box.Serial)?.RequestUpdateContents();
                }
            }
        }
    }
}
