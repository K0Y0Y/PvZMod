// using System.IO;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
// using Terraria.GameContent.ItemDropRules;
using Terraria.Audio;
using System;

namespace PvZMOD.NPCs.Zombies
{
    public class ImpZombie : ModNPC
    {
        public enum npcActionStatus : byte
        {
            WALKING,
            EATING,
            WALKING_DEAD,
            DYING
        }
        public enum npcHealthStatus : byte
        {
            HEALTHY,
            DAMAGED,
            ZERO
        }
        public npcActionStatus zombieActionStatus = npcActionStatus.WALKING;
        public npcHealthStatus zombieHealthStatus = npcHealthStatus.HEALTHY;

        private static readonly Random random = new Random();

        private int frameSpeed = 5;
        private int statusFrameStart = 0;
        private int statusFrameEnd = 7;

        private int walkingFrameStart = 0;
        private int walkingFrameEnd = 7;
        private int eatingFrameStart = 8;
        private int eatingFrameEnd = 15;

        private int walkingDamagedFrameStart = 16;
        private int walkingDamagedFrameEnd = 23;
        private int eatingDamagedFrameStart = 24;
        private int eatingDamagedFrameEnd = 31;

        private int specialDyingFrameStart = 32;
        private int specialDyingFrameEnd = 39;
        private int dyingFrameStart = 40;
        private int dyingFrameEnd = 45;

        private int timeToDead = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 46;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 1f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 30;
            NPC.height = 40;
            NPC.damage = 10;
            NPC.defense = 4;
            NPC.lifeMax = 35;
            // NPC.HitSound = new SoundStyle($"PvZMOD/Sounds/Zombies/Cone") with
            // {
            //     Volume = 0.25f,
            //     // SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
            // };
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 50f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            AIType = NPCID.Zombie;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.AddRange([
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,

            new FlavorTextBestiaryInfoElement("Mods.Bestiary.ImpZombie"),
        ]);

        public override void HitEffect(NPC.HitInfo hit)
        {
            if ((Main.netMode == NetmodeID.Server))
                return;

            if (NPC.life <= 0 && !zombieHealthStatus.Equals(npcHealthStatus.ZERO))
            {
                zombieHealthStatus = npcHealthStatus.ZERO;
                NPC.life = 1;
                NPC.damage = 0;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;

                if (random.NextDouble() >= 0.5)
                {
                    zombieActionStatus = npcActionStatus.DYING;
                    NPC.frame.Y = NPC.height * dyingFrameStart;
                    timeToDead = frameSpeed * (dyingFrameEnd - dyingFrameStart) * 2;
                }
                else
                {
                    zombieActionStatus = npcActionStatus.WALKING_DEAD;
                    NPC.frame.Y = NPC.height * specialDyingFrameStart;
                    timeToDead = frameSpeed * (dyingFrameEnd - specialDyingFrameStart) * 2;
                }

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("ImpZombieHead").Type, 1f);
                NPC.DeathSound = NPC.HitSound = new SoundStyle($"PvZMOD/Sounds/Zombies/Falling") with
                {
                    Volume = 0.5f
                };

                return;
            }

            if (NPC.life <= (NPC.lifeMax * 0.5) && !zombieHealthStatus.Equals(npcHealthStatus.DAMAGED))
            {
                zombieHealthStatus = npcHealthStatus.DAMAGED;
                NPC.netUpdate = true;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("ImpZombieArm").Type, 1f);
            }
        }

        int deathFrameCounter;
        int eatingSoundCooldown;
        public override void AI()
        {
            if (zombieHealthStatus.Equals(npcHealthStatus.ZERO))
            {
                if (!(zombieActionStatus.Equals(npcActionStatus.DYING) || zombieActionStatus.Equals(npcActionStatus.WALKING_DEAD)))
                    return;

                if (deathFrameCounter >= timeToDead)
                {
                    NPC.life = 0;
                    NPC.checkDead();
                    NPC.active = false;
                }
                deathFrameCounter++;

                return;
            }

            Player targetPlayer = Main.player[NPC.target];

            if (Vector2.Distance(NPC.Center, targetPlayer.Center) <= 24f)
            {
                if (zombieHealthStatus.Equals(npcHealthStatus.HEALTHY))
                {
                    statusFrameStart = eatingFrameStart;
                    statusFrameEnd = eatingFrameEnd;
                }
                else
                {
                    statusFrameStart = eatingDamagedFrameStart;
                    statusFrameEnd = eatingDamagedFrameEnd;
                }

                NPC.aiStyle = -1;
                NPC.velocity.X = 0f;
                zombieActionStatus = npcActionStatus.EATING;
                if (eatingSoundCooldown <= 0)
                {
                    SoundEngine.PlaySound(new SoundStyle($"PvZMOD/Sounds/Zombies/Eating_", 2) with
                    {
                        Volume = 0.5f,
                        MaxInstances = 1,
                        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                    });
                    eatingSoundCooldown = frameSpeed * 2;
                }
                else
                    eatingSoundCooldown--;
            }
            else
            {
                if (zombieHealthStatus.Equals(npcHealthStatus.HEALTHY))
                {
                    statusFrameStart = walkingFrameStart;
                    statusFrameEnd = walkingFrameEnd;
                }
                else
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
                    NPC.frame.Y = walkingFrameStart * frameHeight;
            }
            else
            {
                if (zombieHealthStatus.Equals(npcHealthStatus.ZERO))
                {
                    if (NPC.frame.Y >= dyingFrameStart * frameHeight)
                    {
                        NPC.velocity = Vector2.Zero;
                        NPC.aiStyle = -1;
                    }

                    if (NPC.frame.Y > dyingFrameEnd * frameHeight)
                    {
                        NPC.frame.Y = dyingFrameEnd * frameHeight;
                        return;
                    }

                    if (zombieActionStatus.Equals(npcActionStatus.WALKING_DEAD))
                        statusFrameStart = specialDyingFrameStart;
                    else
                        statusFrameStart = dyingFrameStart;

                    statusFrameEnd = dyingFrameEnd;
                }

                if (NPC.frame.Y < (statusFrameStart * frameHeight) || NPC.frame.Y > (statusFrameEnd * frameHeight))
                {
                    NPC.frame.Y = statusFrameStart * frameHeight;
                    NPC.frameCounter = 0;
                }
            }
        }

        // public override void ModifyNPCLoot(NPCLoot npcLoot)
        // {
        // }
    }
}
