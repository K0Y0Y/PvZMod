using System.IO;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Audio;

namespace PvZMOD.NPCs.Zombies
{
    public class BasicZombie : ModNPC
    {
        public enum npcActionStatus : byte
        {
            WALKING,
            EATING
        }
        public enum npcHealthStatus : byte
        {
            HEALTHY,
            DAMAGED,
            ZERO
        }
        public npcActionStatus zombieActionStatus = npcActionStatus.WALKING;
        public npcHealthStatus zombieHealthStatus = npcHealthStatus.HEALTHY;

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
            //
            if (!(Main.netMode != NetmodeID.Server))
                return;

            if (NPC.life <= 0)
            {
                // dont repeat the code below
                if (zombieHealthStatus.Equals(npcHealthStatus.ZERO))
                    return;

                zombieHealthStatus = npcHealthStatus.ZERO;
                NPC.frame.Y = NPC.height * dyingFrameStart;
                NPC.life = 1;
                NPC.damage = 0;
                NPC.velocity = Vector2.Zero;
                NPC.aiStyle = -1;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("BasicZombieHead").Type, 1f);
                NPC.DeathSound = NPC.HitSound = new SoundStyle($"PvZMOD/Sounds/Zombies/Falling") with
                {
                    Volume = 0.5f
                };
            }
            else if (NPC.life <= (NPC.lifeMax / 2))
            {
                // dont repeat the code below
                if (zombieHealthStatus.Equals(npcHealthStatus.DAMAGED))
                    return;

                zombieHealthStatus = npcHealthStatus.DAMAGED;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("BasicZombieArm").Type, 1f);
                NPC.netUpdate = true;
            }
        }

        int deathFrameCounter;
        public override void AI()
        {
            // if is dead, wait to end for death animation
            if (zombieHealthStatus.Equals(npcHealthStatus.ZERO))
            {
                deathFrameCounter++;
                if (deathFrameCounter >= (frameSpeed * (dyingFrameEnd - dyingFrameStart) * 2))
                {
                    NPC.life = 0;
                    NPC.checkDead();
                }

                return;
            }

            Player target = Main.player[NPC.target];

            if (Vector2.Distance(NPC.Center, target.Center) <= 28f)
            {
                if (zombieHealthStatus.Equals(npcHealthStatus.HEALTHY))
                {
                    statusFrameStart = eatingFrameStart;
                    statusFrameEnd = eatingFrameEnd;
                }
                else if (zombieHealthStatus.Equals(npcHealthStatus.DAMAGED))
                {
                    statusFrameStart = eatingDamagedFrameStart;
                    statusFrameEnd = eatingDamagedFrameEnd;
                }

                NPC.aiStyle = -1;
                NPC.velocity.X = 0f;
                zombieActionStatus = npcActionStatus.EATING;
                SoundEngine.PlaySound(new SoundStyle($"PvZMOD/Sounds/Zombies/Eating_", 2) with
                {
                    Volume = 0.5f,
                    MaxInstances = 1,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                });
            }
            else
            {
                if (zombieHealthStatus.Equals(npcHealthStatus.HEALTHY))
                {
                    statusFrameStart = walkingFrameStart;
                    statusFrameEnd = walkingFrameEnd;
                }
                else if (zombieHealthStatus.Equals(npcHealthStatus.DAMAGED))
                {
                    statusFrameStart = walkingDamagedFrameStart;
                    statusFrameEnd = walkingDamagedFrameEnd;
                }

                NPC.aiStyle = 3;
                zombieActionStatus = npcActionStatus.WALKING;
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
                if (NPC.frame.Y < walkingFrameStart * frameHeight || NPC.frame.Y > dyingFrameEnd * frameHeight)
                {
                    NPC.frame.Y = walkingFrameStart * frameHeight;
                    NPC.frameCounter = 0;
                }
                return;
            }

            if (zombieHealthStatus.Equals(npcHealthStatus.ZERO))
            {
                if (NPC.frameCounter >= frameSpeed)
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

            if (NPC.frame.Y < (statusFrameStart * frameHeight) || NPC.frame.Y > (statusFrameEnd * frameHeight))
            {
                NPC.frame.Y = statusFrameStart * frameHeight;
                NPC.frameCounter = 0;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.Shackle, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ZombieArm, 250));
        }

        // public override void SendExtraAI(BinaryWriter writer)
        // {
        //     writer.Write(isEating);
        //     writer.Write(isDying);
        //     writer.Write(isDamaged);
        // }

        // public override void ReceiveExtraAI(BinaryReader reader)
        // {
        //     isEating = reader.ReadBoolean();
        //     isDying = reader.ReadBoolean();
        //     isDamaged = reader.ReadBoolean();
        // }
    }
}
