using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHSE.Core;
using ACNHMobileSpawner;
using SysBot.Base;
using System.Text;
using System.IO;
using System.Collections.Generic;
using static SysBot.Base.SwitchButton;
using System.Diagnostics;

namespace SysBot.ACNHFishing
{
    public sealed class CrossBot : SwitchRoutineExecutor<CrossBotConfig>
    {
        private uint InventoryOffset { get; set; } = (uint)OffsetHelper.InventoryOffset;

        public readonly PocketInjectorAsync PocketInjector;
        public readonly DodoPositionHelper DodoPosition;

        public readonly ISwitchConnectionAsync SwitchConnection;

        public TimeBlock LastTimeState { get; private set; } = new();
        public bool Paused { private get; set; } = false;
        public ulong ChatAddress { get; set; } = 0;
        public DateTime LastTimeWarpTime { get; private set; } = DateTime.Now;

        private Vector3 ExpectedPosition = Vector3.Zero;
        private float ExpectedRotation = 0f;

        public CrossBot(CrossBotConfig cfg) : base(cfg)
        {
            if (Connection is ISwitchConnectionAsync con)
                SwitchConnection = con;
            else
                throw new Exception("Connection is null.");

            if (Connection is SwitchSocketAsync ssa)
                ssa.MaximumTransferSize = cfg.MapPullChunkSize;

            DodoPosition = new DodoPositionHelper(this);
            PocketInjector = new PocketInjectorAsync(SwitchConnection, InventoryOffset);
        }

        public override async Task MainLoop(CancellationToken token)
        {

            // Disconnect our virtual controller; will reconnect once we send a button command after a request.
            LogUtil.LogInfo("Detaching controller on startup as first interaction.", Config.IP);
            await Connection.SendAsync(SwitchCommand.DetachController(), token).ConfigureAwait(false);
            await Task.Delay(200, token).ConfigureAwait(false);

            // get version
            await Task.Delay(0_100, token).ConfigureAwait(false);
            LogUtil.LogInfo("Attempting get version. Please wait...", Config.IP);
            string version = await SwitchConnection.GetVersionAsync(token).ConfigureAwait(false);
            LogUtil.LogInfo($"sys-botbase version identified as: {version}", Config.IP);

            // Get inventory offset
            InventoryOffset = await this.GetCurrentPlayerOffset((uint)OffsetHelper.InventoryOffset, (uint)OffsetHelper.PlayerSize, token).ConfigureAwait(false);
            PocketInjector.WriteOffset = InventoryOffset;

            // Validate inventory offset.
            LogUtil.LogInfo("Checking inventory offset for validity.", Config.IP);
            var valid = await GetIsPlayerInventoryValid(InventoryOffset, token).ConfigureAwait(false);
            if (!valid)
            {
                LogUtil.LogInfo($"Inventory read from {InventoryOffset} (0x{InventoryOffset:X8}) does not appear to be valid.", Config.IP);
                if (Config.RequireValidInventoryMetadata)
                {
                    LogUtil.LogInfo("Exiting!", Config.IP);
                    return;
                }
            }

            // pull in-game time and store it
            var timeBytes = await Connection.ReadBytesAsync((uint)OffsetHelper.TimeAddress, TimeBlock.SIZE, token).ConfigureAwait(false);
            LastTimeState = timeBytes.ToClass<TimeBlock>();
            LogUtil.LogInfo("Started at in-game time: " + LastTimeState.ToString(), Config.IP);

            // pull player posrot and store it
            (ExpectedPosition, ExpectedRotation) = await DodoPosition.GetPosRot(OffsetHelper.PlayerCoordJumps, token).ConfigureAwait(false);

            // inject + hold rod (first slot) and move cursor to second slot
            await Click(B, 0_400, token).ConfigureAwait(false);
            await Click(DDOWN, 1_500, token).ConfigureAwait(false);
            await PocketInjector.Write40(new Item(2377), token).ConfigureAwait(false);
            await Click(X, 0_800, token).ConfigureAwait(false);
            await Click(A, 0_800, token).ConfigureAwait(false);
            await Click(A, 1_800, token).ConfigureAwait(false);

            // move cursor to second slot
            await Click(X, 0_800, token).ConfigureAwait(false);
            await Click(DRIGHT, 0_800, token).ConfigureAwait(false);
            await Click(B, 1_500, token).ConfigureAwait(false);

            while (!token.IsCancellationRequested)
                await FishLoop(token).ConfigureAwait(false);
        }

