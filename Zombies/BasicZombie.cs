using System.IO;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace PvZMOD.NPCs.Zombies
{
    public class BasicZombie : ModNPC
    {
        public bool isEating = false;
        public bool isInjured = false;
        public bool isDying = false;

        private int frameSpeed = 6;

        private int walkingFrameStart = 0;
        private int walkingFrameEnd = 6;
        private int eatingFrameStart = 7;
        private int eatingFrameEnd = 13;

        private int walkingInjuredFrameStart = 14;
        private int walkingInjuredFrameEnd = 20;
        private int eatingInjuredFrameStart = 21;
        private int eatingInjuredFrameEnd = 27;

        private int dyingFrameStart = 28;
        private int dyingFrameEnd = 36;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 37;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 1f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 42;
            NPC.height = 48;
            NPC.damage = 20;
            NPC.defense = 4;
            NPC.lifeMax = 50;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 50f;
            NPC.knockBackResist = .75f;
            NPC.aiStyle = -1;
            AIType = NPCID.Zombie;
            Banner = Item.NPCtoBanner(NPCID.Zombie);
            BannerItem = Item.BannerToItem(Banner);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.AddRange([
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,

            new FlavorTextBestiaryInfoElement("Mods.Bestiary.BasicZombie"),
        ]);

        public override void HitEffect(NPC.HitInfo hit)
        {
            // for (int k = 0; k < 20; k++)
            // {
            //     Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, 0.78f);
            //     Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, default, .54f);
            // }

            if (!isDying && NPC.life <= 0 && Main.netMode != NetmodeID.Server)
            {
                NPC.frame.Y = NPC.height * dyingFrameStart;
                NPC.life = 1;
                NPC.damage = 0;
                NPC.velocity = Vector2.Zero;
                NPC.dontTakeDamage = true;
                isDying = true;
                NPC.netUpdate = true;

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("BasicZombieHead").Type, 1f);
            }

            if (!isInjured && NPC.life <= (NPC.lifeMax / 2) && Main.netMode != NetmodeID.Server)
            {
                isInjured = true;
                NPC.netUpdate = true;

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("BasicZombieArm").Type, 1f);
            }
        }

        int deathFrameCounter;
        public override void AI()
        {
            if (isDying)
            {
                NPC.velocity = Vector2.Zero;
                NPC.aiStyle = -1;

                deathFrameCounter++;
                if (deathFrameCounter >= (frameSpeed * (dyingFrameEnd - dyingFrameStart) * 2))
                {
                    NPC.life = 0;
                    NPC.checkDead();
                }

                return;
            }

            Player target = Main.player[NPC.target];

            if (Vector2.Distance(NPC.Center, target.Center) <= 24f)
            {
                NPC.aiStyle = -1;
                NPC.velocity.X = 0f;
                isEating = true;
            }
            else
            {
                NPC.aiStyle = 3;
                isEating = false;
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
            if (isDying)
            {
                if (NPC.frameCounter++ >= frameSpeed)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y += frameHeight;

                    if (NPC.frame.Y > dyingFrameEnd * frameHeight)
                    {
                        NPC.frame.Y = dyingFrameEnd * frameHeight;
                    }
                }
                return;
            }

            if (NPC.velocity.X != 0f || NPC.IsABestiaryIconDummy)
            {
                // frame speed
                if (NPC.frameCounter++ >= frameSpeed)
                {
                    NPC.frame.Y += frameHeight;
                    NPC.frameCounter = 0;
                }

                if (!isInjured)
                {
                    if (NPC.frame.Y < walkingFrameStart * frameHeight || NPC.frame.Y > walkingFrameEnd * frameHeight)
                        NPC.frame.Y = walkingFrameStart * frameHeight;
                }
                else
                {
                    if (NPC.frame.Y < walkingInjuredFrameStart * frameHeight || NPC.frame.Y > walkingInjuredFrameEnd * frameHeight)
                        NPC.frame.Y = walkingInjuredFrameStart * frameHeight;
                }
            }
            else
            {
                if (isEating)
                {
                    if (NPC.frameCounter++ >= frameSpeed)
                    {
                        NPC.frame.Y += frameHeight;
                        NPC.frameCounter = 0;
                    }

                    if (!isInjured)
                    {
                        if (NPC.frame.Y < eatingFrameStart * frameHeight || NPC.frame.Y > eatingFrameEnd * frameHeight)
                            NPC.frame.Y = eatingFrameStart * frameHeight;
                    }
                    else
                    {
                        if (NPC.frame.Y < eatingInjuredFrameStart * frameHeight || NPC.frame.Y > eatingInjuredFrameEnd * frameHeight)
                            NPC.frame.Y = eatingInjuredFrameStart * frameHeight;
                    }
                }
                else
                {
                    // IDK
                    NPC.frame.Y = 0 * frameHeight;
                }
            }

            // NPC.frame.Y = frameHeight * frame;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.Shackle, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ZombieArm, 250));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(isEating);
            writer.Write(isDying);
            writer.Write(isInjured);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            isEating = reader.ReadBoolean();
            isDying = reader.ReadBoolean();
            isInjured = reader.ReadBoolean();
        }
    }
}
