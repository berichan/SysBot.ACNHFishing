using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.ACNHFishing
{
    public class Vector3
    {
        public static Vector3 Zero => new(0, 0, 0);

        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }

        public bool ValuesEqual(Vector3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }
    }

    public enum OverworldState
    {
        Null,
        Overworld,
        Loading,
        UserArriveLeaving,
        Unknown
    }

    public class DodoPositionHelper
    {

        private readonly ISwitchConnectionAsync Connection;
        private readonly CrossBot BotRunner;
        private readonly CrossBotConfig Config;

        public string DodoCode { get; set; } = "No code set yet."; 

        public DodoPositionHelper(CrossBot bot)
        {
            BotRunner = bot;
            Connection = BotRunner.SwitchConnection;
            Config = BotRunner.Config;
        }

        public async Task<ulong> FollowMainPointer(long[] jumps, CancellationToken token) //include the last jump here
        {
            var jumpsWithoutLast = jumps.Take(jumps.Length - 1);

            byte[] command = Encoding.UTF8.GetBytes($"pointer{string.Concat(jumpsWithoutLast.Select(z => $" {z}"))}\r\n");

            byte[] socketReturn = await Connection.ReadRaw(command, sizeof(ulong) * 2 + 1, token).ConfigureAwait(false);
            var bytes = Base.Decoder.ConvertHexByteStringToBytes(socketReturn);
            bytes = bytes.Reverse().ToArray();

            var offset = (ulong)((long)BitConverter.ToUInt64(bytes, 0) + jumps[jumps.Length - 1]);
            return offset;
        }

        public async Task<OverworldState> GetOverworldState(long[] jumps, CancellationToken token)
        {
            ulong coord = await FollowMainPointer(jumps, token).ConfigureAwait(false);
            return await GetOverworldState(coord, token).ConfigureAwait(false);
        }

        public async Task<OverworldState> GetOverworldState(ulong CoordinateAddress, CancellationToken token)
        {
            var x = BitConverter.ToUInt32(await Connection.ReadBytesAbsoluteAsync(CoordinateAddress + 0x20, 0x4, token).ConfigureAwait(false), 0);
            //LogUtil.LogInfo($"CurrentVal: {x:X8}", Config.IP);
            return GetOverworldState(x);
        }

        public static OverworldState GetOverworldState(uint val) => val switch
        {
            0x00000000 => OverworldState.Null,
            0xC0066666 => OverworldState.Overworld,
            0xBE200000 => OverworldState.UserArriveLeaving,
            _ when (val & 0xFFFF) == 0xC906 => OverworldState.Loading,
            _ => OverworldState.Unknown,
        };

        /// <summary>
        /// Get position and single-axis rotation
        /// </summary>
        /// <param name="jumps"></param>
        /// <param name="token"></param>
        /// <returns>Position and up-vector normalized rotation</returns>
        public async Task<(Vector3, float)> GetPosRot(long[] jumps, CancellationToken token) 
        {
            ulong coord = await FollowMainPointer(jumps, token).ConfigureAwait(false);
            var bytes = await Connection.ReadBytesAbsoluteAsync(coord, 0x40, token).ConfigureAwait(false);
            float x = BitConverter.ToSingle(bytes, 0);
            float y = BitConverter.ToSingle(bytes, 8);
            float z = BitConverter.ToSingle(bytes, 4);
            float rot = BitConverter.ToSingle(bytes, 0x3C);

            return (new Vector3(x, y, z), rot);
        }

        public async Task SetPosRot(long[] jumps, Vector3 pos, float? rot, CancellationToken token)
        {
            ulong coord = await FollowMainPointer(jumps, token).ConfigureAwait(false);
            var bytes = await Connection.ReadBytesAbsoluteAsync(coord, 0x40, token).ConfigureAwait(false);
            bytes.Set(0, BitConverter.GetBytes(pos.X));
            bytes.Set(8, BitConverter.GetBytes(pos.Y));
            bytes.Set(4, BitConverter.GetBytes(pos.Z));

            if (rot != null)
                bytes.Set(0x3C, BitConverter.GetBytes(rot.Value));

            await Connection.WriteBytesAbsoluteAsync(bytes, coord, token).ConfigureAwait(false);
        }
    }
}
