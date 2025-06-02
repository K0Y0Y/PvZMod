// using System.IO;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace PvZMOD.NPCs.Zombies
{
    public class ConeheadZombie : ModNPC
    {
        public enum npcStatus : byte
        {
            WALKING, //0-6m,14-20
            EATING, //7-13,21-27
            // DYING //x-x
        }
        public enum armorStatus : byte
        {
            GOOD, // 0-13
            DAMAGED, //13-27
            RUINED, //28-41
            NO_ARMOR //x-x
        }
        public npcStatus zombieStatus = npcStatus.WALKING;
        public armorStatus coneStatus = armorStatus.GOOD;

        private int frameSpeed = 6;
        private int statusFrameStart = 0;
        private int statusFrameEnd = 6;

        private int walkingFrameStart = 0;
        private int walkingFrameEnd = 6;
        private int eatingFrameStart = 7;
        private int eatingFrameEnd = 13;
        private int walkingDamagedFrameStart = 14;
        private int walkingDamagedFrameEnd = 20;
        private int eatingDamagedFrameStart = 21;
        private int eatingDamagedFrameEnd = 27;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 28;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 1f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 42;
            NPC.height = 60;
            NPC.damage = 20;
            NPC.defense = 4;
            NPC.lifeMax = 100;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 50f;
            NPC.knockBackResist = .75f;
            NPC.aiStyle = -1;
            AIType = NPCID.Zombie;
            // Banner = Item.NPCtoBanner(NPCID.Zombie);
            // BannerItem = Item.BannerToItem(Banner);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.AddRange([
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,

            new FlavorTextBestiaryInfoElement("Mods.Bestiary.ConeheadZombie"),
        ]);

        public override void HitEffect(NPC.HitInfo hit)
        {
            if ((Main.netMode == NetmodeID.Server))
                return;

            if (NPC.life <= 0)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Conehead").Type, 1f);
                // NPC.netUpdate = true;
                return;
            }
            if (NPC.life <= (NPC.lifeMax * 0.5))
            {
                coneStatus = armorStatus.RUINED;
                // NPC.netUpdate = true;
                return;
            }
            if (NPC.life <= (NPC.lifeMax * 0.75))
            {
                coneStatus = armorStatus.DAMAGED;
                // NPC.netUpdate = true;
                return;
            }
        }

        int deathFrameCounter;
        public override void AI()
        {
            Player target = Main.player[NPC.target];

            if (Vector2.Distance(NPC.Center, target.Center) <= 24f)
            {
                NPC.aiStyle = -1;
                NPC.velocity.X = 0f;
                zombieStatus = npcStatus.EATING;
            }
            else
            {
                NPC.aiStyle = 3;
                zombieStatus = npcStatus.WALKING;
            }

            NPC.spriteDirection = NPC.direction;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!Main.dayTime && spawnInfo.Player.ZoneOverworldHeight)
                return 0.1f;

            return 0;
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frame.Y += frameHeight;
                NPC.frameCounter = 0;
            }
            NPC.frameCounter++;

            if (NPC.IsABestiaryIconDummy)
            {
                if (NPC.frame.Y < walkingFrameStart * frameHeight || NPC.frame.Y > walkingFrameEnd * frameHeight)
                    NPC.frame.Y = walkingFrameStart * frameHeight;
                return;
            }
            else
            {
                if (zombieStatus.Equals(npcStatus.WALKING))
                {
                    switch (coneStatus)
                    {
                        case armorStatus.GOOD:
                            statusFrameStart = walkingFrameStart; // 0
                            statusFrameEnd = walkingFrameEnd; // 6
                            break;
                        case armorStatus.DAMAGED:
                            statusFrameStart = walkingDamagedFrameStart; // 14
                            statusFrameEnd = walkingDamagedFrameEnd; // 20
                            break;
                        case armorStatus.RUINED:
                            break;
                        case armorStatus.NO_ARMOR:
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (coneStatus)
                    {
                        case armorStatus.GOOD:
                            statusFrameStart = eatingFrameStart; // 7
                            statusFrameEnd = eatingFrameEnd; // 13
                            break;
                        case armorStatus.DAMAGED:
                            statusFrameStart = eatingDamagedFrameStart; // 21
                            statusFrameEnd = eatingDamagedFrameEnd; // 27
                            break;
                        case armorStatus.RUINED:
                            break;
                        case armorStatus.NO_ARMOR:
                            break;
                        default:
                            break;
                    }
                }
                // animation frame
                if (NPC.frame.Y < (statusFrameStart * frameHeight) || NPC.frame.Y > (statusFrameEnd * frameHeight))
                    NPC.frame.Y = statusFrameStart * frameHeight;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.Shackle, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ZombieArm, 250));
        }

        public override void OnKill()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetSource_Death(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<BasicZombie>());
            }
        }
    }
}
