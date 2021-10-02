﻿namespace ACNHMobileSpawner
{
    public static class OffsetHelper
    {
        // some helpers
        public const ulong PlayerSize = 0x10E500;
        public const ulong PlayerOtherStartPadding = 0x36A50;

        // player other 
        public const ulong InventoryOffset = 0xAED22840; // player 0 (A) 
        private const ulong playerOtherStart = InventoryOffset - 0x10; // helps to get other values, unused 

        public const ulong WalletAddress = InventoryOffset + 0xB8;
        public const ulong MilesAddress = InventoryOffset - 0x25590;
        public const ulong BankAddress = InventoryOffset + 0x224CC;

        // main player offsets functions
        private static ulong getPlayerStart(ulong invOffset) => invOffset - 0x10 - PlayerOtherStartPadding + 0x110;
        public static ulong getPlayerIdAddress(ulong invOffset) => getPlayerStart(invOffset) + 0xAFA8;
        public static ulong getPlayerProfileMainAddress(ulong invOffset) => getPlayerStart(invOffset) + 0x116A0;
        public static ulong getManpu(ulong invOffset) => invOffset - 0x10 + 0xAF7C + 72;
        public static ulong getTownNameAddress(ulong invOffset) => getPlayerIdAddress(invOffset) - 0xB8 + 0x04;

        // main save offsets
        public const ulong TurnipAddress = 0xADD1BB84;
        public const ulong VillagerAddress = TurnipAddress - 0x2d40 - 0x43be2c + 0x10;
        public const ulong VillagerHouseAddress = TurnipAddress - 0x2d40 - 0x43be2c + 0x43abe4;
        public const ulong BackupSaveDiff = 0x86D590;

        public const ulong FieldItemStart = VillagerAddress - 0x10 + 0x22e1b8;
        public const ulong LandMakingMapStart = FieldItemStart + 0xAAA00;
        public const ulong OutsideFieldStart = FieldItemStart + 0xCF998;
        public const ulong MainFieldStructurStart = FieldItemStart + 0xCF600;

        // other addresses
        public const ulong ArriverNameLocAddress = 0xB67D67B8;
        public const ulong ArriverVillageLocAddress = ArriverNameLocAddress - 0x1C;
        public const ulong ArriverVillageId = ArriverVillageLocAddress - 0x4;
        public const ulong ArriverNID = ArriverNameLocAddress - 0x3E8;

        public const ulong TextSpeedAddress = 0xBACF20C;
        public const ulong ChatBufferSize = 0x1E;

        public const ulong DodoAddress = 0xA99015C;
        public const ulong OnlineSessionAddress = 0x920F740;
        public const ulong OnlineSessionVisitorAddress = 0x9D3EFB0;
        public const ulong OnlineSessionVisitorSize = 0x448; // reverse order

        public const ulong TimeAddress = 0x0BAC6338;

        // pointers
        public static readonly long[] PlayerCoordJumps = new long[5] { 0x3A33980L, 0x18L, 0x178L, 0xD0L, 0xD8L };
        public static readonly long[] ChatCoordJumps = new long[2] { 0x3CFDC60L, 0x40L };

        // exefs (main)
        public const ulong AnimationSpeedOffset = 0x037F9200;
        public const ulong WalkSpeedOffset = 0x0114A670;
        public const ulong TimeStateAddress = 0x0027DD60;

        // fishing specific offsets
        public const ulong FishBitingOffset = 0xC304CF4;
        public const ulong IsFishingOffset = 0x14065521;
        public const ulong InventorySlotOffset = 0x43CE9971;
    }
}