        private async Task FishLoop(CancellationToken token)
        {
            // Reel in line if we need to
            if (await IsFishing(token).ConfigureAwait(false))
                await Click(A, 1_500, token).ConfigureAwait(false);

            // Get out of any menus
            await ClickConversation(B, 1_000, token).ConfigureAwait(false);

            bool wasInDialogue = false;
            while (await DodoPosition.GetOverworldState(OffsetHelper.PlayerCoordJumps, token).ConfigureAwait(false) is not OverworldState.Overworld)
            {
                wasInDialogue = true;
                await ClickConversation(B, 1_000, token).ConfigureAwait(false); // Get out of any conversations
            }

            if (wasInDialogue)
                await Task.Delay(2_000, token).ConfigureAwait(false);

            // Ensure we're at our expected position
            (var currentPosition, var currentRotation) = await DodoPosition.GetPosRot(OffsetHelper.PlayerCoordJumps, token).ConfigureAwait(false);
            if (currentPosition.ValuesEqual(ExpectedPosition) || currentRotation != ExpectedRotation)
                await DodoPosition.SetPosRot(OffsetHelper.PlayerCoordJumps, ExpectedPosition, ExpectedRotation, token).ConfigureAwait(false);

            if (wasInDialogue)
                await Task.Delay(2_000, token).ConfigureAwait(false);

            // Inject bait + reset rod uses, then throw bait
            await PocketInjector.Write1Plus39(new Item(2377), new Item(4549), token).ConfigureAwait(false);
            await Click(X, 0_400, token).ConfigureAwait(false);
            for (int i = 0; i < 3; ++i)
                await Click(A, 0_400, token).ConfigureAwait(false);

            // Wait for fishy bait animation
            await Task.Delay(2_500, token).ConfigureAwait(false);

            // Throw line
            await Click(A, 0_400, token).ConfigureAwait(false);

            var caughtSomething = false;
            var sw = new Stopwatch();
            sw.Start();
            while (!caughtSomething && sw.ElapsedMilliseconds < 20_000)
            {
                caughtSomething = await IsBiting(token).ConfigureAwait(false);
                await Task.Delay(0_050, token).ConfigureAwait(false);
            }
            sw.Stop();

            if (!caughtSomething)
                return; // try again

            // We have something! reel it in
            LogUtil.LogInfo("Caught something! Trying to reel it in.", Config.IP);
            for (int i = 0; i < 3; ++i)
                await Click(A, 0_050, token).ConfigureAwait(false);

            await Task.Delay(2_000, token).ConfigureAwait(false);

            // Go through dialogue
            for (int i = 0; i < 8; ++i)
                await ClickConversation(B, 0_800, token).ConfigureAwait(false);

            // Log whatever we've caught
            (var result, var inventoryData) = await PocketInjector.Read(token).ConfigureAwait(false);
            var fishCaught = inventoryData.Where(x => !x.IsNone && x.ItemId != 2377 && x.ItemId != 4549).FirstOrDefault();
            if (fishCaught != null) // We can fail to catch the fish
            {
                var itemName = GameInfo.Strings.GetItemName(fishCaught);
                LogUtil.LogInfo($"I caught a {itemName}!", Config.IP);
            }
            else
                LogUtil.LogInfo("I was too slow...", Config.IP);
        }

