using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
// using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace PvZMOD.NPCs.Zombies
{
    public class BasicZombie : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 14;

            // Attack frames
            NPCID.Sets.AttackFrameCount[Type] = 7;
            // Attack range
            NPCID.Sets.DangerDetectRange[Type] = 32; //-1: 200 px
            // Attack type (3: swing a weapon)
            NPCID.Sets.AttackType[Type] = 3;
            // Attack time in ticks
            NPCID.Sets.AttackTime[Type] = 25;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                // Influences how the NPC looks in the Bestiary
                Velocity = 1f // Draws the NPC in the bestiary as if its walking +1 tiles in the x direction
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 48;
            NPC.damage = 16;
            NPC.defense = 6;
            NPC.lifeMax = 100;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 50f;
            NPC.knockBackResist = .5f;
            NPC.aiStyle = 3;
            AIType = NPCID.Zombie;
            Banner = Item.NPCtoBanner(NPCID.Zombie);
            BannerItem = Item.BannerToItem(Banner);
        }

        // public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.AddRange([
        //     BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,

        //     new FlavorTextBestiaryInfoElement("Mods.PvZDS.Bestiary.BasicZombie"),

        //     new FlavorTextBestiaryInfoElement("Mods.PvZDS.Bestiary.BasicZombie")
        // ]);

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int k = 0; k < 20; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, 0.78f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hit.HitDirection, -2.5f, 0, default, .54f);
            }

            if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
            {
                // for (int i = 1; i < 4; ++i)
                //     Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("DiverZombie" + i).Type, 1f);
            }
        }

        int frameTimer;
        int frame;

        public override void AI()
        {
            NPC.spriteDirection = NPC.direction;
            frameTimer++;
            if (NPC.wet)
            {
                // NPC.noGravity = true;
                NPC.velocity.Y *= .9f;
                NPC.velocity.Y -= .09f;
                NPC.velocity.X *= .95f;
                NPC.rotation = NPC.velocity.X * .1f;
                if (frameTimer >= 10)
                {
                    frame++;
                    frameTimer = 0;
                }

                // if (frame > 6 || frame < 2)
                if (frame > 6)
                    frame = 0;
            }
            else
            {
                NPC.noGravity = false;
                if (NPC.velocity.Y != 0)
                    frame = 0;
                else
                {
                    if (frameTimer >= 10)
                    {
                        frame++;
                        frameTimer = 0;
                    }

                    if (frame > 6)
                        frame = 0;
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!Main.dayTime && spawnInfo.Player.ZoneOverworldHeight)
                return 0.1f;

            return 0;
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                if (++frameTimer >= 10)
                {
                    frameTimer = 0;
                    frame = ++frame % 3;
                }
            }

            NPC.frame.Y = frameHeight * frame;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.Shackle, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ZombieArm, 250));
            // npcLoot.AddOneFromOptions(65, ModContent.ItemType<DiverLegs>(), ModContent.ItemType<DiverHead>(), ModContent.ItemType<DiverBody>());
        }
    }
}