        private async Task RestartGame(CancellationToken token)
        {
            // Close game
            await Click(B, 0_500, token).ConfigureAwait(false);
            await Task.Delay(0_500, token).ConfigureAwait(false);
            await Click(HOME, 0_800, token).ConfigureAwait(false);
            await Task.Delay(0_300, token).ConfigureAwait(false);

            await Click(X, 0_500, token).ConfigureAwait(false);
            await Click(A, 0_500, token).ConfigureAwait(false);

            // Wait for "closing software" wheel
            await Task.Delay(3_500 + Config.RestartGameWait, token).ConfigureAwait(false);

            await Click(A, 1_000 + Config.RestartGameWait, token).ConfigureAwait(false);

            // Click away from any system updates if requested
            if (Config.AvoidSystemUpdate)
                await Click(DUP, 0_600, token).ConfigureAwait(false);

            // Start game
            for (int i = 0; i < 2; ++i)
                await Click(A, 1_000 + Config.RestartGameWait, token).ConfigureAwait(false);

            // Wait for "checking if the game can be played" wheel
            await Task.Delay(5_000 + Config.RestartGameWait, token).ConfigureAwait(false);

            for (int i = 0; i < 3; ++i)
                await Click(A, 1_000, token).ConfigureAwait(false);
        }

        private async Task EndSession(CancellationToken token)
        {
            for (int i = 0; i < 5; ++i)
                await Click(B, 0_300, token).ConfigureAwait(false);

            await Task.Delay(0_500, token).ConfigureAwait(false);
            await Click(MINUS, 0_500, token).ConfigureAwait(false);

            // End session or close gate or close game
            for (int i = 0; i < 5; ++i)
                await Click(A, 1_000, token).ConfigureAwait(false);

            await Task.Delay(14_000, token).ConfigureAwait(false);
        }

        private async Task<bool> IsNetworkSessionActive(CancellationToken token) => (await Connection.ReadBytesAsync((uint)OffsetHelper.OnlineSessionAddress, 0x1, token).ConfigureAwait(false))[0] == 1;

        private async Task Speak(string toSpeak, CancellationToken token)
        {
            // get chat addr
            ChatAddress = await DodoPosition.FollowMainPointer(OffsetHelper.ChatCoordJumps, token).ConfigureAwait(false);
            await Task.Delay(0_200, token).ConfigureAwait(false);

            await Click(R, 0_500, token).ConfigureAwait(false);
            await Click(A, 0_400, token).ConfigureAwait(false);
            await Click(A, 0_400, token).ConfigureAwait(false);

            // Inject text as utf-16, and null the rest
            var chatBytes = Encoding.Unicode.GetBytes(toSpeak);
            var sendBytes = new byte[OffsetHelper.ChatBufferSize * 2];
            Array.Copy(chatBytes, sendBytes, chatBytes.Length);
            await SwitchConnection.WriteBytesAbsoluteAsync(sendBytes, ChatAddress, token).ConfigureAwait(false);

            await Click(PLUS, 0_200, token).ConfigureAwait(false);

            // Exit out of any menus (fail-safe)
            for (int i = 0; i < 2; i++)
                await Click(B, 0_400, token).ConfigureAwait(false);
        }

        private async Task<bool> IsFishing(CancellationToken token) => (await Connection.ReadBytesAsync((uint)OffsetHelper.IsFishingOffset, 0x1, token).ConfigureAwait(false))[0] == 1;
        private async Task<bool> IsBiting(CancellationToken token) => (await Connection.ReadBytesAsync((uint)OffsetHelper.FishBitingOffset, 0x1, token).ConfigureAwait(false))[0] == 1;

        private async Task<bool> GetIsPlayerInventoryValid(uint playerOfs, CancellationToken token)
        {
            var (ofs, len) = InventoryValidator.GetOffsetLength(playerOfs);
            var inventory = await Connection.ReadBytesAsync(ofs, len, token).ConfigureAwait(false);

            return InventoryValidator.ValidateItemBinary(inventory);
        }

        // Additional
        private readonly byte[] MaxTextSpeed = new byte[1] { 3 };
        public async Task ClickConversation(SwitchButton b, int delay, CancellationToken token)
        {
            await Connection.WriteBytesAsync(MaxTextSpeed, (int)OffsetHelper.TextSpeedAddress, token).ConfigureAwait(false);
            await Click(b, delay, token).ConfigureAwait(false);
        }

        public override void SoftStop()
        {
            Paused = true;
        }
    }
}
